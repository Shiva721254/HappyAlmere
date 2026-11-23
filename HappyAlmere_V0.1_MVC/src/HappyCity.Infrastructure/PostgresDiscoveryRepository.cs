using HappyCity.Application;
using HappyCity.Domain;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace HappyCity.Infrastructure;

public sealed class PostgresDiscoveryRepository(HappyCityDbContext database) : IDiscoveryRepository
{
    public const string SearchConfiguration = "dutch";

    public async Task<IReadOnlyList<DiscoveryItem>> FindAsync(DiscoveryFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        cancellationToken.ThrowIfCancellationRequested();
        if (filter.Query is null)
            return (await FindPageAsync(filter, DiscoveryFilter.MaximumResults, null, cancellationToken)).Items;

        var raw = filter.Query;
        var query = ApplyFilter(database.DiscoveryEntries, filter);
        return await query
            .OrderByDescending(x => EF.Property<NpgsqlTsVector?>(x, "SearchVector")!
                .RankCoverDensity(EF.Functions.PlainToTsQuery(SearchConfiguration, raw)))
            .ThenBy(x => x.LegacyId == null)
            .ThenBy(x => x.LegacyId)
            .ThenBy(x => x.Id)
            .Take(DiscoveryFilter.MaximumResults)
            .Select(x => new DiscoveryItem(x.Title, x.Description, x.Category.Slug, x.District, x.Icon, x.Slug))
            .ToArrayAsync(cancellationToken);
    }

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
        var query = ApplyFilter(entries, filter);
        var rows = await query.OrderBy(x => x.LegacyId == null).ThenBy(x => x.LegacyId).ThenBy(x => x.Id)
            .Take(size + 1).Select(x => new { x.Id, x.LegacyId, Item = new DiscoveryItem(x.Title, x.Description, x.Category.Slug, x.District, x.Icon, x.Slug) })
            .ToArrayAsync(cancellationToken);
        var visible = rows.Take(size).ToArray();
        var next = rows.Length > size ? DiscoveryCursor.Encode(filter, size, visible[^1].LegacyId, visible[^1].Id) : null;
        return new DiscoveryPage(visible.Select(x => x.Item).ToArray(), next);
    }

    public async Task<DiscoveryItem?> FindBySlugAsync(string citySlug, string slug, CancellationToken cancellationToken = default)
    {
        if (!DiscoveryFilter.IsValidSlug(slug)) return null;
        cancellationToken.ThrowIfCancellationRequested();
        return await database.DiscoveryEntries.AsNoTracking()
            .Where(x => x.City.Slug == citySlug && x.City.IsActive && x.IsPublished && x.Category.IsActive && x.Slug == slug)
            .Select(x => new DiscoveryItem(x.Title, x.Description, x.Category.Slug, x.District, x.Icon, x.Slug))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> FindDistrictsAsync(string citySlug, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await database.DiscoveryEntries.AsNoTracking()
            .Where(x => x.City.Slug == citySlug && x.City.IsActive && x.IsPublished && x.Category.IsActive)
            .Select(x => x.District)
            .Distinct()
            .OrderBy(x => x)
            .Take(50)
            .ToArrayAsync(cancellationToken);
    }

    private static IQueryable<DiscoveryEntry> ApplyFilter(IQueryable<DiscoveryEntry> entries, DiscoveryFilter filter)
    {
        var query = entries.AsNoTracking()
            .Where(x => x.City.Slug == filter.CitySlug && x.City.IsActive && x.IsPublished && x.Category.IsActive);
        if (filter.Category is not null) query = query.Where(x => x.Category.Slug == filter.Category);
        if (filter.District is not null)
        {
            var districtPattern = filter.District.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            query = query.Where(x => EF.Functions.ILike(x.District, districtPattern, "\\"));
        }
        if (filter.Query is not null)
        {
            var raw = filter.Query;
            var pattern = "%" + raw.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x =>
                EF.Property<NpgsqlTsVector?>(x, "SearchVector")!.Matches(EF.Functions.PlainToTsQuery(SearchConfiguration, raw))
                || EF.Functions.ILike(x.Title + " " + x.Description + " " + x.District, pattern, "\\"));
        }
        return query;
    }
}
