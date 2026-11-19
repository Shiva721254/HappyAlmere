using HappyCity.Domain;

namespace HappyCity.Application;

public interface IDiscoveryService
{
    Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default);
    Task<DiscoveryPage> FindPageAsync(DiscoveryFilter filter, int size, string? cursor, CancellationToken cancellationToken = default);
}
