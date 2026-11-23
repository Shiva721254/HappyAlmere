namespace HappyCity.Application;

public sealed record DiscoveryFilter
{
    public const int MaximumQueryLength = 100;
    public const int MaximumCategoryLength = 30;
    public const int MaximumDistrictLength = 100;
    public const int MaximumSlugLength = 120;
    public const int MaximumResults = 100;

    public string CitySlug { get; }
    public string? Category { get; }
    public string? Query { get; }
    public string? District { get; }

    public DiscoveryFilter(string citySlug, string? category, string? query, string? district = null)
    {
        if (string.IsNullOrWhiteSpace(citySlug) || citySlug.Length > 80 ||
            !System.Text.RegularExpressions.Regex.IsMatch(citySlug, @"\A[a-z0-9]+(-[a-z0-9]+)*\z"))
            throw new ArgumentException("A valid explicit city scope is required.", nameof(citySlug));
        CitySlug = citySlug;
        // PostgreSQL text cannot contain NUL; reject it before any repository I/O.
        if (category?.Contains('\0') == true || query?.Contains('\0') == true || district?.Contains('\0') == true)
            throw new ArgumentException("Search input contains an unsupported character.");
        if (category?.Length > MaximumCategoryLength || query?.Length > MaximumQueryLength || district?.Length > MaximumDistrictLength)
            throw new ArgumentException("Search input exceeds the supported length.");

        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim().ToLowerInvariant();
        Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        District = string.IsNullOrWhiteSpace(district) ? null : district.Trim();
    }

    public static bool IsValidSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug) &&
        slug.Length <= MaximumSlugLength &&
        System.Text.RegularExpressions.Regex.IsMatch(slug, @"\A[a-z0-9]+(-[a-z0-9]+)*\z");
}
