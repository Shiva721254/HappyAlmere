using HappyCity.Application;
using HappyCity.Infrastructure;
using HappyCity.Infrastructure.Identity;
using HappyCity.Web.Security;
using HappyCity.Web.CityContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddIdentityFoundation(builder.Configuration);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddSingleton<SafeQueryTimingInterceptor>();
builder.Services.AddDbContext<HappyCityDbContext>((provider, options) => options.UseNpgsql(
    DatabaseConfiguration.RequireVerifiedTls(builder.Configuration.GetConnectionString("HappyCity")))
    .AddInterceptors(provider.GetRequiredService<SafeQueryTimingInterceptor>()));
builder.Services.AddScoped<IDiscoveryRepository, PostgresDiscoveryRepository>();
builder.Services.AddScoped<ICategoryLookup, PostgresCategoryLookup>();
builder.Services.AddScoped<ICityResolver>(provider => new PostgresCityResolver(
    provider.GetRequiredService<HappyCityDbContext>(),
    builder.Configuration.GetSection("TrustedLocalCities").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>()));
builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddScoped<RequestCityContext>();
builder.Services.AddScoped<ICityContext>(provider => provider.GetRequiredService<RequestCityContext>());
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Use the transport peer, not user-controlled forwarded headers. Unknown peers share a budget.
    options.AddPolicy("search", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Max(1, Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        return ValueTask.CompletedTask;
    };
});

var trustedProxy = TrustedProxyConfiguration.Create(builder.Configuration);
var app = builder.Build();
if (trustedProxy is not null)
{
    app.UseForwardedHeaders(trustedProxy);
}
// Register headers before exception handling; OnStarting also covers cleared error responses.
app.UseMiddleware<SecurityHeadersMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseMiddleware<CityContextMiddleware>();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

// Entry point for real-pipeline integration tests.
public partial class Program { }
