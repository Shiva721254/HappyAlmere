using HappyCity.Domain;

namespace HappyCity.Web.ViewModels;

public sealed record DiscoveryViewModel(IReadOnlyList<DiscoveryItem> Items, string? Query, string? Category, string? NextCursor = null);
