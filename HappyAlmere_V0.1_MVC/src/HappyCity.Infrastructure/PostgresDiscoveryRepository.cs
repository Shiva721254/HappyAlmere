using HappyCity.Application;
using HappyCity.Domain;
using Microsoft.EntityFrameworkCore;

namespace HappyCity.Infrastructure;

public sealed class PostgresDiscoveryRepository(HappyCityDbContext database) : IDiscoveryRepository
{
    public async Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        cancellationToken.ThrowIfCancellationRequested();
        var query = database.DiscoveryEntries.AsNoTracking()
            .Where(x => x.City.Slug == filter.CitySlug && x.IsPublished);
        if (filter.Category is not null)
            query = query.Where(x => x.Category.Slug == filter.Category);
        if (filter.Query is not null)
        {
            // Escape LIKE metacharacters: the public search is literal substring search.
            var pattern = "%" + filter.Query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x => EF.Functions.ILike(x.Title + " " + x.Description + " " + x.District, pattern, "\\"));
        }

        return await query.OrderBy(x => x.Id).Take(DiscoveryFilter.MaximumResults)
            .Select(x => new DiscoveryItem(x.Title, x.Description, x.Category.Slug, x.District, x.Icon))
            .ToArrayAsync(cancellationToken);
    }
}
