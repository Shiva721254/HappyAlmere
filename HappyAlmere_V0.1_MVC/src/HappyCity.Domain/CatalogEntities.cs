namespace HappyCity.Domain;

public sealed class CityDomain
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CityId { get; set; }
    public required string Hostname { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class District
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CityId { get; set; }
    public Guid? ParentId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? ExternalCode { get; set; }
    public required string DistrictType { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class Location
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CityId { get; set; }
    public Guid? DistrictId { get; set; }
    public required string Street { get; set; }
    public required string HouseNumber { get; set; }
    public string? HouseNumberSuffix { get; set; }
    public required string PostalCode { get; set; }
    public required string CountryCode { get; set; }
    public string? BagIdentifier { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CategoryTranslation
{
    public Guid CategoryId { get; set; }
    public required string LanguageCode { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
