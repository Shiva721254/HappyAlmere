namespace HappyCity.Application;

// Public geographic scope, never private-resource authorization.
public interface ICityContext
{
    Guid CityId { get; }
    string CitySlug { get; }
    string CityName { get; }
    string CountryCode { get; }
    string DefaultLanguage { get; }
    string TimeZone { get; }
    string RequestedHost { get; }
    string? CanonicalHost { get; }
    bool IsLocalEntryPoint { get; }
    bool RequiresCanonicalRedirect { get; }
}
