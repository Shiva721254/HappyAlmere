using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HappyCity.FoundationTests;

public sealed class FoundationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HTTPS_PORT"] = "7143"
            }));
        // Probe actions exist only in the test assembly, never the production application.
        builder.ConfigureServices(services =>
            services.AddControllersWithViews().AddApplicationPart(typeof(SecurityProbeController).Assembly));
    }

    public HttpClient HttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost:7143"),
        AllowAutoRedirect = false
    });
}

public sealed class SecurityProbeController : Controller
{
    [HttpGet("/test/throw")]
    public IActionResult Throw() => throw new InvalidOperationException("sensitive-test-detail");

    [HttpPost("/test/unsafe")]
    public IActionResult Unsafe() => Ok();
}

public sealed class WebTests
{
    [Theory]
    [InlineData("/", "Haal meer uit")]
    [InlineData("/Home/Business", "Voor ondernemers")]
    [InlineData("/Home/Privacy", "Privacy")]
    [InlineData("/?q=familie", "Een familiedag plannen")]
    [InlineData("/?q=does-not-exist", "Geen resultaten")]
    [InlineData("/?category=activiteiten", "Een wandeling aan het water")]
    [InlineData("/?category=eten", "Lokaal lunchen")]
    [InlineData("/?category=besparen", "Winkelaanbiedingen")]
    [InlineData("/?category=diensten", "Huis en tuin")]
    [InlineData("/?category=winkelen", "Ontdek je buurt")]
    public async Task Pages_and_search_render(string path, string expected)
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expected, await response.Content.ReadAsStringAsync());
        AssertHeaders(response);
    }

    [Theory]
    [InlineData("q", 101)]
    [InlineData("category", 31)]
    public async Task Oversized_input_is_rejected(string name, int length)
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync($"/?{name}={new string('x', length)}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertHeaders(response);
    }

    [Fact]
    public async Task Search_encodes_untrusted_text_and_retains_category()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        var html = await client.GetStringAsync("/?category=eten&q=" + Uri.EscapeDataString("<script>alert(1)</script>"));
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("name=\"category\" value=\"eten\"", html);
    }

    [Fact]
    public async Task Http_redirects_to_https()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost:5143"), AllowAutoRedirect = false
        });
        using var response = await client.GetAsync("/?q=familie");
        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal("https://localhost:7143/?q=familie", response.Headers.Location!.AbsoluteUri);
    }

    [Fact]
    public async Task Untrusted_host_is_rejected()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        client.DefaultRequestHeaders.Host = "attacker.example";
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unsafe_action_requires_antiforgery_token()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.PostAsync("/test/unsafe", new StringContent(""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/test/throw", HttpStatusCode.InternalServerError)]
    [InlineData("/not-found", HttpStatusCode.NotFound)]
    [InlineData("/css/site.css", HttpStatusCode.OK)]
    public async Task Security_headers_cover_errors_and_static_files(string path, HttpStatusCode status)
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(status, response.StatusCode);
        AssertHeaders(response);
        Assert.DoesNotContain("sensitive-test-detail", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Hsts_is_emitted_for_non_loopback_https_host()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        // HSTS intentionally excludes localhost. Override only the test host allowlist.
        await using var hosted = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["AllowedHosts"] = "example.test" })));
        using var hostedClient = hosted.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://example.test"), AllowAutoRedirect = false
        });
        using var response = await hostedClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("max-age=", response.Headers.GetValues("Strict-Transport-Security").Single());
    }

    [Fact]
    public async Task Search_budget_returns_429_without_blocking_privacy()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        for (var i = 0; i < 120; i++)
        {
            using var allowed = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }
        using var rejected = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        AssertHeaders(rejected);
        using var privacy = await client.GetAsync("/Home/Privacy");
        Assert.Equal(HttpStatusCode.OK, privacy.StatusCode);
    }

    private static void AssertHeaders(HttpResponseMessage response)
    {
        Assert.Contains("script-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("noindex, nofollow", response.Headers.GetValues("X-Robots-Tag").Single());
        Assert.Contains("camera=()", response.Headers.GetValues("Permissions-Policy").Single());
    }
}
