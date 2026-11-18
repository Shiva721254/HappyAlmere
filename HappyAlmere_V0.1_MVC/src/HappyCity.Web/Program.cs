using HappyCity.Application;
using HappyCity.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddSingleton<IDiscoveryRepository, DemoDiscoveryRepository>();
builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddRateLimiter(o => {
 o.RejectionStatusCode = 429;
 o.AddFixedWindowLimiter("search", x => { x.PermitLimit = 120; x.Window = TimeSpan.FromMinutes(1); x.QueueLimit = 0; });
});
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.Use(async (context, next) => {
 context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'none'; style-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
 context.Response.Headers["X-Content-Type-Options"] = "nosniff";
 context.Response.Headers["X-Frame-Options"] = "DENY";
 context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
 context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
 await next();
});
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
