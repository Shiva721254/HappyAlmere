using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HappyCity.Infrastructure;

public sealed class HappyCityDbContextFactory : IDesignTimeDbContextFactory<HappyCityDbContext>
{
    public HappyCityDbContext CreateDbContext(string[] args)
    {
        // Explicit privileged connection used only by tooling; never by the web process.
        var connection = DatabaseConfiguration.RequireVerifiedTls(
            Environment.GetEnvironmentVariable("HAPPYCITY_MIGRATION_CONNECTION"));
        return new HappyCityDbContext(new DbContextOptionsBuilder<HappyCityDbContext>()
            .UseNpgsql(connection).Options);
    }
}
