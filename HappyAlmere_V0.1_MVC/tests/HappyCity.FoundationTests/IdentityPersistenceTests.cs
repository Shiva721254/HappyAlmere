using System.Security.Claims;
using HappyCity.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HappyCity.FoundationTests;

[Trait("Category", "Postgres")]
public sealed class IdentityPersistenceTests
{
    internal static ServiceProvider Services() => new ServiceCollection().AddLogging()
        .AddIdentityFoundation(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["IdentityFoundation:Enabled"] = "true",
            ["ConnectionStrings:HappyCityIdentity"] = Environment.GetEnvironmentVariable("ConnectionStrings__HappyCityIdentity")
        }).Build()).BuildServiceProvider();

    private static ApplicationUser User() => new() { Email = Guid.NewGuid().ToString("N") + "@example.test" };

    [Theory]
    [InlineData("UPDATE identity.users SET email=NULL WHERE id=@id", "23502")]
    [InlineData("UPDATE identity.users SET security_stamp=NULL WHERE id=@id", "23502")]
    [InlineData("UPDATE identity.users SET concurrency_stamp=NULL WHERE id=@id", "23502")]
    [InlineData("UPDATE identity.users SET normalized_email=' ' WHERE id=@id", "23514")]
    [InlineData("UPDATE identity.users SET normalized_user_name='' WHERE id=@id", "23514")]
    [InlineData("UPDATE identity.users SET access_failed_count=-1 WHERE id=@id", "23514")]
    [InlineData("UPDATE identity.users SET id='00000000-0000-0000-0000-000000000000' WHERE id=@id", "23514")]
    [InlineData("UPDATE identity.users SET id='53d96df5-475e-4b96-01f6-f4b683b37d68' WHERE id=@id", "23514")]
    [InlineData("UPDATE identity.users SET email=repeat('x',257) WHERE id=@id", "22001")]
    [InlineData("INSERT INTO identity.user_roles VALUES (@id,'53d96df5-475e-4b96-a1f6-f4b683b37d68')", "23503")]
    [InlineData("INSERT INTO identity.user_claims(user_id) VALUES ('53d96df5-475e-4b96-a1f6-f4b683b37d68')", "23503")]
    public async Task Physical_constraints_reject_exact_invalid_operation(string sql, string state)
    {
        using var services = Services();
        using var scope = services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = User();
        Assert.True((await manager.CreateAsync(user)).Succeeded);
        try
        {
            await using var connection = await IdentityPrivilegeTests.Open("ConnectionStrings__HappyCityIdentity");
            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("id", user.Id);
            var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal(state, error.SqlState);
            await transaction.RollbackAsync();
        }
        finally { await manager.DeleteAsync(user); }
    }

    [Fact]
    public async Task Framework_managers_round_trip_hash_normalize_and_cascade_all_stores()
    {
        using var services = Services();
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var db = scope.ServiceProvider.GetRequiredService<HappyCityIdentityDbContext>();
        var user = User();
        var role = new ApplicationRole { Name = "Synthetic-" + Guid.NewGuid().ToString("N") };
        var password = Guid.NewGuid().ToString("N") + "aA!9";
        Assert.Equal('4', user.Id.ToString()[14]);
        Assert.Contains(user.Id.ToString()[19], "89ab");
        Assert.Equal(user.Id.ToString("N"), user.UserName);
        Assert.True((await users.CreateAsync(user, password)).Succeeded);
        Assert.True((await roles.CreateAsync(role)).Succeeded);
        try
        {
            Assert.Equal(user.Email!.ToUpperInvariant(), user.NormalizedEmail);
            Assert.Equal(user.UserName!.ToUpperInvariant(), user.NormalizedUserName);
            Assert.Equal(user.Id, (await users.FindByEmailAsync(user.Email.ToUpperInvariant()))!.Id);
            Assert.Equal(user.Id, (await users.FindByNameAsync(user.UserName.ToUpperInvariant()))!.Id);
            Assert.True(user.PasswordHash is not null && user.PasswordHash != password);
            Assert.True(await users.CheckPasswordAsync(user, password));
            Assert.False(await users.CheckPasswordAsync(user, password + "wrong"));
            Assert.True((await users.AddToRoleAsync(user, role.Name!)).Succeeded);
            Assert.False((await users.AddToRoleAsync(user, role.Name!)).Succeeded);
            Assert.True((await users.AddClaimAsync(user, new Claim("synthetic", "fixture"))).Succeeded);
            Assert.True((await roles.AddClaimAsync(role, new Claim("synthetic", "fixture"))).Succeeded);
            Assert.True((await users.AddLoginAsync(user, new UserLoginInfo("Synthetic", user.Id.ToString(), "Synthetic"))).Succeeded);
            Assert.True((await users.SetAuthenticationTokenAsync(user, "Synthetic", "fixture", "not-a-credential")).Succeeded);
            Assert.Single(await users.GetClaimsAsync(user));
            Assert.Single(await roles.GetClaimsAsync(role));
            Assert.Single(await users.GetLoginsAsync(user));
            Assert.Equal("not-a-credential", await users.GetAuthenticationTokenAsync(user, "Synthetic", "fixture"));
            user.PhoneNumber = "synthetic";
            Assert.True((await users.UpdateAsync(user)).Succeeded);
            Assert.False((await roles.CreateAsync(new ApplicationRole { Name = role.Name })).Succeeded);
            Assert.False((await users.CreateAsync(new ApplicationUser { Email = user.Email })).Succeeded);
            Assert.False((await users.CreateAsync(new ApplicationUser { UserName = user.UserName, Email = User().Email })).Succeeded);
            Assert.True((await users.DeleteAsync(user)).Succeeded);
            Assert.False(await db.UserRoles.AnyAsync(x => x.UserId == user.Id));
            Assert.False(await db.UserClaims.AnyAsync(x => x.UserId == user.Id));
            Assert.False(await db.UserLogins.AnyAsync(x => x.UserId == user.Id));
            Assert.False(await db.UserTokens.AnyAsync(x => x.UserId == user.Id));
            Assert.True((await roles.DeleteAsync(role)).Succeeded);
            Assert.False(await db.RoleClaims.AnyAsync(x => x.RoleId == role.Id));
        }
        finally
        {
            await db.Users.Where(x => x.Id == user.Id).ExecuteDeleteAsync();
            await db.Roles.Where(x => x.Id == role.Id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Stale_framework_concurrency_stamp_rejects_update()
    {
        using var services = Services();
        using var first = services.CreateScope();
        using var second = services.CreateScope();
        var manager = first.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var other = second.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = User();
        Assert.True((await manager.CreateAsync(user)).Succeeded);
        try
        {
            var stale = (await other.FindByIdAsync(user.Id.ToString()))!;
            user.PhoneNumber = "first";
            Assert.True((await manager.UpdateAsync(user)).Succeeded);
            stale.PhoneNumber = "stale";
            var result = await other.UpdateAsync(stale);
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Code == "ConcurrencyFailure");
        }
        finally { await manager.DeleteAsync(user); }
    }

    [Fact]
    public async Task Physical_role_and_relationship_invariants_and_role_cascade()
    {
        using var services = Services();
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var db = scope.ServiceProvider.GetRequiredService<HappyCityIdentityDbContext>();
        var user = User();
        var role = new ApplicationRole { Name = "Synthetic-" + Guid.NewGuid().ToString("N") };
        Assert.True((await users.CreateAsync(user)).Succeeded);
        Assert.True((await roles.CreateAsync(role)).Succeeded);
        Assert.Equal('4', role.Id.ToString()[14]);
        Assert.Contains(role.Id.ToString()[19], "89ab");
        try
        {
            Assert.True((await users.AddToRoleAsync(user, role.Name!)).Succeeded);
            Assert.True((await roles.AddClaimAsync(role, new Claim("synthetic", "fixture"))).Succeeded);
            await using var connection = await IdentityPrivilegeTests.Open("ConnectionStrings__HappyCityIdentity");
            foreach (var (sql, state) in new[]
            {
                ("INSERT INTO identity.user_roles VALUES (@user,@role)", "23505"),
                ("INSERT INTO identity.roles SELECT @new,name,normalized_name,concurrency_stamp FROM identity.roles WHERE id=@role", "23505"),
                ("UPDATE identity.roles SET normalized_name='' WHERE id=@role", "23514"),
                ("UPDATE identity.roles SET concurrency_stamp=NULL WHERE id=@role", "23502"),
                ("UPDATE identity.roles SET id='00000000-0000-0000-0000-000000000000' WHERE id=@role", "23514")
            })
            {
                await using var transaction = await connection.BeginTransactionAsync();
                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("user", user.Id);
                command.Parameters.AddWithValue("role", role.Id);
                command.Parameters.AddWithValue("new", Guid.NewGuid());
                var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
                Assert.Equal(state, error.SqlState);
                await transaction.RollbackAsync();
            }
            Assert.True((await roles.DeleteAsync(role)).Succeeded);
            Assert.False(await db.UserRoles.AnyAsync(x => x.RoleId == role.Id));
            Assert.False(await db.RoleClaims.AnyAsync(x => x.RoleId == role.Id));
            Assert.True(await db.Users.AnyAsync(x => x.Id == user.Id));
        }
        finally
        {
            await db.Users.Where(x => x.Id == user.Id).ExecuteDeleteAsync();
            await db.Roles.Where(x => x.Id == role.Id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Concurrent_normalized_email_insert_is_rejected_by_database()
    {
        using var services = Services();
        using var first = services.CreateScope();
        using var second = services.CreateScope();
        var a = first.ServiceProvider.GetRequiredService<HappyCityIdentityDbContext>();
        var b = second.ServiceProvider.GetRequiredService<HappyCityIdentityDbContext>();
        var user = User();
        user.NormalizedEmail = user.Email!.ToUpperInvariant();
        user.NormalizedUserName = user.UserName!.ToUpperInvariant();
        var duplicate = User();
        duplicate.Email = user.Email;
        duplicate.NormalizedEmail = user.NormalizedEmail;
        duplicate.NormalizedUserName = duplicate.UserName!.ToUpperInvariant();
        a.Users.Add(user); b.Users.Add(duplicate);
        static async Task<string> Save(HappyCityIdentityDbContext db)
        {
            try { await db.SaveChangesAsync(); return "saved"; }
            catch (DbUpdateException e) when (e.InnerException is PostgresException p) { return p.SqlState; }
        }
        try
        {
            var outcomes = await Task.WhenAll(Save(a), Save(b));
            Assert.Equal(1, outcomes.Count(x => x == "saved"));
            Assert.Equal(1, outcomes.Count(x => x == "23505"));
        }
        finally { await a.Users.Where(x => x.NormalizedEmail == user.NormalizedEmail).ExecuteDeleteAsync(); }
    }
}
