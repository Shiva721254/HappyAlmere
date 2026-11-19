using System.Net;
using HappyCity.Application;
using HappyCity.Domain;
using HappyCity.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace HappyCity.FoundationTests;

[Trait("Category", "Postgres")]
public sealed class PostgresTests
{
    private static string Connection(bool migration = false) =>
        DatabaseConfiguration.RequireVerifiedTls(Environment.GetEnvironmentVariable(
            migration ? "HAPPYCITY_MIGRATION_CONNECTION" : "ConnectionStrings__HappyCity"));

    private static HappyCityDbContext Database(bool migration = false) =>
        new(new DbContextOptionsBuilder<HappyCityDbContext>().UseNpgsql(Connection(migration)).Options);

    [Fact]
    public async Task Migrations_are_current_and_repeatable()
    {
        await using var database = Database(true);
        await database.Database.MigrateAsync();
        Assert.Empty(await database.Database.GetPendingMigrationsAsync());
        Assert.False(database.Database.HasPendingModelChanges());
        Assert.Equal(6, await database.DiscoveryEntries.CountAsync());
    }

    [Theory]
    [InlineData(null, null, 6)]
    [InlineData("activiteiten", null, 2)]
    [InlineData(null, " FAMILIE ", 1)]
    [InlineData(" ETEN ", "lunch", 1)]
    [InlineData("diensten", "familie", 0)]
    [InlineData(null, "does-not-exist", 0)]
    [InlineData(null, "ideeën", 1)]
    [InlineData(null, "%", 0)]
    [InlineData(null, "_", 0)]
    [InlineData(null, "\\", 0)]
    [InlineData(null, "' OR 1=1 --", 0)]
    public async Task Real_postgres_search_matches_literal_filter_contract(string? category, string? query, int count)
    {
        await using var database = Database();
        var service = new DiscoveryService(new PostgresDiscoveryRepository(database));
        var results = await service.FindAsync(new DiscoveryFilter("almere", category, query));
        Assert.Equal(count, results.Count);
        Assert.Empty(database.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Cities_and_drafts_are_isolated_and_queries_are_bounded()
    {
        await using var database = Database(true);
        await using var transaction = await database.Database.BeginTransactionAsync();
        var utrecht = new City { Slug = "utrecht", Name = "Synthetic Utrecht" };
        database.Cities.Add(utrecht);
        await database.SaveChangesAsync();
        database.DiscoveryEntries.Add(Entry(utrecht.Id, "wandeling", "Utrecht only"));
        var draft = Entry(1, "draft", "Unpublished");
        draft.IsPublished = false;
        database.DiscoveryEntries.Add(draft);
        await database.SaveChangesAsync();
        var repository = new PostgresDiscoveryRepository(database);
        Assert.Single(await repository.FindAsync(new DiscoveryFilter("utrecht", null, null)));
        var almere = await repository.FindAsync(new DiscoveryFilter("almere", null, null));
        Assert.Equal(6, almere.Count);
        Assert.DoesNotContain(almere, item => item.Title is "Utrecht only" or "Unpublished");
        Assert.Empty(await repository.FindAsync(new DiscoveryFilter("unknown-city", null, null)));

        for (var i = 0; i < 105; i++)
            database.DiscoveryEntries.Add(Entry(1, "bounded-" + i, "Bounded " + i));
        await database.SaveChangesAsync();
        Assert.Equal(DiscoveryFilter.MaximumResults,
            (await repository.FindAsync(new DiscoveryFilter("almere", null, null))).Count);
        // Disposing the transaction rolls back only these test fixtures, never the seeded database.
    }

    [Theory]
    [InlineData("duplicate", PostgresErrorCodes.UniqueViolation)]
    [InlineData("city", PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData("category", PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData("title", PostgresErrorCodes.CheckViolation)]
    [InlineData("slug", PostgresErrorCodes.CheckViolation)]
    [InlineData("length", PostgresErrorCodes.StringDataRightTruncation)]
    public async Task Database_enforces_content_constraints(string scenario, string sqlState)
    {
        await using var database = Database(true);
        await using var transaction = await database.Database.BeginTransactionAsync();
        var entry = Entry(1, "constraint-test", "Synthetic test");
        switch (scenario)
        {
            case "duplicate": entry.Slug = "wandeling"; break;
            case "city": entry.CityId = int.MaxValue; break;
            case "category": entry.CategoryId = int.MaxValue; break;
            case "title": entry.Title = " "; break;
            case "slug": entry.Slug = "Invalid Slug"; break;
            case "length": entry.Title = new string('x', 201); break;
        }
        database.Add(entry);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
        Assert.Equal(sqlState, Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    [Fact]
    public async Task Runtime_connection_uses_tls_and_cannot_write_or_create_tables()
    {
        await using var connection = new NpgsqlConnection(Connection());
        await connection.OpenAsync();
        await using var ssl = new NpgsqlCommand("SELECT ssl FROM pg_stat_ssl WHERE pid = pg_backend_pid()", connection);
        Assert.Equal(true, await ssl.ExecuteScalarAsync());
        foreach (var sql in new[] { "DELETE FROM \"DiscoveryEntries\" WHERE false", "CREATE TABLE forbidden_probe (id integer)" })
        {
            await using var command = new NpgsqlCommand(sql, connection);
            var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
        }
    }

    [Fact]
    public async Task Cancellation_reaches_the_repository()
    {
        await using var database = Database();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new PostgresDiscoveryRepository(database).FindAsync(new DiscoveryFilter("almere", null, null), cancellation.Token));
    }

    [Fact]
    public async Task Untrusted_certificate_is_rejected_by_real_connection()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            "CN=Untrusted test root", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        var path = Path.Combine(Path.GetTempPath(), "happycity-test-" + Guid.NewGuid() + ".crt");
        await File.WriteAllTextAsync(path, certificate.ExportCertificatePem());
        try
        {
            var settings = new NpgsqlConnectionStringBuilder(Connection())
            {
                RootCertificate = path, Pooling = false, Timeout = 3
            };
            await using var connection = new NpgsqlConnection(settings.ConnectionString);
            await Assert.ThrowsAsync<NpgsqlException>(() => connection.OpenAsync());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Mvc_uses_postgres_and_cannot_be_switched_to_another_city_by_query_string()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:HappyCity"] = Connection()
                }));
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        using var response = await client.GetAsync("/?q=familie&city=utrecht&citySlug=utrecht");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Een familiedag plannen", html);
        Assert.DoesNotContain("Utrecht only", html);
        Assert.Contains("script-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    private static DiscoveryEntry Entry(int cityId, string slug, string title) => new()
    {
        CityId = cityId, CategoryId = 1, Slug = slug, Title = title,
        Description = "Synthetic test only", District = "Test district", Icon = "*", IsPublished = true
    };

    [Fact]
    public async Task Unencrypted_tcp_is_rejected_by_postgres()
    {
        // Deliberately invalid client for a negative server-policy test, not application configuration.
        var settings = new NpgsqlConnectionStringBuilder(Connection()) { SslMode = SslMode.Disable, Pooling = false };
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.OpenAsync());
        Assert.Equal(PostgresErrorCodes.InvalidAuthorizationSpecification, exception.SqlState);
    }

    [Fact]
    public async Task Database_outage_returns_generic_error_without_demo_fallback()
    {
        var unavailable = new NpgsqlConnectionStringBuilder(Connection()) { Port = 1, Timeout = 1, Pooling = false };
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:HappyCity"] = unavailable.ConnectionString
                }));
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Er is iets misgegaan.", html);
        Assert.DoesNotContain("Een familiedag plannen", html);
        Assert.DoesNotContain(unavailable.Password!, html);
        Assert.Contains("script-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }
}
