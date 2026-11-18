using HappyCity.Application;
using HappyCity.Domain;

namespace HappyCity.Infrastructure;

public sealed class DemoDiscoveryRepository : IDiscoveryRepository
{
    private static readonly IReadOnlyList<DiscoveryItem> Items = Array.AsReadOnly(new DiscoveryItem[]
    {
        new("Een wandeling aan het water", "Inspiratie voor een gratis buitenactiviteit.", "activiteiten", "Almere Stad", "☀"),
        new("Een familiedag plannen", "Ontdek ideeën voor een dag met kinderen.", "activiteiten", "Almere Buiten", "♧"),
        new("Lokaal lunchen", "Voorbeeld van een categorie voor lunch en restaurants.", "eten", "Almere Haven", "♨"),
        new("Winkelaanbiedingen", "Hier komen actuele, gecontroleerde acties.", "besparen", "Almere Stad", "€"),
        new("Huis en tuin", "Vind straks lokale vakmensen voor onderhoud.", "diensten", "Almere Poort", "⌂"),
        new("Ontdek je buurt", "Inspiratie voor lokale winkels en voorzieningen.", "winkelen", "Muziekwijk", "◇")
    });

    public Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<DiscoveryItem> results = Items
            .Where(x => filter.Category is null || x.Category.Equals(filter.Category, StringComparison.OrdinalIgnoreCase))
            .Where(x => filter.Query is null || $"{x.Title} {x.Description} {x.District}".Contains(filter.Query, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // In-memory work completes synchronously; a future EF repository will await database I/O.
        return Task.FromResult(results);
    }
}
