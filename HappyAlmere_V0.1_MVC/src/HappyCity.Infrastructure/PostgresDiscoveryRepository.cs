using HappyCity.Application;
using HappyCity.Domain;
using Microsoft.EntityFrameworkCore;

namespace HappyCity.Infrastructure;

public sealed class PostgresDiscoveryRepository(HappyCityDbContext database) : IDiscoveryRepository
{
    public async Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default) =>
        (await FindPageAsync(filter, DiscoveryFilter.MaximumResults, null, cancellationToken)).Items;

    public async Task<DiscoveryPage> FindPageAsync(DiscoveryFilter filter, int size, string? cursor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var position = DiscoveryCursor.Parse(cursor, filter, size);
        cancellationToken.ThrowIfCancellationRequested();
        var entries = database.DiscoveryEntries.AsQueryable();
        if (position is not null)
        {
            // All values are EF parameters; row comparison matches the complete ordering.
            var rank = position.LegacyId is null;
            var legacy = position.LegacyId ?? 0;
            entries = database.DiscoveryEntries.FromSqlInterpolated($"SELECT * FROM discovery.discovery_entries WHERE ((legacy_id IS NULL), COALESCE(legacy_id,0), id) > ({rank}, {legacy}, {position.Id})");
        }
        var query = entries.AsNoTracking().Where(x => x.City.Slug == filter.CitySlug && x.City.IsActive && x.IsPublished && x.Category.IsActive);
        if (filter.Category is not null) query = query.Where(x => x.Category.Slug == filter.Category);
        if (filter.Query is not null)
        {
            var pattern = "%" + filter.Query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x => EF.Functions.ILike(x.Title + " " + x.Description + " " + x.District, pattern, "\\"));
        }
        var rows = await query.OrderBy(x => x.LegacyId == null).ThenBy(x => x.LegacyId).ThenBy(x => x.Id)
            .Take(size + 1).Select(x => new { x.Id, x.LegacyId, Item = new DiscoveryItem(x.Title, x.Description, x.Category.Slug, x.District, x.Icon) })
            .ToArrayAsync(cancellationToken);
        var visible = rows.Take(size).ToArray();
        var next = rows.Length > size ? DiscoveryCursor.Encode(filter, size, visible[^1].LegacyId, visible[^1].Id) : null;
        return new DiscoveryPage(visible.Select(x => x.Item).ToArray(), next);
    }
}
