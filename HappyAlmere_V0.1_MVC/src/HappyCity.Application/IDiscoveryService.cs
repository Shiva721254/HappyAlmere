using HappyCity.Domain;

namespace HappyCity.Application;

public interface IDiscoveryService
{
    Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default);
}
