namespace HappyCity.Application;

public sealed record CityResolution(string CitySlug, string? CanonicalHost);
public interface ICityResolver
{
    Task<CityResolution?> ResolveAsync(string hostname, CancellationToken cancellationToken = default);
}
