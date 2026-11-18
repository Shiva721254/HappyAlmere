using HappyCity.Domain;

namespace HappyCity.Application;

public sealed class DiscoveryService(IDiscoveryRepository repository) : IDiscoveryService
{
    public Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return repository.FindAsync(filter, cancellationToken);
    }
}
