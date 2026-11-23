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

    [Fact]
    public void R01_independent_authority_matrix_matches_on_all_four_paths()
    {
        using var evidence = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("HAPPYCITY_IDENTITY_EVIDENCE")!));
        var root = evidence.RootElement;
        var paths = root.GetProperty("r01AuthorityPaths");
        Assert.Equal(4, paths.EnumerateObject().Count());
        var digest = paths.GetProperty("clean").GetProperty("sha256").GetString();
        foreach (var name in new[] { "clean", "upgrade", "recovered", "restored" })
        {
            var path = paths.GetProperty(name);
            Assert.Equal("PASS", path.GetProperty("status").GetString());
            Assert.Equal(digest, path.GetProperty("sha256").GetString());
            Assert.True(path.GetProperty("dimensions").GetProperty("effective").GetInt32() > 1000);
        }
        var reference = root.GetProperty("r01AuthorityReference");
        foreach (var dimension in new[] { "roles", "memberships", "paths", "objects", "acl", "effective", "ownership", "routines", "defaults" })
            Assert.Equal(JsonValueKind.Array, reference.GetProperty(dimension).ValueKind);
        var mutations = root.GetProperty("r01Mutations").EnumerateArray().ToArray();
        Assert.True(mutations.Length >= 70);
        foreach (var mutation in mutations)
        {
            Assert.Equal("PASS", mutation.GetProperty("status").GetString());
            Assert.Equal(digest, mutation.GetProperty("cleanupAuthoritySha256").GetString());
            Assert.Contains(mutation.GetProperty("actualMismatch").EnumerateArray(),
                actual => actual.GetString() == mutation.GetProperty("expectedMismatch").GetString());
        }
        foreach (var scenario in Enumerable.Range(6, 27).Select(n => $"T{n:00}"))
            Assert.Contains(mutations, mutation => mutation.GetProperty("scenario").GetString() == scenario);
    }
}
