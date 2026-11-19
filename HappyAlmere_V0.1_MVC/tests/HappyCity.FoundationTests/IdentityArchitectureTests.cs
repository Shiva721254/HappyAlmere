using System.Net;
using System.Diagnostics;
using System.Text.Json;
using HappyCity.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HappyCity.FoundationTests;

public sealed class IdentityArchitectureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_child_processes_exclude_privileged_environment(bool powershell)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "HappyAlmere.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var environmentFile = Path.GetTempFileName();
        try
        {
            var start = new ProcessStartInfo(powershell ? (OperatingSystem.IsWindows() ? "powershell" : "pwsh") : "node")
            {
                WorkingDirectory = root.FullName, RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            foreach (var name in new[] { "POSTGRES_PASSWORD", "HAPPYCITY_MIGRATION_CONNECTION", "HAPPYCITY_IDENTITY_MIGRATION_CONNECTION",
                "HAPPYCITY_TEST_HAPPY_BACKUP", "HAPPYCITY_IDENTITY_RETIRED_CONNECTION", "FUTURE_PRIVILEGED_SECRET" })
                start.Environment[name] = "synthetic-sentinel";
            start.Environment["ConnectionStrings__HappyCity"] = "synthetic-runtime";
            start.Environment["ConnectionStrings__HappyCityIdentity"] = "synthetic-runtime";
            start.Environment["IdentityFoundation__Enabled"] = "false";
            if (powershell)
            {
                start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-Command");
                start.ArgumentList.Add("function dotnet { & node -e 'process.stdout.write(JSON.stringify(Object.keys(process.env)))' }; & ./scripts/Start-Local.ps1 -EnvironmentFile '" + environmentFile.Replace("'", "''") + "'");
            }
            else
            {
                start.ArgumentList.Add("-e");
                start.ArgumentList.Add("const {runtimeEnvironment}=require('./scripts/run-browser-tests.cjs'); const r=require('node:child_process').spawnSync(process.execPath,['-e','process.stdout.write(JSON.stringify(Object.keys(process.env)))'],{env:runtimeEnvironment(process.env),encoding:'utf8'});process.stdout.write(r.stdout);process.exit(r.status);");
            }
            using var process = Process.Start(start)!;
            var output = await process.StandardOutput.ReadToEndAsync();
            var errors = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Assert.True(process.ExitCode == 0, "Environment boundary probe failed: " + errors);
            var names = JsonSerializer.Deserialize<string[]>(output)!.Select(x => x.ToUpperInvariant()).ToArray();
            Assert.Contains("CONNECTIONSTRINGS__HAPPYCITY", names);
            Assert.Contains("CONNECTIONSTRINGS__HAPPYCITYIDENTITY", names);
            Assert.Contains("IDENTITYFOUNDATION__ENABLED", names);
            Assert.DoesNotContain(names, n => n.Contains("MIGRATION") || n.Contains("BACKUP") || n.Contains("RESTORE") || n.Contains("RETIRED") || n.Contains("SECRET") || n == "POSTGRES_PASSWORD");
        }
        finally { File.Delete(environmentFile); }
    }
    [Fact]
    public void Default_off_registers_no_identity_services()
    {
        var services = new ServiceCollection();
        services.AddIdentityFoundation(new ConfigurationBuilder().Build());
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(UserManager<ApplicationUser>));
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(HappyCityIdentityDbContext));
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IAuthenticationSchemeProvider));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Host=localhost;Username=happy_identity_app;SSL Mode=Disable")]
    [InlineData("Host=localhost;Username=happy_identity_owner;SSL Mode=VerifyFull")]
    public void Enabled_invalid_configuration_fails_closed(string? connection)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["IdentityFoundation:Enabled"] = "true", ["ConnectionStrings:HappyCityIdentity"] = connection
        }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddIdentityFoundation(config));
    }

    [Fact]
    public void Enabled_services_resolve_without_sign_in_or_cookie_schemes()
    {
        using var services = IdentityPersistenceTests.Services();
        using var scope = services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<HappyCityIdentityDbContext>());
        Assert.Null(scope.ServiceProvider.GetService<SignInManager<ApplicationUser>>());
        Assert.Null(scope.ServiceProvider.GetService<IAuthenticationSchemeProvider>());
    }

    [Theory]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    [InlineData("/register")]
    [InlineData("/login")]
    [InlineData("/manage/info")]
    [InlineData("/Account/ForgotPassword")]
    [InlineData("/admin")]
    public async Task Persistence_does_not_publish_account_endpoints(string path)
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out _));
    }
}
