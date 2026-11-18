using HappyCity.Domain;
namespace HappyCity.Application;
public interface IDiscoveryRepository { IReadOnlyList<DiscoveryItem> GetAll(); }
public interface IDiscoveryService { IReadOnlyList<DiscoveryItem> Find(string? category, string? query); }
public sealed class DiscoveryService(IDiscoveryRepository repository) : IDiscoveryService {
 public IReadOnlyList<DiscoveryItem> Find(string? category, string? query) => repository.GetAll()
 .Where(x => string.IsNullOrWhiteSpace(category) || x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
 .Where(x => string.IsNullOrWhiteSpace(query) || $"{x.Title} {x.Description} {x.District}".Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
}
