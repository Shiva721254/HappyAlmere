using System.Net;
using HappyCity.Application;
using HappyCity.Domain;
using HappyCity.Web.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HappyCity.FoundationTests;

public sealed class R1VisualContentPolishTests
{
    [Fact]
    public void ExtractSource_extracts_valid_https_domain_and_cleans_description()
    {
        var result = DiscoveryPresentation.ExtractSource(
            "Theater aan het Weerwater in Almere Centrum. Bron: kunstlinie.nl.");

        Assert.Equal("Theater aan het Weerwater in Almere Centrum.", result.CleanDescription);
        Assert.Equal("kunstlinie.nl", result.SourceDomain);
        Assert.Equal("https://kunstlinie.nl", result.OfficialUrl);
    }

    [Theory]
    [InlineData("Gewone beschrijving zonder bronvermelding.")]
    [InlineData("Poging met scheme. Bron: javascript:alert(1).")]
    [InlineData("Poging met pad. Bron: evil.nl/phish.")]
    [InlineData("Poging met query. Bron: evil.nl?x=1.")]
    [InlineData("Poging met hoofdletters. Bron: EVIL.NL.")]
    [InlineData("Poging zonder punt. Bron: localhost.")]
    [InlineData("Poging met ongeldige tld. Bron: test.123.")]
    public void ExtractSource_rejects_missing_or_unsafe_domains(string description)
    {
        var result = DiscoveryPresentation.ExtractSource(description);
        Assert.Null(result.SourceDomain);
        Assert.Null(result.OfficialUrl);
        Assert.Equal(description, result.CleanDescription);
    }

    [Theory]
    [InlineData(DiscoveryCategories.Activities, "art-activiteiten", "Activiteiten")]
    [InlineData(DiscoveryCategories.Food, "art-eten", "Eten & drinken")]
    [InlineData(DiscoveryCategories.Savings, "art-besparen", "Besparen")]
    [InlineData(DiscoveryCategories.Services, "art-diensten", "Diensten")]
    [InlineData(DiscoveryCategories.Shopping, "art-winkelen", "Winkelen")]
    [InlineData("onbekend", "art-default", "onbekend")]
    public void Category_presentation_maps_classes_and_labels(string category, string expectedClass, string expectedLabel)
    {
        Assert.Equal(expectedClass, DiscoveryPresentation.CategoryArtClass(category));
        Assert.Equal(expectedLabel, DiscoveryPresentation.CategoryDisplayName(category));
    }

    [Fact]
    public async Task Home_page_renders_quick_searches_category_classes_and_no_prototype_empty_cta()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        var html = await client.GetStringAsync("/");
        Assert.Contains("Populair:", html);
        Assert.Contains("?q=wandelen", html);
        Assert.Contains("?q=kringloop", html);
        Assert.Contains("art-activiteiten", html);
        Assert.Contains("art-eten", html);
        Assert.DoesNotContain("href=\"https://", html);

        var emptyHtml = await client.GetStringAsync("/?q=does-not-exist");
        Assert.Contains("Alle plekken bekijken", emptyHtml);
        Assert.DoesNotContain("Alle voorbeelden", emptyHtml);
    }

    [Fact]
    public async Task Detail_page_renders_safe_external_official_website_button_for_sourced_entries()
    {
        await using var factory = new FoundationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDiscoveryRepository>();
                services.AddSingleton<IDiscoveryRepository>(new SourcedItemRepository());
            }));

        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:7143"),
            AllowAutoRedirect = false
        });

        // Home page shows source domain as a plain badge, NEVER an external link (keeps home link crawl internal).
        var homeHtml = await client.GetStringAsync("/");
        Assert.Contains("kunstlinie.nl", homeHtml);
        Assert.DoesNotContain("href=\"https://kunstlinie.nl\"", homeHtml);
        Assert.DoesNotContain("Bron: kunstlinie.nl", homeHtml);

        // Detail page renders clickable HTTPS link with rel="noopener noreferrer".
        using var detailResponse = await client.GetAsync("/ontdek/kunstlinie-almere");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detailHtml = await detailResponse.Content.ReadAsStringAsync();
        Assert.Contains("href=\"https://kunstlinie.nl\"", detailHtml);
        Assert.Contains("rel=\"noopener noreferrer\"", detailHtml);
        Assert.Contains("Officiële website (kunstlinie.nl)", detailHtml);
        Assert.DoesNotContain("Bron: kunstlinie.nl", detailHtml);
    }

    private sealed class SourcedItemRepository : IDiscoveryRepository
    {
        private static readonly DiscoveryItem Item = new(
            "Kunstlinie",
            "Theater aan het Weerwater in Almere Centrum. Bron: kunstlinie.nl.",
            DiscoveryCategories.Activities,
            "Almere Stad",
            "☀",
            "kunstlinie-almere");

        public Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiscoveryItem>>(new[] { Item });

        public Task<DiscoveryPage> FindPageAsync(DiscoveryFilter filter, int limit, string? cursor = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DiscoveryPage(new[] { Item }, null));

        public Task<DiscoveryItem?> FindBySlugAsync(string citySlug, string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiscoveryItem?>(slug == Item.Slug ? Item : null);

        public Task<IReadOnlyList<string>> FindDistrictsAsync(string citySlug, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(new[] { "Almere Stad" });
    }
}
