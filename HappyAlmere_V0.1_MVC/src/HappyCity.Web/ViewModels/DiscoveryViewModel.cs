using HappyCity.Domain;

namespace HappyCity.Web.ViewModels;

public sealed record DiscoveryViewModel(
    IReadOnlyList<DiscoveryItem> Items,
    string? Query,
    string? Category,
    string? NextCursor = null,
    string? District = null,
    IReadOnlyList<string>? Districts = null)
{
    public IReadOnlyList<string> AvailableDistricts => Districts ?? Array.Empty<string>();
}

public sealed record DiscoveryDetailViewModel(
    DiscoveryItem Item,
    IReadOnlyList<DiscoveryItem> Related);
