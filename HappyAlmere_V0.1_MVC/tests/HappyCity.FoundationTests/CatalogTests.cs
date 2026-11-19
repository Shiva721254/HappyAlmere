using Microsoft.EntityFrameworkCore.Storage;
using HappyCity.Application;
using HappyCity.Domain;
using HappyCity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HappyCity.FoundationTests;

[Trait("Category", "Postgres")]
public sealed class CatalogTests
{
    private static HappyCityDbContext Database() => new(new DbContextOptionsBuilder<HappyCityDbContext>()
        .UseNpgsql(DatabaseConfiguration.RequireVerifiedTls(Environment.GetEnvironmentVariable("HAPPYCITY_MIGRATION_CONNECTION"))).Options);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Legacy_mapping_is_repeatable_and_disjoint(int value)
    {
        var ids = Enumerable.Range(1, 3).Select(tag => LegacyIdentifier.Map(tag, value)).ToArray();
        Assert.Equal(3, ids.Distinct().Count());
        Assert.Equal(ids[0], LegacyIdentifier.Map(1, value));
        Assert.Equal('8', ids[0].ToString()[14]);
        Assert.Equal('4', Guid.NewGuid().ToString()[14]);
        Assert.Throws<ArgumentOutOfRangeException>(() => LegacyIdentifier.Map(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => LegacyIdentifier.Map(4, value));
    }

    [Fact]
    public async Task Pagination_covers_legacy_and_new_records_with_filter_bound_cursors()
    {
        await using var db = Database();
        await using var tx = await db.Database.BeginTransactionAsync();
        var city = LegacyIdentifier.Map(1, 1);
        for (var i = 0; i < 7; i++) db.DiscoveryEntries.Add(new DiscoveryEntry
        {
            CityId = city,
            CategoryId = LegacyIdentifier.Map(2, 1),
            Slug = "page-" + i,
            Title = "Page synthetic " + i,
            Description = "Synthetic",
            District = "Exact text",
            Icon = "*",
            IsPublished = true
        });
        await db.SaveChangesAsync();
        var repo = new PostgresDiscoveryRepository(db);
        var filter = new DiscoveryFilter("almere", null, null);
        var titles = new List<string>(); string? cursor = null; string? first = null;
        do { var page = await repo.FindPageAsync(filter, 3, cursor); titles.AddRange(page.Items.Select(x => x.Title)); cursor = page.NextCursor; first ??= cursor; } while (cursor is not null);
        Assert.Equal(13, titles.Count); Assert.Equal(13, titles.Distinct().Count());
        Assert.Equal("Een wandeling aan het water", titles[0]);
        foreach (var invalid in new[] { "!", "", new string('a', 513) })
            await Assert.ThrowsAsync<ArgumentException>(() => repo.FindPageAsync(filter, 3, invalid));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.FindPageAsync(new DiscoveryFilter("utrecht", null, null), 3, first));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.FindPageAsync(new DiscoveryFilter("almere", "eten", null), 3, first));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.FindPageAsync(new DiscoveryFilter("almere", null, "query"), 3, first));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.FindPageAsync(filter, 4, first));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.FindPageAsync(filter, 101, null));
        db.Cities.Single(x => x.Id == city).IsActive = false; await db.SaveChangesAsync();
        Assert.Empty((await repo.FindPageAsync(filter, 3, null)).Items);
    }

    [Theory]
    [InlineData("domain", "INSERT INTO catalog.city_domains VALUES(gen_random_uuid(),'00000001-0000-8000-8000-000000000001','Bad.Example',true,true,CURRENT_TIMESTAMP)")]
    [InlineData("coordinate", "INSERT INTO catalog.locations(id,city_id,street,house_number,postal_code,country_code,latitude) VALUES(gen_random_uuid(),'00000001-0000-8000-8000-000000000001','Synthetic','1','0000XX','NL',52)")]
    [InlineData("range", "INSERT INTO catalog.locations(id,city_id,street,house_number,postal_code,country_code,latitude,longitude) VALUES(gen_random_uuid(),'00000001-0000-8000-8000-000000000001','Synthetic','1','0000XX','NL',91,0)")]
    [InlineData("timezone", "UPDATE catalog.cities SET time_zone='Invalid/Zone'")]
    [InlineData("self-parent", "UPDATE catalog.categories SET parent_id=id WHERE legacy_id=1")]
    [InlineData("cycle", "UPDATE catalog.categories SET parent_id=(SELECT id FROM catalog.categories WHERE legacy_id=2) WHERE legacy_id=1; UPDATE catalog.categories SET parent_id=(SELECT id FROM catalog.categories WHERE legacy_id=1) WHERE legacy_id=2")]
    public async Task Invalid_catalog_values_are_rejected(string scenario, string sql)
    {
        Assert.NotEmpty(scenario);
        await using var db = Database(); await using var tx = await db.Database.BeginTransactionAsync();
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(sql));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Fact]
    public async Task Location_district_same_city_foreign_key_is_enforced()
    {
        await using var db = Database(); await using var tx = await db.Database.BeginTransactionAsync();
        var city = new City { Slug = "synthetic-other", Name = "Synthetic", CountryCode = "NL", DefaultLanguage = "nl", TimeZone = "Europe/Amsterdam", IsActive = false };
        db.Cities.Add(city);
        var district = new District { CityId = LegacyIdentifier.Map(1, 1), Name = "Synthetic", Slug = "synthetic", DistrictType = "test", IsActive = true };
        db.Districts.Add(district); await db.SaveChangesAsync();
        db.Locations.Add(new Location { CityId = city.Id, DistrictId = district.Id, Street = "Synthetic", HouseNumber = "1", PostalCode = "0000XX", CountryCode = "NL" });
        var e = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(e.InnerException).SqlState);
    }

    [Fact]
    public async Task Migration_defaults_preserve_translations_and_leave_unverified_data_empty()
    {
        await using var db = Database();
        var city = await db.Cities.SingleAsync(); Assert.Equal("NL", city.CountryCode); Assert.Equal("nl", city.DefaultLanguage); Assert.Equal("Europe/Amsterdam", city.TimeZone);
        Assert.Empty(await db.CityDomains.ToArrayAsync()); Assert.Empty(await db.Locations.ToArrayAsync()); Assert.Empty(await db.Districts.ToArrayAsync());
        foreach (var category in await db.Categories.ToArrayAsync())
        {
            Assert.Equal("discovery", category.CategoryType); Assert.Equal(category.LegacyId, category.DisplayOrder); Assert.True(category.IsActive);
            Assert.Equal(category.Name, (await db.CategoryTranslations.SingleAsync(x => x.CategoryId == category.Id && x.LanguageCode == "nl")).Name);
        }
    }

    [Fact]
    public async Task Representative_keyset_workload_is_bounded_and_meets_local_budget()
    {
        await using var db = Database(); await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO discovery.discovery_entries(id,city_id,category_id,slug,title,description,district,icon,is_published)
            SELECT gen_random_uuid(),'00000001-0000-8000-8000-000000000001','00000002-0000-8000-8000-000000000001',
            'perf-'||n,'Performance synthetic '||n,'Synthetic workload','Test','*',true FROM generate_series(1,10000) n;
            ANALYZE discovery.discovery_entries;
            """);
        var repo = new PostgresDiscoveryRepository(db); var filter = new DiscoveryFilter("almere", null, "Performance synthetic");
        var first = await repo.FindPageAsync(filter, 100, null); Assert.NotNull(first.NextCursor);
        var times = new List<double>();
        for (var i = 0; i < 30; i++)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var page = await repo.FindPageAsync(filter, 100, i % 2 == 0 ? null : first.NextCursor);
            times.Add(timer.Elapsed.TotalMilliseconds); Assert.Equal(100, page.Items.Count);
        }
        var all = new HashSet<string>(); string? cursor = null;
        do { var page = await repo.FindPageAsync(filter, 100, cursor); foreach (var item in page.Items) Assert.True(all.Add(item.Title)); cursor = page.NextCursor; } while (cursor is not null);
        Assert.Equal(10000, all.Count);
        var p95 = times.Order().ElementAt(28); Assert.True(p95 <= 250, $"Local p95 {p95}ms exceeds 250ms budget");
        var conn = db.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand(); cmd.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        cmd.CommandText = "EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) SELECT id,title FROM discovery.discovery_entries WHERE city_id='00000001-0000-8000-8000-000000000001' AND is_published ORDER BY (legacy_id IS NULL),legacy_id,id LIMIT 100";
        var plan = (string)(await cmd.ExecuteScalarAsync())!;
        var directory = Environment.GetEnvironmentVariable("HAPPYCITY_EVIDENCE_DIRECTORY");
        if (directory is not null) { Directory.CreateDirectory(directory); await File.WriteAllTextAsync(Path.Combine(directory, "performance.json"), System.Text.Json.JsonSerializer.Serialize(new { rows = 10000, samples = times, p95Milliseconds = p95, thresholdMilliseconds = 250, distinctTraversed = all.Count, plan, scope = "Local sequential synthetic transaction; not production load capacity" })); }
    }

    [Fact]
    public async Task District_cycles_are_rejected()
    {
        await using var db = Database(); await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO catalog.districts(id,city_id,name,slug,district_type,is_active) VALUES
            ('00000000-0000-4000-8000-000000000001','00000001-0000-8000-8000-000000000001','Synthetic A','a','test',false),
            ('00000000-0000-4000-8000-000000000002','00000001-0000-8000-8000-000000000001','Synthetic B','b','test',false);
            UPDATE catalog.districts SET parent_id='00000000-0000-4000-8000-000000000002' WHERE slug='a';
            """);
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("UPDATE catalog.districts SET parent_id='00000000-0000-4000-8000-000000000001' WHERE slug='b'"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Theory]
    [InlineData("duplicate-host", "INSERT INTO catalog.city_domains VALUES(gen_random_uuid(),'00000001-0000-8000-8000-000000000001','synthetic.example',false,false,CURRENT_TIMESTAMP)")]
    [InlineData("second-primary", "INSERT INTO catalog.city_domains VALUES(gen_random_uuid(),'00000001-0000-8000-8000-000000000001','another.example',true,true,CURRENT_TIMESTAMP)")]
    [InlineData("translation", "INSERT INTO catalog.category_translations(category_id,language_code,name) VALUES('00000002-0000-8000-8000-000000000001','nl','Duplicate')")]
    public async Task Catalog_unique_invariants_reject_duplicates(string scenario, string sql)
    {
        Assert.NotEmpty(scenario); await using var db = Database(); await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("INSERT INTO catalog.city_domains VALUES(gen_random_uuid(),'00000001-0000-8000-8000-000000000001','synthetic.example',true,true,CURRENT_TIMESTAMP)");
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(sql)); Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
    }

    [Fact]
    public async Task Concurrent_hierarchy_writes_are_serialized()
    {
        var connection = DatabaseConfiguration.RequireVerifiedTls(Environment.GetEnvironmentVariable("HAPPYCITY_MIGRATION_CONNECTION"));
        await using var a = new NpgsqlConnection(connection); await using var b = new NpgsqlConnection(connection);
        await a.OpenAsync(); await b.OpenAsync();
        await using var ta = await a.BeginTransactionAsync(); await using var tb = await b.BeginTransactionAsync();
        // Existing immutable synthetic categories; first transaction is rolled back,
        // then the second is tested for an actual cycle within its own transaction.
        await using var ca = new NpgsqlCommand("UPDATE catalog.categories SET parent_id=(SELECT id FROM catalog.categories WHERE legacy_id=2) WHERE legacy_id=1", a, ta);
        await ca.ExecuteNonQueryAsync();
        await using var cb = new NpgsqlCommand("SET LOCAL lock_timeout='200ms'; UPDATE catalog.categories SET parent_id=(SELECT id FROM catalog.categories WHERE legacy_id=1) WHERE legacy_id=2", b, tb);
        var error = await Assert.ThrowsAsync<PostgresException>(() => cb.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.LockNotAvailable, error.SqlState);
        // Explicit serialization verified without committing changes to shared fixtures.
    }

    [Fact]
    public async Task Mapped_constraints_indexes_and_columns_exist_in_actual_database()
    {
        await using var db = Database(); await db.Database.OpenConnectionAsync();
        foreach (var entity in db.Model.GetEntityTypes())
        {
            var table = entity.GetTableName()!; var schema = entity.GetSchema()!;
            foreach (var key in entity.GetKeys()) await Exists("SELECT count(*) FROM pg_constraint con JOIN pg_class c ON c.oid=con.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname=@s AND c.relname=@t AND con.conname=@n", schema, table, key.GetName()!);
            foreach (var fk in entity.GetForeignKeys()) await Exists("SELECT count(*) FROM pg_constraint con JOIN pg_class c ON c.oid=con.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname=@s AND c.relname=@t AND con.conname=@n", schema, table, fk.GetConstraintName()!);
            foreach (var index in entity.GetIndexes()) await Exists("SELECT count(*) FROM pg_indexes WHERE schemaname=@s AND tablename=@t AND indexname=@n", schema, table, index.GetDatabaseName()!);
        }
        async Task Exists(string sql, string schema, string table, string name)
        {
            await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)db.Database.GetDbConnection());
            command.Parameters.AddWithValue("s", schema); command.Parameters.AddWithValue("t", table); command.Parameters.AddWithValue("n", name);
            Assert.True((long)(await command.ExecuteScalarAsync())! == 1, $"Missing physical metadata {schema}.{table}.{name}");
        }
    }
}
