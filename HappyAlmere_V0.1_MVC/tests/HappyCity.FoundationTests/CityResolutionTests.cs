using System.Collections.Concurrent;
using System.Data.Common;
using HappyCity.Application;
using HappyCity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace HappyCity.FoundationTests;

// These checks are invoked against the actual restored target by the one lifecycle test.
internal static class CityResolutionTests
{
    internal static readonly Guid Utrecht = Guid.Parse("60000000-0000-4000-8000-000000000002");

    internal static async Task VerifyAsync(string runtime, string migration, CityCommandRecorder recorder)
    {
        await using var database = new HappyCityDbContext(new DbContextOptionsBuilder<HappyCityDbContext>()
            .UseNpgsql(DatabaseConfiguration.RequireVerifiedTls(runtime)).AddInterceptors(recorder).Options);
        var resolver = new PostgresCityResolver(database, new Dictionary<string, string>
        {
            ["localhost"] = "almere", ["127.0.0.1"] = "almere", ["bypass.example"] = "almere",
            ["unknown.test"] = "almere"
        });
        foreach (var host in new[] { "ALMERE.TEST", "utrecht.test", "alias.almere.test", "localhost", "127.0.0.1" })
        {
            var before = recorder.Count;
            var resolution = await resolver.ResolveAsync(host);
            Assert.NotNull(resolution);
            Assert.Equal(1, recorder.Count - before);
            Assert.Equal(host is "localhost" or "127.0.0.1" ? "localResolver" : "registeredResolver",
                recorder.Snapshot()[before].Kind);
            var other = host is "utrecht.test" or "alias.almere.test";
            Assert.Equal(other ? Utrecht : HappyCity.Domain.LegacyIdentifier.Map(1, 1), resolution.CityId);
            Assert.Equal(other ? "city-utrecht" : "almere", resolution.CitySlug);
            Assert.Equal("NL", resolution.CountryCode);
            Assert.Equal("nl", resolution.DefaultLanguage);
            Assert.Equal("Europe/Amsterdam", resolution.TimeZone);
            Assert.Equal(host.ToLowerInvariant(), resolution.RequestedHost);
            Assert.Equal(host is "localhost" or "127.0.0.1", resolution.IsLocalEntryPoint);
            Assert.Equal(host == "alias.almere.test", resolution.RequiresCanonicalRedirect);
            Assert.Equal(resolution.IsLocalEntryPoint ? null : other ? "utrecht.test" : "almere.test", resolution.CanonicalHost);
        }
        foreach (var host in new[] { "unknown.test", "inactive.test", "city-inactive.test", "no-primary.test",
                     "bypass.example", "happyalmere.nl", "almere.test.", " almere.test", "almere.test ",
                     "https://almere.test", "almere.test@evil.test", "a..test", "-a.test", "a-.test",
                     "é.test", "a_b.test", "", new string('a', 64) + ".test", new string('a', 254) })
            Assert.Null(await resolver.ResolveAsync(host));
        Assert.Null(await new PostgresCityResolver(database, new Dictionary<string, string>()).ResolveAsync("localhost"));
        Assert.Null(await new PostgresCityResolver(database, new Dictionary<string, string> { ["localhost"] = "city-inactive" }).ResolveAsync("localhost"));

        await using var connection = new NpgsqlConnection(DatabaseConfiguration.RequireVerifiedTls(migration));
        await connection.OpenAsync();
        foreach (var (statement, expected) in new[]
        {
            ("INSERT INTO catalog.city_domains(id,city_id,hostname,is_primary,is_active) VALUES(gen_random_uuid(),'60000000-0000-4000-8000-000000000002','almere.test',false,true)", "23505"),
            ("INSERT INTO catalog.city_domains(id,city_id,hostname,is_primary,is_active) VALUES(gen_random_uuid(),'60000000-0000-4000-8000-000000000002','second-primary.test',true,true)", "23505"),
            ("INSERT INTO catalog.districts(id,city_id,parent_id,slug,name,district_type,is_active) VALUES(gen_random_uuid(),'60000000-0000-4000-8000-000000000002','64000000-0000-4000-8000-000000000001','foreign-parent','Synthetic','test',true)", "23503"),
            ("UPDATE catalog.cities SET country_code='ZZ' WHERE slug='city-utrecht'", "23514")
        })
        {
            await using var transaction = await connection.BeginTransactionAsync();
            await using (var positive = new NpgsqlCommand("INSERT INTO catalog.districts(id,city_id,slug,name,district_type,is_active) VALUES('64000000-0000-4000-8000-000000000001','00000001-0000-8000-8000-000000000001','city-positive','Synthetic','test',true)", connection, transaction))
                Assert.Equal(1, await positive.ExecuteNonQueryAsync());
            await using var negative = new NpgsqlCommand(statement, connection, transaction);
            var error = await Assert.ThrowsAsync<PostgresException>(() => negative.ExecuteNonQueryAsync());
            Assert.Equal(expected, error.SqlState);
            await transaction.RollbackAsync();
        }
        Assert.False(database.Database.HasPendingModelChanges());
    }
}

internal sealed record CityCommandEvidence(string Kind, string[] ParameterTypes, int ParameterCount, bool ParametersReferenced);

internal sealed class CityCommandRecorder : DbCommandInterceptor
{
    private readonly ConcurrentQueue<CityCommandEvidence> commands = new();
    internal int Count => commands.Count;
    internal CityCommandEvidence[] Snapshot() => commands.ToArray();
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        var sql = command.CommandText;
        var kind = sql.Contains("city_domains", StringComparison.Ordinal) ? "registeredResolver"
            : sql.Contains("discovery_entries", StringComparison.Ordinal) ? "discovery"
            : sql.Contains("cities", StringComparison.Ordinal) ? "localResolver"
            : throw new InvalidOperationException("Unknown City command evidence category.");
        var parameters = command.Parameters.Cast<DbParameter>().ToArray();
        Assert.InRange(parameters.Length, 1, 32);
        var types = parameters.Select(p => p.DbType).ToArray();
        Assert.All(types, type => Assert.True(Enum.IsDefined(type), "Unknown parameter type."));
        var referenced = parameters.All(parameter => System.Text.RegularExpressions.Regex.IsMatch(sql,
            @"@" + System.Text.RegularExpressions.Regex.Escape(parameter.ParameterName.TrimStart('@')) + @"\b",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant));
        Assert.True(referenced, "City query parameter reference missing.");
        // SQL and parameter names/values remain in memory and never enter the wire record.
        commands.Enqueue(new CityCommandEvidence(kind, types.Select(type => type.ToString()).ToArray(),
            parameters.Length, referenced));
        return ValueTask.FromResult(result);
    }
}
