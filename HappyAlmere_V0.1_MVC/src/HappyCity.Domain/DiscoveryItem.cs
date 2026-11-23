namespace HappyCity.Domain;
public sealed record DiscoveryItem(string Title, string Description, string Category, string District, string Icon, string Slug = "");
