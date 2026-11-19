using HappyCity.Application;
using Microsoft.EntityFrameworkCore;

namespace HappyCity.Infrastructure;

public sealed class PostgresCityResolver(HappyCityDbContext database, IReadOnlyDictionary<string, string> localEntries) : ICityResolver
{
    public async Task<CityResolution?> ResolveAsync(string hostname, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostname) || hostname.Length > 253 || hostname != hostname.Trim()) return null;
        var host = hostname.ToLowerInvariant();
        // Only explicitly configured development entry points bypass the domain table.
        // Forwarded headers and request query parameters are never inputs here.
        if (localEntries.TryGetValue(host, out var slug))
        {
            var active = await database.Cities.AsNoTracking().AnyAsync(x => x.Slug == slug && x.IsActive, cancellationToken);
            return active ? new CityResolution(slug, null) : null;
        }
        if (!host.EndsWith(".test", StringComparison.Ordinal)) return null; // synthetic-only boundary
        var match = await (from domain in database.CityDomains.AsNoTracking()
                           join city in database.Cities on domain.CityId equals city.Id
                           where domain.Hostname == host && domain.IsActive && city.IsActive
                           select new { city.Id, city.Slug, domain.IsPrimary }).SingleOrDefaultAsync(cancellationToken);
        if (match is null) return null;
        var primary = await database.CityDomains.AsNoTracking()
            .Where(x => x.CityId == match.Id && x.IsActive && x.IsPrimary)
            .Select(x => x.Hostname).SingleOrDefaultAsync(cancellationToken);
        if (primary is null || !primary.EndsWith(".test", StringComparison.Ordinal)) return null;
        return new CityResolution(match.Slug, match.IsPrimary ? null : primary);
    }
}
