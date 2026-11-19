using System.Text.Json;
using HappyCity.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HappyCity.FoundationTests;

[Trait("Category", "Postgres")]
public sealed class IdentityMigrationTests
{
    [Fact]
    public async Task Separate_identity_history_matches_model_and_has_exactly_seven_functional_tables()
    {
        using var services = IdentityPersistenceTests.Services();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyCityIdentityDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Single(db.Database.GetMigrations());
        Assert.All(db.Model.GetEntityTypes(), e => Assert.Equal("identity", e.GetSchema()));
        Assert.Equal(7, db.Model.GetEntityTypes().Count());
        await using var connection = await IdentityPrivilegeTests.Open("HAPPYCITY_TEST_HAPPY_IDENTITY_BACKUP");
        await using var query = new NpgsqlCommand("SELECT count(*) FROM identity.\"__EFMigrationsHistory\"", connection);
        Assert.Equal(1L, await query.ExecuteScalarAsync());
    }

    [Fact]
    public void Independent_rehearsal_proves_rollback_recovery_contract_and_complete_noop()
    {
        var path = Environment.GetEnvironmentVariable("HAPPYCITY_IDENTITY_EVIDENCE");
        Assert.False(string.IsNullOrWhiteSpace(path));
        using var evidence = JsonDocument.Parse(File.ReadAllText(path!));
        Assert.Equal("PASS", evidence.RootElement.GetProperty("status").GetString());
        var checks = evidence.RootElement.GetProperty("checks");
        foreach (var name in new[] { "exactP0001FullIdentityRollback", "independentFreshRecoveryAndReapplication",
            "existingDataMetadataHistoryPrivilegesPreserved", "cleanAndUpgradeBothContextsEqual",
            "independentPhysicalContractFourPaths", "independentContractMismatchControls", "completeIdentityNoOp",
            "explicitAclMembershipGrantOptionsAndExactDenials", "coordinatedSharedStateBothHistoriesSequencesAndAclRestore",
            "retiredRestoreAuthenticationAndAuthorizationDenied" })
            Assert.Equal("PASS", checks.GetProperty(name).GetString());
    }
}
