using HappyCity.Application;
using HappyCity.Domain;

namespace HappyCity.FoundationTests;

public sealed class DemoDiscoveryRepository : IDiscoveryRepository
{
    private static readonly IReadOnlyList<DiscoveryItem> Items = Array.AsReadOnly(new DiscoveryItem[]
    {
        new("Een wandeling aan het water", "Inspiratie voor een gratis buitenactiviteit.", DiscoveryCategories.Activities, "Almere Stad", "☀", "wandeling"),
        new("Een familiedag plannen", "Ontdek ideeën voor een dag met kinderen.", DiscoveryCategories.Activities, "Almere Buiten", "♧", "familiedag"),
        new("Lokaal lunchen", "Voorbeeld van een categorie voor lunch en restaurants.", DiscoveryCategories.Food, "Almere Haven", "♨", "lunch"),
        new("Winkelaanbiedingen", "Hier komen actuele, gecontroleerde acties.", DiscoveryCategories.Savings, "Almere Stad", "€", "aanbiedingen"),
        new("Huis en tuin", "Vind straks lokale vakmensen voor onderhoud.", DiscoveryCategories.Services, "Almere Poort", "⌂", "huis-tuin"),
        new("Ontdek je buurt", "Inspiratie voor lokale winkels en voorzieningen.", DiscoveryCategories.Shopping, "Muziekwijk", "◇", "buurt")
    });

    public Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        cancellationToken.ThrowIfCancellationRequested();
        if (filter.CitySlug != "almere")
            return Task.FromResult<IReadOnlyList<DiscoveryItem>>(Array.Empty<DiscoveryItem>());
        IReadOnlyList<DiscoveryItem> results = Items
            .Where(x => filter.Category is null || x.Category.Equals(filter.Category, StringComparison.OrdinalIgnoreCase))
            .Where(x => filter.District is null || x.District.Equals(filter.District, StringComparison.OrdinalIgnoreCase))
            .Where(x => filter.Query is null || $"{x.Title} {x.Description} {x.District}".Contains(filter.Query, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // In-memory work completes synchronously; a future EF repository will await database I/O.
        return Task.FromResult(results);
    }
    public async Task<DiscoveryPage> FindPageAsync(DiscoveryFilter filter, int size, string? cursor, CancellationToken cancellationToken = default)
    {
        DiscoveryCursor.Parse(cursor, filter, size);
        return new DiscoveryPage((await FindAsync(filter, cancellationToken)).Take(size).ToArray(), null);
    }

    public Task<DiscoveryItem?> FindBySlugAsync(string citySlug, string slug, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (citySlug != "almere" || !DiscoveryFilter.IsValidSlug(slug))
            return Task.FromResult<DiscoveryItem?>(null);
        return Task.FromResult(Items.FirstOrDefault(x => x.Slug.Equals(slug, StringComparison.Ordinal)));
    }

    public Task<IReadOnlyList<string>> FindDistrictsAsync(string citySlug, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (citySlug != "almere")
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        IReadOnlyList<string> districts = Items.Select(x => x.District).Distinct().Order().ToArray();
        return Task.FromResult(districts);
    }
}
