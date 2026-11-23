using System.Net;

namespace HappyCity.FoundationTests;

public sealed class R1PolishTrustTests
{
    [Theory]
    [InlineData("/Home/Over")]
    [InlineData("/over")]
    public async Task Over_page_renders_editorial_mission_and_security_headers(string path)
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Over HappyAlmere", html);
        Assert.Contains("Onafhankelijk en eerlijk", html);
        Assert.Contains("info@happyalmere.nl", html);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        AssertAllSecurityHeaders(response);
    }

    [Fact]
    public async Task Privacy_page_exposes_gdpr_rights_and_takedown_channel()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync("/Home/Privacy");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Privacy", html);
        Assert.Contains("id=\"correctie\"", html);
        Assert.Contains("info@happyalmere.nl", html);
        Assert.Contains("privacy@happyalmere.nl", html);
        Assert.Contains("AVG", html);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        AssertAllSecurityHeaders(response);
    }

    [Fact]
    public async Task Business_page_exposes_transparent_terms_and_faq()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync("/Home/Business");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Voor ondernemers", html);
        Assert.Contains("Veelgestelde vragen", html);
        Assert.Contains("info@happyalmere.nl", html);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        AssertAllSecurityHeaders(response);
    }

    [Fact]
    public async Task Favicon_is_served_as_svg_with_security_headers()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync("/favicon.svg");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
        var svg = await response.Content.ReadAsStringAsync();
        Assert.Contains("<svg", svg);
        AssertAllSecurityHeaders(response);
    }

    [Fact]
    public async Task Detail_page_links_to_correction_channel_and_layout_links_to_over()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        var detailHtml = await client.GetStringAsync("/ontdek/wandeling");
        Assert.Contains("/Home/Privacy#correctie", detailHtml);
        Assert.Contains("Meld een correctie of verwijdering", detailHtml);
        Assert.Contains("href=\"/Home/Over\"", detailHtml);
        Assert.Contains("favicon.svg", detailHtml);
    }

    [Theory]
    [InlineData("/not-found")]
    [InlineData("/ontdek/does-not-exist")]
    [InlineData("/ontdek/INVALID_SLUG")]
    [InlineData("/.env")]
    [InlineData("/appsettings.json")]
    public async Task Missing_routes_return_branded_dutch_404_with_security_headers(string path)
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Pagina niet gevonden", html);
        Assert.Contains("Terug naar HappyAlmere", html);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        AssertAllSecurityHeaders(response);
    }

    private static void AssertAllSecurityHeaders(HttpResponseMessage response)
    {
        Assert.Contains("script-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("noindex, nofollow", response.Headers.GetValues("X-Robots-Tag").Single());
        Assert.Contains("camera=()", response.Headers.GetValues("Permissions-Policy").Single());
        Assert.False(response.Headers.Contains("Server"));
    }
}
