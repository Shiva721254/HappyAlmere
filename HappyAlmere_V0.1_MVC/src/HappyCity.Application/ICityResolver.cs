namespace HappyCity.Application;

public sealed record CityResolution(Guid CityId, string CitySlug, string CityName,
    string CountryCode, string DefaultLanguage, string TimeZone, string RequestedHost,
    string? CanonicalHost, bool IsLocalEntryPoint, bool RequiresCanonicalRedirect);
public interface ICityResolver
{
    Task<CityResolution?> ResolveAsync(string hostname, CancellationToken cancellationToken = default);
}
