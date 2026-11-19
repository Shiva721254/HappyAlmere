using HappyCity.Infrastructure;
using HappyCity.Infrastructure.Identity;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
