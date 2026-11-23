using System.Net;
using HappyCity.Application;
using HappyCity.Domain;
using HappyCity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HappyCity.FoundationTests;

public sealed class R1DiscoveryCompletionWebTests
{
    [Fact]
    public async Task Detail_page_renders_item_related_cards_and_security_headers()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync("/ontdek/wandeling");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Een wandeling aan het water", html);
        Assert.Contains("Almere Stad", html);
        Assert.Contains("Een familiedag plannen", html); // Related item in activiteiten
        Assert.Contains("Terug naar overzicht", html);
        Assert.Contains("script-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Theory]
    [InlineData("/ontdek/does-not-exist")]
    [InlineData("/ontdek/INVALID_SLUG")]
    [InlineData("/ontdek/bad--slug")]
    public async Task Detail_page_returns_404_for_missing_or_invalid_slug(string path)
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("script-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task District_filter_filters_results_and_retains_hidden_input()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync("/?district=Almere%20Haven");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Lokaal lunchen", html);
        Assert.DoesNotContain("Een wandeling aan het water", html);
        Assert.Contains("name=\"district\" value=\"Almere Haven\"", html);
        Assert.Contains("Geselecteerd stadsdeel: Almere Haven", html);
    }

    [Fact]
    public async Task Combined_category_and_district_filter_scopes_correctly()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync("/?category=activiteiten&district=Almere%20Buiten");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Een familiedag plannen", html);
        Assert.DoesNotContain("Een wandeling aan het water", html);
        Assert.Contains("Geselecteerd: activiteiten · Almere Buiten", html);
    }

    [Theory]
    [InlineData("/?district=%00")]
    [InlineData("/?district=Almere%00Stad")]
    public async Task Invalid_district_input_returns_bad_request(string path)
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Oversized_district_returns_bad_request()
    {
        await using var factory = new FoundationFactory();
        using var client = factory.HttpsClient();
        using var response = await client.GetAsync("/?district=" + new string('x', DiscoveryFilter.MaximumDistrictLength + 1));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("kunstlinie-almere", true)]
    [InlineData("a", true)]
    [InlineData("", false)]
    [InlineData("Invalid-Slug", false)]
    [InlineData("bad--slug", false)]
    [InlineData("-leading", false)]
    [InlineData("trailing-", false)]
    public void Slug_validator_enforces_strict_pattern(string slug, bool expected)
    {
        Assert.Equal(expected, DiscoveryFilter.IsValidSlug(slug));
    }
}

[Trait("Category", "Postgres")]
public sealed class R1DiscoveryCompletionPostgresTests
{
    private static string Connection(bool migration = false) =>
        DatabaseConfiguration.RequireVerifiedTls(Environment.GetEnvironmentVariable(
            migration ? "HAPPYCITY_MIGRATION_CONNECTION" : "ConnectionStrings__HappyCity"));

    private static HappyCityDbContext Database(bool migration = false) =>
        new(new DbContextOptionsBuilder<HappyCityDbContext>().UseNpgsql(Connection(migration)).Options);

    [Fact]
    public async Task FindBySlug_and_FindDistricts_enforce_city_and_publication_isolation()
    {
        await using var db = Database(true);
        await using var tx = await db.Database.BeginTransactionAsync();

        var utrecht = new City { Slug = "utrecht-detail-test", Name = "Synthetic Utrecht", CountryCode = "NL", DefaultLanguage = "nl", TimeZone = "Europe/Amsterdam", IsActive = true };
        db.Cities.Add(utrecht);
        await db.SaveChangesAsync();

        db.DiscoveryEntries.AddRange(
            new DiscoveryEntry { CityId = utrecht.Id, CategoryId = LegacyIdentifier.Map(2, 1), Slug = "utrecht-only-place", Title = "Utrecht Place", Description = "Other city", District = "Utrecht Centrum", Icon = "*", IsPublished = true },
            new DiscoveryEntry { CityId = LegacyIdentifier.Map(1, 1), CategoryId = LegacyIdentifier.Map(2, 1), Slug = "almere-draft-place", Title = "Almere Draft", Description = "Unpublished", District = "Draft Wijk", Icon = "*", IsPublished = false }
        );
        await db.SaveChangesAsync();

        var repo = new PostgresDiscoveryRepository(db);

        var found = await repo.FindBySlugAsync("almere", "wandeling");
        Assert.NotNull(found);
        Assert.Equal("Een wandeling aan het water", found!.Title);
        Assert.Equal("wandeling", found.Slug);

        // Cross-city and unpublished slugs MUST return null
        Assert.Null(await repo.FindBySlugAsync("almere", "utrecht-only-place"));
        Assert.Null(await repo.FindBySlugAsync("almere", "almere-draft-place"));
        Assert.Null(await repo.FindBySlugAsync("utrecht-detail-test", "wandeling"));

        var almereDistricts = await repo.FindDistrictsAsync("almere");
        Assert.Contains("Almere Stad", almereDistricts);
        Assert.Contains("Almere Haven", almereDistricts);
        Assert.DoesNotContain("Utrecht Centrum", almereDistricts);
        Assert.DoesNotContain("Draft Wijk", almereDistricts);
    }

    [Fact]
    public async Task District_filter_is_case_insensitive_and_escapes_wildcards()
    {
        await using var db = Database();
        var repo = new PostgresDiscoveryRepository(db);

        var haven = await repo.FindAsync(new DiscoveryFilter("almere", null, null, "almere haven"));
        Assert.Single(haven);
        Assert.Equal("Lokaal lunchen", haven[0].Title);

        Assert.Empty(await repo.FindAsync(new DiscoveryFilter("almere", null, null, "%")));
        Assert.Empty(await repo.FindAsync(new DiscoveryFilter("almere", null, null, "_")));
    }
}
