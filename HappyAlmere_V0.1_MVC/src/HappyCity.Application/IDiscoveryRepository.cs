using HappyCity.Domain;

namespace HappyCity.Application;

public interface IDiscoveryRepository
{
    Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default);
    Task<DiscoveryPage> FindPageAsync(DiscoveryFilter filter, int size, string? cursor, CancellationToken cancellationToken = default);
    Task<DiscoveryItem?> FindBySlugAsync(string citySlug, string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> FindDistrictsAsync(string citySlug, CancellationToken cancellationToken = default);
}
