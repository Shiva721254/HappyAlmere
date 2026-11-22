using HappyCity.Application;
using Microsoft.EntityFrameworkCore;

namespace HappyCity.Infrastructure;

public sealed class PostgresCityResolver(
    HappyCityDbContext database,
    IReadOnlyDictionary<string, string> localEntries,
    IReadOnlySet<string>? allowedLiveDomains = null) : ICityResolver
{
    private readonly IReadOnlySet<string> liveDomains = allowedLiveDomains ?? EmptyLiveDomains;
    private static readonly IReadOnlySet<string> EmptyLiveDomains = new HashSet<string>(StringComparer.Ordinal);

    public static IReadOnlySet<string> CreateAllowedLiveDomains(bool allowLiveDomains, IEnumerable<string>? configuredDomains)
    {
        if (!allowLiveDomains)
            return EmptyLiveDomains;

        var domains = new HashSet<string>(StringComparer.Ordinal);
        if (configuredDomains is not null)
        {
            foreach (var raw in configuredDomains)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("CityResolution:AllowedLiveDomains contains an empty hostname.");
                var host = raw.Trim().ToLowerInvariant();
                if (!IsValidDnsHost(host) || host.EndsWith(".test", StringComparison.Ordinal))
                    throw new InvalidOperationException("CityResolution:AllowedLiveDomains requires explicit ASCII FQDN live hostnames.");
                domains.Add(host);
            }
        }

        if (domains.Count == 0)
            throw new InvalidOperationException("CityResolution:AllowLiveDomains requires at least one explicit hostname in CityResolution:AllowedLiveDomains.");

        return domains;
    }

    public async Task<CityResolution?> ResolveAsync(string hostname, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(hostname) || hostname.Length > 253) return null;
        var host = hostname.ToLowerInvariant();
        // Only explicitly configured development entry points bypass the domain table.
        // Forwarded headers and request query parameters are never inputs here.
        if (host is "localhost" or "127.0.0.1")
        {
            if (!localEntries.TryGetValue(host, out var slug)) return null;
            return await database.Cities.AsNoTracking()
                .Where(x => x.Slug == slug && x.IsActive)
                .Select(x => new CityResolution(x.Id, x.Slug, x.Name, x.CountryCode,
                    x.DefaultLanguage, x.TimeZone, host, null, true, false))
                .SingleOrDefaultAsync(cancellationToken);
        }
        if (!IsAllowedRegisteredHost(host)) return null;
        var match = await (from domain in database.CityDomains.AsNoTracking()
                           join city in database.Cities on domain.CityId equals city.Id
                           join primary in database.CityDomains on city.Id equals primary.CityId
                           where domain.Hostname == host && domain.IsActive && city.IsActive
                               && primary.IsPrimary && primary.IsActive
                           select new CityResolution(city.Id, city.Slug, city.Name,
                               city.CountryCode, city.DefaultLanguage, city.TimeZone, host,
                               primary.Hostname, false, domain.Hostname != primary.Hostname))
                           .SingleOrDefaultAsync(cancellationToken);
        return match?.CanonicalHost is string canonical && IsAllowedRegisteredHost(canonical) ? match : null;
    }

    private bool IsAllowedRegisteredHost(string host) =>
        IsValidDnsHost(host) && (host.EndsWith(".test", StringComparison.Ordinal) || liveDomains.Contains(host));

    private static bool IsValidDnsHost(string host)
    {
        if (host.Length is 0 or > 253) return false;
        var labels = host.Split('.');
        return labels.Length >= 2 && labels.All(label => label.Length is >= 1 and <= 63
            && char.IsAsciiLetterOrDigit(label[0]) && char.IsAsciiLetterOrDigit(label[^1])
            && label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'));
    }
}
