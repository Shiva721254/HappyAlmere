using HappyCity.Domain;

namespace HappyCity.Application;

public sealed class DiscoveryService(IDiscoveryRepository repository) : IDiscoveryService
{
    public Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return repository.FindAsync(filter, cancellationToken);
    }
    public Task<DiscoveryPage> FindPageAsync(DiscoveryFilter filter, int size, string? cursor, CancellationToken cancellationToken = default) =>
        repository.FindPageAsync(filter, size, cursor, cancellationToken);

    public Task<DiscoveryItem?> FindBySlugAsync(string citySlug, string slug, CancellationToken cancellationToken = default)
    {
        _ = new DiscoveryFilter(citySlug, null, null);
        if (!DiscoveryFilter.IsValidSlug(slug))
            return Task.FromResult<DiscoveryItem?>(null);
        return repository.FindBySlugAsync(citySlug, slug, cancellationToken);
    }

    public Task<IReadOnlyList<string>> FindDistrictsAsync(string citySlug, CancellationToken cancellationToken = default)
    {
        _ = new DiscoveryFilter(citySlug, null, null);
        return repository.FindDistrictsAsync(citySlug, cancellationToken);
    }
}
