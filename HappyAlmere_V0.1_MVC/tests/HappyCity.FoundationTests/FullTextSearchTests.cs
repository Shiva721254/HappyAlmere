using System.Diagnostics;
using HappyCity.Application;
using HappyCity.Domain;
using HappyCity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace HappyCity.FoundationTests;

public sealed class FullTextSearchModelTests
{
    [Fact]
    public void Model_configures_weighted_dutch_tsvector_column_and_gin_index()
    {
        var options = new DbContextOptionsBuilder<HappyCityDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=happyalmere;SSL Mode=VerifyFull")
            .Options;
        using var db = new HappyCityDbContext(options);
        var entity = db.Model.FindEntityType(typeof(DiscoveryEntry))!;
        var property = entity.FindProperty("SearchVector");
        Assert.NotNull(property);
        Assert.Equal(typeof(NpgsqlTsVector), property!.ClrType);
        Assert.Equal("search_vector", property.GetColumnName());
        Assert.True(property.GetIsStored());
        var sql = property.GetComputedColumnSql()!;
        Assert.Contains("to_tsvector('dutch'", sql);
        Assert.Contains("'A'", sql);
        Assert.Contains("'B'", sql);
        Assert.Contains("'C'", sql);

        var index = entity.GetIndexes().Single(i => i.GetDatabaseName() == "ix_discovery_entries_search_vector");
        Assert.Equal("gin", index.GetMethod());
        Assert.Single(index.Properties);
        Assert.Equal("SearchVector", index.Properties[0].Name);
    }
}

[Trait("Category", "Postgres")]
public sealed class FullTextSearchTests
{
    private static string Connection(bool migration = false) =>
        DatabaseConfiguration.RequireVerifiedTls(Environment.GetEnvironmentVariable(
            migration ? "HAPPYCITY_MIGRATION_CONNECTION" : "ConnectionStrings__HappyCity"));

    private static HappyCityDbContext Database(bool migration = false) =>
        new(new DbContextOptionsBuilder<HappyCityDbContext>().UseNpgsql(Connection(migration)).Options);

    [Theory]
    [InlineData("wandelen", "Een wandeling aan het water")]
    [InlineData("water wandelen", "Een wandeling aan het water")]
    [InlineData("kind", "Een familiedag plannen")]
    [InlineData("familie", "Een familiedag plannen")]
    [InlineData("lunchen", "Lokaal lunchen")]
    [InlineData("buurt winkels", "Ontdek je buurt")]
    public async Task Dutch_full_text_search_supports_stemming_multiword_and_compound_matching(string query, string expectedTitle)
    {
        await using var db = Database();
        var repo = new PostgresDiscoveryRepository(db);
        var results = await repo.FindAsync(new DiscoveryFilter("almere", null, query));
        Assert.Contains(results, item => item.Title == expectedTitle);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("\\")]
    [InlineData("' OR 1=1 --")]
    [InlineData("\"unclosed quote")]
    [InlineData("AND OR NOT")]
    [InlineData("()[]{}:*&|!")]
    [InlineData("-nonexistent")]
    [InlineData("wandeling OR nonexistent")]
    public async Task Adversarial_and_malformed_queries_never_throw_or_leak_rows(string query)
    {
        await using var db = Database();
        var repo = new PostgresDiscoveryRepository(db);
        var results = await repo.FindAsync(new DiscoveryFilter("almere", null, query));
        Assert.Empty(results);
    }

    [Fact]
    public async Task Search_ranks_title_above_district_above_description_above_substring_fallback()
    {
        await using var db = Database(true);
        await using var tx = await db.Database.BeginTransactionAsync();
        var almere = LegacyIdentifier.Map(1, 1);
        var cat = LegacyIdentifier.Map(2, 2);

        // Insert deliberately in reverse priority order to prove ranking drives ordering, not insertion/id order.
        db.DiscoveryEntries.AddRange(
            new DiscoveryEntry { CityId = almere, CategoryId = cat, Slug = "fts-rank-fallback", Title = "Speciaalkoffiebar", Description = "Rustige plek in de buurt", District = "Centrum", Icon = "*", IsPublished = true },
            new DiscoveryEntry { CityId = almere, CategoryId = cat, Slug = "fts-rank-desc", Title = "Lokale bakker", Description = "Verse koffie en broodjes", District = "Haven", Icon = "*", IsPublished = true },
            new DiscoveryEntry { CityId = almere, CategoryId = cat, Slug = "fts-rank-district", Title = "Lokale plek", Description = "Gezellig samenkomen", District = "Koffie kwartier", Icon = "*", IsPublished = true },
            new DiscoveryEntry { CityId = almere, CategoryId = cat, Slug = "fts-rank-title", Title = "Koffie aan het water", Description = "Uitzicht op het meer", District = "Buiten", Icon = "*", IsPublished = true },
            new DiscoveryEntry { CityId = almere, CategoryId = cat, Slug = "fts-rank-draft", Title = "Koffie concept", Description = "Niet gepubliceerd", District = "Stad", Icon = "*", IsPublished = false }
        );
        await db.SaveChangesAsync();

        var repo = new PostgresDiscoveryRepository(db);
        var results = await repo.FindAsync(new DiscoveryFilter("almere", null, "koffie"));
        Assert.Equal(4, results.Count);
        Assert.Equal("Koffie aan het water", results[0].Title);
        Assert.Equal("Lokale plek", results[1].Title);
        Assert.Equal("Lokale bakker", results[2].Title);
        Assert.Equal("Speciaalkoffiebar", results[3].Title);
        Assert.DoesNotContain(results, r => r.Title == "Koffie concept");
    }

    [Fact]
    public async Task Search_vector_column_gin_index_and_selective_fts_latency_are_verified()
    {
        await using var db = Database(true);
        await db.Database.OpenConnectionAsync();

        await using (var colCmd = new NpgsqlCommand(
            "SELECT udt_name, is_generated FROM information_schema.columns WHERE table_schema='discovery' AND table_name='discovery_entries' AND column_name='search_vector'",
            (NpgsqlConnection)db.Database.GetDbConnection()))
        await using (var reader = await colCmd.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal("tsvector", reader.GetString(0));
            Assert.Equal("ALWAYS", reader.GetString(1));
        }

        await using (var idxCmd = new NpgsqlCommand(
            "SELECT indexdef FROM pg_indexes WHERE schemaname='discovery' AND tablename='discovery_entries' AND indexname='ix_discovery_entries_search_vector'",
            (NpgsqlConnection)db.Database.GetDbConnection()))
        {
            var indexDef = (string?)await idxCmd.ExecuteScalarAsync();
            Assert.NotNull(indexDef);
            Assert.Contains("USING gin (search_vector)", indexDef, StringComparison.OrdinalIgnoreCase);
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO discovery.discovery_entries(id,city_id,category_id,slug,title,description,district,icon,is_published)
            SELECT gen_random_uuid(),
                   '00000001-0000-8000-8000-000000000001'::uuid,
                   '00000002-0000-8000-8000-000000000001'::uuid,
                   'fts-bench-' || n,
                   CASE WHEN n <= 5 THEN 'Zeldzame fietstocht ' || n ELSE 'Algemene buurtactiviteit ' || n END,
                   CASE WHEN n <= 5 THEN 'Route langs de Oostvaardersplassen' ELSE 'Reguliere omschrijving voor wijkbewoners' END,
                   'Almere Stad',
                   '*',
                   true
            FROM generate_series(1, 5000) n;
            ANALYZE discovery.discovery_entries;
            """);

        // Verify the GIN index is valid, populated with Dutch lexemes, and directly usable by @@ plainto_tsquery('dutch', ...).
        await db.Database.ExecuteSqlRawAsync("SET LOCAL enable_seqscan = off");
        await using (var planCmd = new NpgsqlCommand(
            "EXPLAIN (ANALYZE, FORMAT TEXT) SELECT id FROM discovery.discovery_entries WHERE search_vector @@ plainto_tsquery('dutch', 'fietstochten')",
            (NpgsqlConnection)db.Database.GetDbConnection(),
            (NpgsqlTransaction)tx.GetDbTransaction()))
        {
            var lines = new List<string>();
            await using var r = await planCmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) lines.Add(r.GetString(0));
            var planText = string.Join('\n', lines);
            Assert.Contains("ix_discovery_entries_search_vector", planText);
        }
        await db.Database.ExecuteSqlRawAsync("SET LOCAL enable_seqscan = on");

        var repo = new PostgresDiscoveryRepository(db);
        // Warmup
        var first = await repo.FindAsync(new DiscoveryFilter("almere", null, "fietstochten"));
        Assert.Equal(5, first.Count);

        var times = new List<double>();
        for (var i = 0; i < 25; i++)
        {
            var sw = Stopwatch.StartNew();
            var page = await repo.FindAsync(new DiscoveryFilter("almere", null, "fietstochten"));
            times.Add(sw.Elapsed.TotalMilliseconds);
            Assert.Equal(5, page.Count);
        }
        var p95 = times.Order().ElementAt(23);
        Assert.True(p95 <= 250, $"Dutch FTS 5000-row p95 {p95:F2}ms exceeded 250ms budget");
    }
}
