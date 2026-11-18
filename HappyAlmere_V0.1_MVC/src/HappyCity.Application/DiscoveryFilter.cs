namespace HappyCity.Application;

public sealed record DiscoveryFilter
{
    public const int MaximumQueryLength = 100;
    public const int MaximumCategoryLength = 30;

    public string? Category { get; }
    public string? Query { get; }

    public DiscoveryFilter(string? category, string? query)
    {
        if (category?.Length > MaximumCategoryLength || query?.Length > MaximumQueryLength)
            throw new ArgumentException("Search input exceeds the supported length.");

        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
    }
}
