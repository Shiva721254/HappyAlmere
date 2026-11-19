namespace HappyCity.Domain;

public sealed class City
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? LegacyId { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public required string CountryCode { get; set; }
    public required string DefaultLanguage { get; set; }
    public required string TimeZone { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
