using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using HappyCity.Application;
using HappyCity.Domain;
using HappyCity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HappyCity.FoundationTests;

[Trait("Category", "Postgres")]
public sealed class QueryEvidenceTests
{
    private sealed record Captured(string Sql, NpgsqlParameter[] Parameters);
    private sealed class Capture : DbCommandInterceptor
    {
        public Captured? Latest { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Latest = new Captured(command.CommandText, command.Parameters.Cast<NpgsqlParameter>().Select(p => p.Clone()).ToArray());
            return ValueTask.FromResult(result);
        }
    }
    private sealed class Messages : ILogger<SafeQueryTimingInterceptor>
    {
        public List<string> Values { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Values.Add(formatter(state, exception));
    }

    [Fact]
    public void Slow_monitor_is_thresholded_rate_bounded_and_contains_no_query_data()
    {
        var messages = new Messages(); var monitor = new SafeQueryTimingInterceptor(messages);
        monitor.Record(TimeSpan.FromMilliseconds(249)); Assert.Empty(messages.Values);
        monitor.Record(TimeSpan.FromMilliseconds(251)); monitor.Record(TimeSpan.FromMilliseconds(999));
        Assert.Single(messages.Values); Assert.Equal("Database read exceeded local threshold: 251 ms", messages.Values[0]);
    }

    [Fact]
    public async Task Actual_repository_queries_cover_deep_filtered_mixed_state_workloads()
    {
        var capture = new Capture();
        var connection = DatabaseConfiguration.RequireVerifiedTls(Environment.GetEnvironmentVariable("HAPPYCITY_MIGRATION_CONNECTION"));
        await using var db = new HappyCityDbContext(new DbContextOptionsBuilder<HappyCityDbContext>().UseNpgsql(connection).AddInterceptors(capture).Options);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO catalog.cities(id,slug,name,country_code,default_language,time_zone,is_active) VALUES
            ('10000000-0000-4000-8000-000000000001','r01-other','Synthetic','NL','nl','Europe/Amsterdam',true),
            ('10000000-0000-4000-8000-000000000002','r01-inactive','Synthetic','NL','nl','Europe/Amsterdam',false);
            INSERT INTO catalog.categories(id,slug,name,category_type,display_order,is_active) VALUES
            ('10000000-0000-4000-8000-000000000003','r01-inactive','Synthetic','discovery',0,false);
            INSERT INTO discovery.discovery_entries(id,city_id,category_id,slug,title,description,district,icon,is_published)
            SELECT gen_random_uuid(),
            CASE WHEN n%10=0 THEN '10000000-0000-4000-8000-000000000001'::uuid WHEN n%10=1 THEN '10000000-0000-4000-8000-000000000002'::uuid ELSE '00000001-0000-8000-8000-000000000001'::uuid END,
            CASE WHEN n%11=0 THEN '10000000-0000-4000-8000-000000000003'::uuid WHEN n%2=0 THEN '00000002-0000-8000-8000-000000000001'::uuid ELSE '00000002-0000-8000-8000-000000000002'::uuid END,
            'r01-perf-'||n,'R01 synthetic '||n,CASE WHEN n%7=0 THEN 'needle synthetic' ELSE 'ordinary synthetic' END,'Synthetic','*',n%13<>0
            FROM generate_series(1,10000) n;
            ANALYZE discovery.discovery_entries;
            """);
        var repo = new PostgresDiscoveryRepository(db);
        var evidence = new List<object>();
        foreach (var filter in new[] { new DiscoveryFilter("almere", null, "R01 synthetic"), new DiscoveryFilter("almere", "activiteiten", "needle synthetic"), new DiscoveryFilter("r01-other", null, "R01 synthetic"), new DiscoveryFilter("r01-inactive", null, "R01 synthetic"), new DiscoveryFilter("almere", "r01-inactive", null) })
        {
            string? cursor = null; var checkpoints = new List<string?> { null }; var titles = new HashSet<string>();
            do
            {
                var page = await repo.FindPageAsync(filter, 50, cursor); foreach (var item in page.Items) Assert.True(titles.Add(item.Title)); cursor = page.NextCursor;
                if (cursor is not null) checkpoints.Add(cursor);
            } while (cursor is not null);
            // Independent SQL population count, not a repetition of keyset logic.
            await using var count = new NpgsqlCommand("SELECT count(*) FROM discovery.discovery_entries e JOIN catalog.cities c ON c.id=e.city_id JOIN catalog.categories k ON k.id=e.category_id WHERE c.slug=@city AND c.is_active AND k.is_active AND e.is_published AND (@category IS NULL OR k.slug=@category) AND (@query IS NULL OR e.title||' '||e.description||' '||e.district ILIKE '%'||@query||'%')", (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)tx.GetDbTransaction());
            count.Parameters.AddWithValue("city", filter.CitySlug); count.Parameters.Add(new NpgsqlParameter("category", NpgsqlTypes.NpgsqlDbType.Text) { Value = (object?)filter.Category ?? DBNull.Value }); count.Parameters.Add(new NpgsqlParameter("query", NpgsqlTypes.NpgsqlDbType.Text) { Value = (object?)filter.Query ?? DBNull.Value });
            Assert.Equal((long)(await count.ExecuteScalarAsync())!, titles.Count);
            foreach (var index in new[] { 0, checkpoints.Count / 2, checkpoints.Count - 1 }.Distinct())
            {
                var position = checkpoints[index]; await repo.FindPageAsync(filter, 50, position); var times = new List<double>();
                for (var i = 0; i < 30; i++) { var timer = Stopwatch.StartNew(); await repo.FindPageAsync(filter, 50, position); times.Add(timer.Elapsed.TotalMilliseconds); }
                var captured = capture.Latest!; Assert.Contains("JOIN", captured.Sql, StringComparison.OrdinalIgnoreCase);
                if (position is not null) Assert.Contains("COALESCE", captured.Sql, StringComparison.OrdinalIgnoreCase);
                await using var plan = new NpgsqlCommand("EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) " + captured.Sql, (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)tx.GetDbTransaction());
                plan.Parameters.AddRange(captured.Parameters.Select(p => p.Clone()).ToArray());
                var planJson = (string)(await plan.ExecuteScalarAsync())!;
                var p95 = times.Order().ElementAt(28); Assert.True(p95 <= 250, $"Local workload p95 {p95} exceeds 250ms");
                evidence.Add(new { syntheticScenario = new { filter.CitySlug, filter.Category, filter.Query }, pageIndex = index, pages = checkpoints.Count, distinctRows = titles.Count, p95Milliseconds = p95, thresholdMilliseconds = 250, samples = times, sql = captured.Sql, parameterTypes = captured.Parameters.Select(p => new { p.ParameterName, Type = p.NpgsqlDbType.ToString() }), plan = JsonDocument.Parse(planJson).RootElement.Clone() });
            }
        }
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.FindPageAsync(new DiscoveryFilter("almere", null, null), 50, null, cancelled.Token));
        var directory = Environment.GetEnvironmentVariable("HAPPYCITY_EVIDENCE_DIRECTORY"); if (directory is not null) { Directory.CreateDirectory(directory); await File.WriteAllTextAsync(Path.Combine(directory, "r01-query-plans.json"), JsonSerializer.Serialize(new { rows = 10000, scope = "Actual EF commands and bound synthetic parameters, local sequential workload; not production SLA", workloads = evidence }, new JsonSerializerOptions { WriteIndented = true })); }
    }
}
