namespace HappyCity.Domain;

public sealed class DiscoveryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? LegacyId { get; set; }
    public Guid CityId { get; set; }
    public Guid CategoryId { get; set; }
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string District { get; set; }
    public required string Icon { get; set; }
    public bool IsPublished { get; set; }
    public City City { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
