using HappyCity.Infrastructure;
using HappyCity.Infrastructure.Identity;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HappyCity.FoundationTests;

[Trait("Category", "Postgres")]
public sealed class IdentityPrivilegeTests
{
    [Fact]
    public async Task Runtime_rejects_actual_insufficient_authority_despite_valid_connection_configuration()
    {
        await using var migration = await Open("HAPPYCITY_IDENTITY_MIGRATION_CONNECTION");
        using var services = IdentityPersistenceTests.Services();
        using (var positive = services.CreateScope())
            await positive.ServiceProvider.GetRequiredService<HappyCityIdentityDbContext>().Users.CountAsync();
        try
        {
            await using var revoke = new NpgsqlCommand("REVOKE INSERT ON identity.users FROM happy_identity_app", migration);
            await revoke.ExecuteNonQueryAsync();
            using var negative = services.CreateScope();
            var database = negative.ServiceProvider.GetRequiredService<HappyCityIdentityDbContext>();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => database.Users.CountAsync());
            Assert.Equal("Identity runtime database authority verification failed.", error.Message);
        }
        finally
        {
            await using var restore = new NpgsqlCommand("GRANT INSERT ON identity.users TO happy_identity_app", migration);
            await restore.ExecuteNonQueryAsync();
        }
        using var recovered = services.CreateScope();
        await recovered.ServiceProvider.GetRequiredService<HappyCityIdentityDbContext>().Users.CountAsync();
    }
    internal static async Task<NpgsqlConnection> Open(string variable)
    {
        var connection = new NpgsqlConnection(DatabaseConfiguration.RequireVerifiedTls(Environment.GetEnvironmentVariable(variable)));
        await connection.OpenAsync();
        // pg_stat_ssl exposes connection details to the authenticated login, not
        // an owner selected by PGOPTIONS. Verify TLS as that login, then restore
        // the intended migration execution role before exercising its rights.
        await using var tls = new NpgsqlCommand("SET ROLE NONE; SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid()", connection);
        Assert.Equal(true, await tls.ExecuteScalarAsync());
        var role = variable switch
        {
            "HAPPYCITY_MIGRATION_CONNECTION" => "happy_owner",
            "HAPPYCITY_IDENTITY_MIGRATION_CONNECTION" => "happy_identity_owner",
            _ => null
        };
        if (role is not null)
        {
            await using var restore = new NpgsqlCommand("SET ROLE " + role, connection);
            await restore.ExecuteNonQueryAsync();
        }
        return connection;
    }

    [Theory]
    [InlineData("ConnectionStrings__HappyCityIdentity", "SELECT count(*) FROM identity.users", "SELECT * FROM catalog.cities")]
    [InlineData("ConnectionStrings__HappyCityIdentity", "SELECT count(*) FROM identity.users", "UPDATE catalog.cities SET name=name WHERE false")]
    [InlineData("ConnectionStrings__HappyCityIdentity", "SELECT count(*) FROM identity.users", "DELETE FROM identity.\"__EFMigrationsHistory\" WHERE false")]
    [InlineData("ConnectionStrings__HappyCityIdentity", "SELECT count(*) FROM identity.users", "CREATE TABLE identity.denied(id integer)")]
    [InlineData("ConnectionStrings__HappyCityIdentity", "SELECT count(*) FROM identity.users", "SET ROLE happy_owner")]
    [InlineData("ConnectionStrings__HappyCityIdentity", "SELECT count(*) FROM identity.users", "SET ROLE happy_identity_owner")]
    [InlineData("ConnectionStrings__HappyCityIdentity", "SELECT count(*) FROM identity.users", "SELECT * FROM identity.\"__EFMigrationsHistory\"")]
    [InlineData("ConnectionStrings__HappyCityIdentity", "SELECT count(*) FROM identity.users", "SELECT setval('identity.user_claims_id_seq',100)")]
    [InlineData("ConnectionStrings__HappyCity", "SELECT count(*) FROM catalog.cities", "SELECT * FROM identity.users")]
    [InlineData("HAPPYCITY_TEST_HAPPY_BACKUP", "SELECT count(*) FROM catalog.cities", "SELECT * FROM identity.users")]
    [InlineData("HAPPYCITY_TEST_HAPPY_IDENTITY_BACKUP", "SELECT count(*) FROM identity.users", "SELECT * FROM discovery.discovery_entries")]
    [InlineData("HAPPYCITY_TEST_HAPPY_IDENTITY_BACKUP", "SELECT count(*) FROM identity.users", "DELETE FROM identity.users WHERE false")]
    [InlineData("HAPPYCITY_TEST_HAPPY_IDENTITY_BACKUP", "SELECT count(*) FROM identity.users", "SELECT nextval('identity.user_claims_id_seq')")]
    [InlineData("HAPPYCITY_MIGRATION_CONNECTION", "SELECT count(*) FROM catalog.cities", "SET ROLE happy_identity_owner")]
    [InlineData("HAPPYCITY_IDENTITY_MIGRATION_CONNECTION", "SELECT count(*) FROM identity.users", "SET ROLE happy_owner")]
    public async Task Positive_control_precedes_exact_authorization_denial(string variable, string positive, string denied)
    {
        await using var connection = await Open(variable);
        await using var control = new NpgsqlCommand(positive, connection);
        await control.ExecuteScalarAsync();
        await using var command = new NpgsqlCommand(denied, connection);
        var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("42501", error.SqlState);
    }

    [Fact]
    public async Task Retired_identity_restore_login_denied_after_live_tls_control()
    {
        await using var control = await Open("HAPPYCITY_IDENTITY_RESTORE_CONTROL");
        await using var connection = new NpgsqlConnection(DatabaseConfiguration.RequireVerifiedTls(
            Environment.GetEnvironmentVariable("HAPPYCITY_IDENTITY_RETIRED_CONNECTION")));
        var error = await Assert.ThrowsAsync<PostgresException>(() => connection.OpenAsync());
        Assert.Equal("28000", error.SqlState);
        Assert.Equal("role \"happy_identity_restore\" is not permitted to log in", error.MessageText);
    }

    [Theory]
    [InlineData("ConnectionStrings__HappyCity")]
    [InlineData("HAPPYCITY_IDENTITY_MIGRATION_CONNECTION")]
    [InlineData("HAPPYCITY_TEST_HAPPY_IDENTITY_BACKUP")]
    public void Privileged_or_wrong_identity_configuration_is_rejected(string variable) =>
        Assert.Throws<InvalidOperationException>(() => IdentityConfiguration.RequireRuntimeConnection(Environment.GetEnvironmentVariable(variable)));
}

// The original tests above are unchanged. R01 adds connection lifecycle evidence
// without exposing production test hooks or introducing a mocking dependency.
public sealed class IdentityRuntimeSafetyTests
{
    internal static DbConnectionInterceptor Interceptor() => (DbConnectionInterceptor)Activator.CreateInstance(
        typeof(IdentityConfiguration).GetNestedType("RuntimeAuthorityInterceptor", BindingFlags.NonPublic)!, true)!;

    internal static string AuthoritySql => (string)typeof(IdentityConfiguration)
        .GetNestedType("RuntimeAuthorityInterceptor", BindingFlags.NonPublic)!
        .GetField("Sql", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;

    [Theory]
    [InlineData(false, "false", false)]
    [InlineData(true, "false", false)]
    [InlineData(false, "error", false)]
    [InlineData(true, "error", false)]
    [InlineData(false, "error", true)]
    [InlineData(true, "error", true)]
    [InlineData(true, "cancel", false)]
    [InlineData(true, "cancel", true)]
    public async Task Failure_closes_or_disposes_without_exposing_diagnostics(bool asynchronous, string result, bool closeFails)
    {
        using var cancellation = new CancellationTokenSource();
        using var connection = new ProbeConnection(() =>
        {
            if (result == "cancel") { cancellation.Cancel(); cancellation.Token.ThrowIfCancellationRequested(); }
            if (result == "error") throw new InvalidOperationException("synthetic-private-diagnostic");
            return false;
        }, closeFails);
        var interceptor = Interceptor();
        if (result == "cancel")
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => interceptor.ConnectionOpenedAsync(connection, null!, cancellation.Token));
        else
        {
            var error = asynchronous
                ? await Assert.ThrowsAsync<InvalidOperationException>(() => interceptor.ConnectionOpenedAsync(connection, null!))
                : Assert.Throws<InvalidOperationException>(() => interceptor.ConnectionOpened(connection, null!));
            Assert.Equal("Identity runtime database authority verification failed.", error.Message);
            Assert.Null(error.InnerException);
        }
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Equal(1, connection.Executions);
        Assert.Equal(closeFails, connection.WasDisposed);
        Assert.Equal(5, connection.LastTimeout);
    }

    [Fact]
    public async Task Already_cancelled_open_closes_without_a_query()
    {
        using var connection = new ProbeConnection(() => true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Interceptor().ConnectionOpenedAsync(connection, null!, new CancellationToken(true)));
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Equal(0, connection.Executions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Every_reopen_checks_once_without_caching(bool asynchronous)
    {
        var allowed = true;
        using var connection = new ProbeConnection(() => allowed);
        var interceptor = Interceptor();
        for (var i = 0; i < 3; i++)
        {
            connection.Open();
            if (asynchronous) await interceptor.ConnectionOpenedAsync(connection, null!);
            else interceptor.ConnectionOpened(connection, null!);
            Assert.Equal(i + 1, connection.Executions);
            connection.Close();
        }
        allowed = false;
        connection.Open();
        if (asynchronous) await Assert.ThrowsAsync<InvalidOperationException>(() => interceptor.ConnectionOpenedAsync(connection, null!));
        else Assert.Throws<InvalidOperationException>(() => interceptor.ConnectionOpened(connection, null!));
        Assert.Equal(4, connection.Executions);
        Assert.Equal(ConnectionState.Closed, connection.State);
    }

    [Theory]
    [InlineData("Username=happy_identity_backup;SSL Mode=VerifyFull")]
    [InlineData("Username=happy_identity_app;SSL Mode=Require")]
    [InlineData("Username=happy_identity_app;SSL Mode=VerifyCA")]
    [InlineData("Username=happy_identity_app;SSL Mode=Disable")]
    [InlineData("Username=happy_identity_app;SSL Mode=VerifyFull;Options=-c role=happy_identity_owner")]
    public void Configuration_drift_is_rejected(string settings) =>
        Assert.Throws<InvalidOperationException>(() => IdentityConfiguration.RequireRuntimeConnection("Host=localhost;" + settings));

    private sealed class ProbeConnection(Func<object?> result, bool closeFails = false) : DbConnection
    {
        private ConnectionState state = ConnectionState.Open;
        public int Executions { get; private set; }
        public int LastTimeout { get; private set; }
        public bool WasDisposed { get; private set; }
        [AllowNull] public override string ConnectionString { get; set; } = "Host=localhost;Username=happy_identity_app;SSL Mode=VerifyFull";
        public override string Database => "synthetic";
        public override string DataSource => "localhost";
        public override string ServerVersion => "18";
        public override ConnectionState State => state;
        public override void Open() => state = ConnectionState.Open;
        public override void Close() { if (closeFails) throw new InvalidOperationException("synthetic-close-failure"); state = ConnectionState.Closed; }
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new ProbeCommand(this);
        protected override void Dispose(bool disposing) { WasDisposed = true; state = ConnectionState.Closed; }
        private object? Execute(string sql, int timeout)
        {
            Assert.Equal(AuthoritySql, sql);
            Executions++; LastTimeout = timeout;
            return result();
        }

        private sealed class ProbeCommand(ProbeConnection parent) : DbCommand
        {
            [AllowNull] public override string CommandText { get; set; } = "";
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; } = parent;
            protected override DbTransaction? DbTransaction { get; set; }
            protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
            public override void Cancel() { }
            public override void Prepare() { }
            public override int ExecuteNonQuery() => throw new NotSupportedException();
            protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
            public override object? ExecuteScalar()
            {
                return parent.Execute(CommandText, CommandTimeout);
            }
            public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) => Task.FromResult(ExecuteScalar());
        }
    }
}

[Trait("Category", "Postgres")]
public sealed class IdentityRuntimePostgresTests
{
    public static IEnumerable<object[]> DmlMutations()
    {
        foreach (var table in new[] { "users", "roles", "user_roles", "user_claims", "role_claims", "user_logins", "user_tokens" })
            foreach (var privilege in new[] { "SELECT", "INSERT", "UPDATE", "DELETE" })
                foreach (var asynchronous in new[] { false, true })
                    yield return new object[] { table, privilege, asynchronous };
    }

    [Theory]
    [MemberData(nameof(DmlMutations))]
    public async Task All_table_dml_drift_rejects_and_closes_on_reopen(string table, string privilege, bool asynchronous)
    {
        await using var owner = await IdentityPrivilegeTests.Open("HAPPYCITY_IDENTITY_MIGRATION_CONNECTION");
        using var services = IdentityPersistenceTests.Services();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyCityIdentityDbContext>();
        await db.Database.OpenConnectionAsync();
        await db.Users.CountAsync();
        await db.Database.CloseConnectionAsync();
        try
        {
            await using var revoke = new NpgsqlCommand($"REVOKE {privilege} ON identity.{table} FROM happy_identity_app", owner);
            await revoke.ExecuteNonQueryAsync();
            var error = asynchronous
                ? await Assert.ThrowsAsync<InvalidOperationException>(() => db.Users.CountAsync())
                : Assert.Throws<InvalidOperationException>(() => db.Users.Count());
            Assert.Equal("Identity runtime database authority verification failed.", error.Message);
            Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);
        }
        finally
        {
            await using var grant = new NpgsqlCommand($"GRANT {privilege} ON identity.{table} TO happy_identity_app", owner);
            await grant.ExecuteNonQueryAsync();
        }
        await db.Users.CountAsync();
        Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_wrong_authenticated_role_fails_the_runtime_statement(bool asynchronous)
    {
        await using var connection = await IdentityPrivilegeTests.Open("HAPPYCITY_TEST_HAPPY_IDENTITY_BACKUP");
        await using var command = new NpgsqlCommand(IdentityRuntimeSafetyTests.AuthoritySql, connection);
        Assert.Equal(false, asynchronous ? await command.ExecuteScalarAsync() : command.ExecuteScalar());
        // Actual callback also closes the already-open incorrectly configured connection.
        if (asynchronous)
            await Assert.ThrowsAsync<InvalidOperationException>(() => IdentityRuntimeSafetyTests.Interceptor().ConnectionOpenedAsync(connection, null!));
        else Assert.Throws<InvalidOperationException>(() => IdentityRuntimeSafetyTests.Interceptor().ConnectionOpened(connection, null!));
        Assert.Equal(ConnectionState.Closed, connection.State);
    }

    [Fact]
    public async Task Actual_cancelled_callback_closes_pooled_connection_and_next_open_verifies()
    {
        await using var connection = await IdentityPrivilegeTests.Open("ConnectionStrings__HappyCityIdentity");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => IdentityRuntimeSafetyTests.Interceptor()
            .ConnectionOpenedAsync(connection, null!, new CancellationToken(true)));
        Assert.Equal(ConnectionState.Closed, connection.State);
        await connection.OpenAsync();
        await IdentityRuntimeSafetyTests.Interceptor().ConnectionOpenedAsync(connection, null!);
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Authority_overhead_has_forty_warmed_samples_and_meets_approved_target(bool asynchronous)
    {
        const int samples = 40, warmups = 5;
        var authority = new List<double>(); var open = new List<double>(); var total = new List<double>();
        var settings = IdentityConfiguration.RequireRuntimeConnection(Environment.GetEnvironmentVariable("ConnectionStrings__HappyCityIdentity"));
        var commands = 0;
        for (var iteration = -warmups; iteration < samples; iteration++)
        {
            await using var connection = new NpgsqlConnection(settings);
            var entire = Stopwatch.StartNew(); var stage = Stopwatch.StartNew();
            if (asynchronous) await connection.OpenAsync(); else connection.Open();
            var opening = stage.Elapsed.TotalMilliseconds;
            await using var command = new NpgsqlCommand(IdentityRuntimeSafetyTests.AuthoritySql, connection) { CommandTimeout = 5 };
            stage.Restart();
            Assert.Equal(true, asynchronous ? await command.ExecuteScalarAsync() : command.ExecuteScalar());
            var checking = stage.Elapsed.TotalMilliseconds;
            await using var operation = new NpgsqlCommand("SELECT count(*) FROM identity.users", connection);
            if (asynchronous) await operation.ExecuteScalarAsync(); else operation.ExecuteScalar();
            if (iteration >= 0)
            {
                authority.Add(checking); open.Add(opening); total.Add(entire.Elapsed.TotalMilliseconds); commands++;
            }
        }
        static object Statistics(List<double> values)
        {
            var ordered = values.Order().ToArray();
            double Percentile(double p) => ordered[(int)Math.Ceiling(p * ordered.Length) - 1];
            return new { samples = values, p50 = Percentile(.50), p95 = Percentile(.95), p99 = Percentile(.99), max = ordered[^1] };
        }
        var p95 = authority.Order().ElementAt((int)Math.Ceiling(samples * .95) - 1);
        var path = Environment.GetEnvironmentVariable("HAPPYCITY_IDENTITY_EVIDENCE");
        Assert.False(string.IsNullOrWhiteSpace(path));
        var evidence = JsonNode.Parse(File.ReadAllText(path!))!.AsObject();
        var metrics = evidence["r01RuntimeOverhead"] as JsonObject ?? new JsonObject();
        evidence["r01RuntimeOverhead"] = metrics;
        metrics[asynchronous ? "async-pooled" : "sync-pooled"] = JsonSerializer.SerializeToNode(new
        {
            status = p95 <= 25 ? "PASS" : "FAIL", warmedSamples = samples, warmups, authorityCommands = commands,
            commandsPerOpen = 1, errors = 0, timeouts = 0, targetP95Milliseconds = 25,
            authorityQuery = Statistics(authority), connectionOpen = Statistics(open), openAuthorityAndRead = Statistics(total),
            scope = "Synthetic localhost, pooled sequential opens; open includes pool checkout (TLS handshake only on a physical miss); not capacity evidence."
        });
        File.WriteAllText(path!, evidence.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        Assert.True(p95 <= 25, $"R01-CL07 target not met: {p95:F4} ms; stop and request amendment, do not relax 25 ms.");
    }
}
