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
}
