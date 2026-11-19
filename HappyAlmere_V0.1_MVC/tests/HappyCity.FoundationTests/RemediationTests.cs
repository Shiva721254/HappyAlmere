using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using HappyCity.Application;
using HappyCity.Domain;
using HappyCity.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
namespace HappyCity.FoundationTests;

[Trait("Category", "Postgres")]
public sealed class RemediationTests
{
    private static string Connection => DatabaseConfiguration.RequireVerifiedTls(Environment.GetEnvironmentVariable("HAPPYCITY_MIGRATION_CONNECTION"));
    private static HappyCityDbContext Database(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<HappyCityDbContext>().UseNpgsql(Connection).AddInterceptors(interceptors).Options);

    [Fact]
    public async Task Trusted_hosts_resolve_canonical_alias_and_reject_inactive_unknown_and_forwarded_values()
    {
        await using var db = Database(); await using var tx = await db.Database.BeginTransactionAsync();
        var city = new City { Slug = "synthetic-utrecht", Name = "Synthetic Utrecht", CountryCode = "NL", DefaultLanguage = "nl", TimeZone = "Europe/Amsterdam", IsActive = true };
        db.Cities.Add(city); await db.SaveChangesAsync();
        var almere = LegacyIdentifier.Map(1, 1);
        db.CityDomains.AddRange(new CityDomain { CityId = almere, Hostname = "almere.test", IsActive = true, IsPrimary = true },
            new CityDomain { CityId = almere, Hostname = "alias.almere.test", IsActive = true },
            new CityDomain { CityId = city.Id, Hostname = "utrecht.test", IsActive = true, IsPrimary = true },
            new CityDomain { CityId = city.Id, Hostname = "inactive.test", IsActive = false });
        await db.SaveChangesAsync();
        var resolver = new PostgresCityResolver(db, new Dictionary<string, string> { { "localhost", "almere" } });
        Assert.Equal(new CityResolution("almere", null), await resolver.ResolveAsync("localhost"));
        Assert.Equal(new CityResolution("almere", null), await resolver.ResolveAsync("ALMERE.TEST"));
        Assert.Equal(new CityResolution("almere", "almere.test"), await resolver.ResolveAsync("alias.almere.test"));
        Assert.Equal("synthetic-utrecht", (await resolver.ResolveAsync("utrecht.test"))!.CitySlug);
        foreach (var host in new[] { "unknown.test", "inactive.test", "happyalmere.nl", "almere.test.", "almere.test@evil.test", " almere.test", "127.0.0.1" }) Assert.Null(await resolver.ResolveAsync(host));
        city.IsActive = false; await db.SaveChangesAsync(); Assert.Null(await resolver.ResolveAsync("utrecht.test"));
    }

    [Fact]
    public async Task Http_host_isolation_and_canonical_redirect_ignore_forwarded_headers()
    {
        await using var db = Database();
        var city = new City { Slug = "http-synthetic", Name = "Synthetic HTTP", CountryCode = "NL", DefaultLanguage = "nl", TimeZone = "Europe/Amsterdam", IsActive = true };
        db.Cities.Add(city); await db.SaveChangesAsync();
        var domains = new[]{new CityDomain {CityId=city.Id,Hostname="utrecht.test",IsActive=true,IsPrimary=true},
            new CityDomain {CityId=city.Id,Hostname="alias.almere.test",IsActive=true}};
        var entry = new DiscoveryEntry { CityId = city.Id, CategoryId = LegacyIdentifier.Map(2, 1), Slug = "http-only", Title = "Synthetic other city only", Description = "Synthetic", District = "Test", Icon = "*", IsPublished = true };
        db.CityDomains.AddRange(domains); db.DiscoveryEntries.Add(entry); await db.SaveChangesAsync();
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => { b.UseEnvironment("Production"); b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { { "ConnectionStrings:HappyCity", Environment.GetEnvironmentVariable("ConnectionStrings__HappyCity") } })); });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            using var request = new HttpRequestMessage(HttpMethod.Get, "/?city=almere"); request.Headers.Host = "utrecht.test"; request.Headers.Add("X-Forwarded-Host", "evil.test");
            using var response = await client.SendAsync(request); var html = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains(entry.Title, html); Assert.DoesNotContain("Een familiedag plannen", html);
            using var alias = new HttpRequestMessage(HttpMethod.Get, "/?q=test"); alias.Headers.Host = "alias.almere.test:9999"; alias.Headers.Add("Forwarded", "host=evil.test;proto=http");
            using var redirected = await client.SendAsync(alias); Assert.Equal(HttpStatusCode.MovedPermanently, redirected.StatusCode); Assert.Equal("https://utrecht.test/?q=test", redirected.Headers.Location!.ToString());
            using var forged = new HttpRequestMessage(HttpMethod.Get, "/"); forged.Headers.Host = "evil.test"; forged.Headers.Add("X-Forwarded-Host", "localhost");
            using var denied = await client.SendAsync(forged); Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
            using var unknown = await client.GetAsync(new Uri("https://almere.test/")); Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            city.IsActive = false; await db.SaveChangesAsync();
            using var inactive = await client.GetAsync(new Uri("https://utrecht.test/")); Assert.Equal(HttpStatusCode.NotFound, inactive.StatusCode);
            using var local = new HttpRequestMessage(HttpMethod.Get, "/"); local.Headers.Add("X-Forwarded-Host", "utrecht.test"); using var localResponse = await client.SendAsync(local); Assert.Contains("Een familiedag plannen", await localResponse.Content.ReadAsStringAsync());
        }
        finally { db.DiscoveryEntries.Remove(entry); db.CityDomains.RemoveRange(domains); db.Cities.Remove(city); await db.SaveChangesAsync(); }
    }

    [Fact]
    public async Task Translation_resolution_uses_requested_city_default_then_legacy_and_is_bounded()
    {
        await using var db = Database(); await using var tx = await db.Database.BeginTransactionAsync();
        var id = LegacyIdentifier.Map(2, 1); var lookup = new PostgresCategoryLookup(db);
        var original = (await db.Categories.SingleAsync(x => x.Id == id)).Name;
        Assert.Equal(original, (await lookup.FindAsync("almere", "en")).Single(x => x.Id == id).Name);
        db.CategoryTranslations.Add(new CategoryTranslation { CategoryId = id, LanguageCode = "en", Name = "Synthetic English label" }); await db.SaveChangesAsync();
        Assert.Equal("Synthetic English label", (await lookup.FindAsync("almere", " EN ")).Single(x => x.Id == id).Name);
        var city = await db.Cities.SingleAsync(); city.DefaultLanguage = "en";
        db.CategoryTranslations.Remove(await db.CategoryTranslations.SingleAsync(x => x.CategoryId == id && x.LanguageCode == "nl")); await db.SaveChangesAsync();
        Assert.Equal("Synthetic English label", (await lookup.FindAsync("almere", "nl")).Single(x => x.Id == id).Name);
        db.CategoryTranslations.Remove(await db.CategoryTranslations.SingleAsync(x => x.CategoryId == id)); await db.SaveChangesAsync();
        Assert.Equal(original, (await lookup.FindAsync("almere", "nl")).Single(x => x.Id == id).Name);
        Assert.Empty(await lookup.FindAsync("unknown-city", "nl"));
        await Assert.ThrowsAsync<ArgumentException>(() => lookup.FindAsync("almere", "zz"));
        await db.Database.ExecuteSqlRawAsync("INSERT INTO catalog.categories(id,slug,name,category_type,display_order,is_active) SELECT gen_random_uuid(),'bounded-'||n,'Synthetic','discovery',n,true FROM generate_series(1,120) n");
        Assert.Equal(100, (await lookup.FindAsync("almere", "nl")).Count);
    }

    [Theory]
    [InlineData("UPDATE catalog.cities SET country_code='ZZ'")]
    [InlineData("UPDATE catalog.cities SET default_language='fr'")]
    [InlineData("UPDATE catalog.cities SET default_language='NL'")]
    [InlineData("UPDATE catalog.category_translations SET language_code='zz'")]
    [InlineData("INSERT INTO catalog.locations(id,city_id,street,house_number,postal_code,country_code) VALUES(gen_random_uuid(),'00000001-0000-8000-8000-000000000001','Synthetic','1','0000XX','BE')")]
    public async Task Unsupported_regional_values_are_rejected_by_database(string sql)
    {
        await using var db = Database(); await using var tx = await db.Database.BeginTransactionAsync();
        Assert.Equal("NL", RegionalPolicy.Country(" nl ")); Assert.Equal("en", RegionalPolicy.Language(" EN "));
        Assert.Throws<ArgumentException>(() => RegionalPolicy.Country("BE"));
        var e = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(sql)); Assert.Equal("23514", e.SqlState);
    }

    [Fact]
    public async Task Invalid_translation_foreign_key_is_rejected()
    {
        await using var db = Database(); await using var tx = await db.Database.BeginTransactionAsync();
        db.CategoryTranslations.Add(new CategoryTranslation { CategoryId = Guid.NewGuid(), LanguageCode = "nl", Name = "Synthetic" });
        var e = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); Assert.Equal("23503", Assert.IsType<PostgresException>(e.InnerException).SqlState);
    }

    [Theory]
    [InlineData("categories")]
    [InlineData("districts")]
    public async Task Waiting_hierarchy_transaction_rejects_cycle_after_competitor_commits(string table)
    {
        var idA = Guid.NewGuid(); var idB = Guid.NewGuid();
        await using var a = new NpgsqlConnection(Connection); await using var b = new NpgsqlConnection(Connection); await a.OpenAsync(); await b.OpenAsync();
        var insert = table == "categories" ? "INSERT INTO catalog.categories(id,slug,name,category_type,display_order,is_active) VALUES(@a,@sa,'Synthetic','discovery',0,false),(@b,@sb,'Synthetic','discovery',0,false)" : "INSERT INTO catalog.districts(id,city_id,slug,name,district_type,is_active) VALUES(@a,'00000001-0000-8000-8000-000000000001',@sa,'Synthetic','test',false),(@b,'00000001-0000-8000-8000-000000000001',@sb,'Synthetic','test',false)";
        await using (var seed = new NpgsqlCommand(insert, a)) { seed.Parameters.AddWithValue("a", idA); seed.Parameters.AddWithValue("b", idB); seed.Parameters.AddWithValue("sa", "r01-" + idA.ToString("N")[..20]); seed.Parameters.AddWithValue("sb", "r01-" + idB.ToString("N")[..20]); await seed.ExecuteNonQueryAsync(); }
        try
        {
            await using (var ta = await a.BeginTransactionAsync())
            await using (var tb = await b.BeginTransactionAsync())
            {
                await Change(a, ta, table, idA, idB);
                var waiting = Change(b, tb, table, idB, idA); await Task.Delay(150); Assert.False(waiting.IsCompleted);
                await ta.CommitAsync();
                var e = await Assert.ThrowsAsync<PostgresException>(async () => await waiting.WaitAsync(TimeSpan.FromSeconds(5))); Assert.Equal("23514", e.SqlState); Assert.Equal("Hierarchy cycle rejected", e.MessageText); await tb.RollbackAsync();
            }
            // Valid competing change succeeds after commit; then rollback leaves valid prior state.
            await using (var ta = await a.BeginTransactionAsync())
            await using (var tb = await b.BeginTransactionAsync())
            {
                await Change(a, ta, table, idA, null);
                var waiting = Change(b, tb, table, idB, idA); await Task.Delay(150); Assert.False(waiting.IsCompleted); await ta.CommitAsync(); await waiting.WaitAsync(TimeSpan.FromSeconds(5)); await tb.RollbackAsync();
            }
            await using var verify = new NpgsqlCommand($"SELECT count(*) FROM catalog.{table} WHERE id IN (@a,@b) AND parent_id IS NOT NULL", a); verify.Parameters.AddWithValue("a", idA); verify.Parameters.AddWithValue("b", idB); Assert.Equal(0L, await verify.ExecuteScalarAsync());
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand($"UPDATE catalog.{table} SET parent_id=NULL WHERE id IN (@a,@b); DELETE FROM catalog.{table} WHERE id IN (@a,@b)", a); cleanup.Parameters.AddWithValue("a", idA); cleanup.Parameters.AddWithValue("b", idB); await cleanup.ExecuteNonQueryAsync();
        }
    }

    private static async Task Change(NpgsqlConnection c, NpgsqlTransaction tx, string table, Guid id, Guid? parent)
    {
        await using var command = new NpgsqlCommand($"SET LOCAL statement_timeout='4s'; UPDATE catalog.{table} SET parent_id=@parent WHERE id=@id", c, tx); command.Parameters.AddWithValue("id", id); command.Parameters.Add(new NpgsqlParameter("parent", NpgsqlTypes.NpgsqlDbType.Uuid) { Value = (object?)parent ?? DBNull.Value }); await command.ExecuteNonQueryAsync();
    }

    [Theory]
    [InlineData("categories", false)]
    [InlineData("categories", true)]
    [InlineData("districts", false)]
    [InlineData("districts", true)]
    public async Task Multirow_hierarchy_cycles_are_rejected(string table, bool update)
    {
        await using var db = Database(); await using var tx = await db.Database.BeginTransactionAsync(); var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var insert = table == "categories" ? $"INSERT INTO catalog.categories(id,parent_id,slug,name,category_type,display_order,is_active) VALUES('{a}',PARENTA,'r01-{a.ToString("N")[..20]}','Synthetic','discovery',0,false),('{b}',PARENTB,'r01-{b.ToString("N")[..20]}','Synthetic','discovery',0,false)" : $"INSERT INTO catalog.districts(id,parent_id,city_id,slug,name,district_type,is_active) VALUES('{a}',PARENTA,'00000001-0000-8000-8000-000000000001','r01-{a.ToString("N")[..20]}','Synthetic','test',false),('{b}',PARENTB,'00000001-0000-8000-8000-000000000001','r01-{b.ToString("N")[..20]}','Synthetic','test',false)";
        if (update) await db.Database.ExecuteSqlRawAsync(insert.Replace("PARENTA", "NULL").Replace("PARENTB", "NULL"));
        var sql = update ? $"UPDATE catalog.{table} SET parent_id=CASE WHEN id='{a}' THEN '{b}'::uuid ELSE '{a}'::uuid END WHERE id IN ('{a}','{b}')" : insert.Replace("PARENTA", $"'{b}'").Replace("PARENTB", $"'{a}'");
        var e = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(sql)); Assert.Equal("23514", e.SqlState); Assert.Equal("Hierarchy cycle rejected", e.MessageText);
    }
}
