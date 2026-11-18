using HappyCity.Domain;

namespace HappyCity.Application;

public interface IDiscoveryRepository
{
    Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default);
}
