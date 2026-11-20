using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HappyCity.Infrastructure.Identity;

public static class IdentityServiceRegistration
{
    public static IServiceCollection AddIdentityFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("IdentityFoundation:Enabled")) return services;
        var connection = IdentityConfiguration.RequireRuntimeConnection(configuration.GetConnectionString("HappyCityIdentity"));
        services.AddDbContext<HappyCityIdentityDbContext>(options => options
            .UseNpgsql(connection, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "identity"))
            .AddInterceptors(new IdentityConfiguration.RuntimeAuthorityInterceptor()));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Stores.MaxLengthForKeys = 128;
        }).AddRoles<ApplicationRole>().AddEntityFrameworkStores<HappyCityIdentityDbContext>();
        return services;
    }
}
