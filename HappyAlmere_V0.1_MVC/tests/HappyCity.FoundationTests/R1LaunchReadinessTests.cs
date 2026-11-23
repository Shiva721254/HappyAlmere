using System.Net;
using System.Text.Json;
using HappyCity.Domain;
using HappyCity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HappyCity.FoundationTests;

public sealed class R1LaunchReadinessTests
{
    [Fact]
    public void Live_domains_are_disabled_by_default()
    {
        var domains = PostgresCityResolver.CreateAllowedLiveDomains(false, new[] { "happyalmere.nl" });
        Assert.Empty(domains);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("localhost")]
    [InlineData("*.happyalmere.nl")]
    [InlineData("happyalmere.nl.")]
    [InlineData("almere.test")]
    [InlineData("bad host.nl")]
    [InlineData("é.nl")]
    public void Live_domain_allowlist_fails_closed_on_invalid_or_wildcard_entries(string? entry)
    {
        var input = entry is null ? Array.Empty<string>() : new[] { entry };
        Assert.Throws<InvalidOperationException>(() => PostgresCityResolver.CreateAllowedLiveDomains(true, input));
    }

    [Fact]
    public void Live_domain_allowlist_normalizes_valid_fqdns()
    {
        var domains = PostgresCityResolver.CreateAllowedLiveDomains(true, new[] { "Staging.HappyAlmere.NL", "happyalmere.nl" });
        Assert.Equal(2, domains.Count);
        Assert.Contains("staging.happyalmere.nl", domains);
        Assert.Contains("happyalmere.nl", domains);
    }

    [Fact]
    public async Task Healthz_returns_ok_with_security_headers_and_without_city_resolution()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("script-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.False(response.Headers.Contains("Server"));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", json.RootElement.GetProperty("status").GetString());
    }

    [Fact, Trait("Category", "Postgres")]
    public async Task Explicitly_allowlisted_live_domain_resolves_only_when_enabled()
    {
        var connection = DatabaseConfiguration.RequireVerifiedTls(Environment.GetEnvironmentVariable("HAPPYCITY_MIGRATION_CONNECTION"));
        await using var db = new HappyCityDbContext(new DbContextOptionsBuilder<HappyCityDbContext>().UseNpgsql(connection).Options);
        await using var tx = await db.Database.BeginTransactionAsync();
        var almere = LegacyIdentifier.Map(1, 1);
        db.CityDomains.Add(new CityDomain { CityId = almere, Hostname = "staging.happyalmere.nl", IsActive = true, IsPrimary = true });
        await db.SaveChangesAsync();

        var disabledResolver = new PostgresCityResolver(db, new Dictionary<string, string>());
        Assert.Null(await disabledResolver.ResolveAsync("staging.happyalmere.nl"));

        var liveAllowlist = PostgresCityResolver.CreateAllowedLiveDomains(true, new[] { "staging.happyalmere.nl" });
        var enabledResolver = new PostgresCityResolver(db, new Dictionary<string, string>(), liveAllowlist);
        var resolved = await enabledResolver.ResolveAsync("STAGING.HAPPYALMERE.NL");
        Assert.NotNull(resolved);
        Assert.Equal("almere", resolved.CitySlug);
        Assert.Equal("staging.happyalmere.nl", resolved.CanonicalHost);
        Assert.False(resolved.RequiresCanonicalRedirect);
        Assert.Null(await enabledResolver.ResolveAsync("unlisted.happyalmere.nl"));
    }
}
