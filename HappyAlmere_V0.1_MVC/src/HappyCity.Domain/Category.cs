namespace HappyCity.Domain;

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? LegacyId { get; set; }
    public Guid? ParentId { get; set; }
    public required string Slug { get; set; }
    // Preserved Dutch display fallback; translations are authoritative when present.
    public required string Name { get; set; }
    public required string CategoryType { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
