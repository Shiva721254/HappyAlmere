namespace HappyCity.Application;

public sealed record CategoryLabel(Guid Id, string Slug, string Name);
public interface ICategoryLookup
{
    Task<IReadOnlyList<CategoryLabel>> FindAsync(string citySlug, string language, CancellationToken cancellationToken = default);
}
