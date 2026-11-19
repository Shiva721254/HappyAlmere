using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HappyCity.Infrastructure.Identity;

public sealed class HappyCityIdentityDbContextFactory : IDesignTimeDbContextFactory<HappyCityIdentityDbContext>
{
    public HappyCityIdentityDbContext CreateDbContext(string[] args)
    {
        var connection = DatabaseConfiguration.RequireVerifiedTls(
            Environment.GetEnvironmentVariable("HAPPYCITY_IDENTITY_MIGRATION_CONNECTION"));
        return new(new DbContextOptionsBuilder<HappyCityIdentityDbContext>()
            .UseNpgsql(connection, options => options.MigrationsHistoryTable("__EFMigrationsHistory", "identity"))
            .Options);
    }
}
