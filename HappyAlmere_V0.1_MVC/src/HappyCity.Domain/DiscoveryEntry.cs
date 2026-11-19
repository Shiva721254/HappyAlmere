namespace HappyCity.Domain;

public sealed class DiscoveryEntry
{
    public int Id { get; set; }
    public int CityId { get; set; }
    public int CategoryId { get; set; }
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string District { get; set; }
    public required string Icon { get; set; }
    public bool IsPublished { get; set; }
    public City City { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
