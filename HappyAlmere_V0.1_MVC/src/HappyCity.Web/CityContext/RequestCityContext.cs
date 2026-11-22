using HappyCity.Application;

namespace HappyCity.Web.CityContext;

public sealed class RequestCityContext : ICityContext
{
    private CityResolution? resolution;
    private CityResolution Value => Volatile.Read(ref resolution)
        ?? throw new InvalidOperationException("City context is not initialized.");

    public void Initialize(CityResolution value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (Interlocked.CompareExchange(ref resolution, value, null) is not null)
            throw new InvalidOperationException("City context is already initialized.");
    }

    public Guid CityId => Value.CityId;
    public string CitySlug => Value.CitySlug;
    public string CityName => Value.CityName;
    public string CountryCode => Value.CountryCode;
    public string DefaultLanguage => Value.DefaultLanguage;
    public string TimeZone => Value.TimeZone;
    public string RequestedHost => Value.RequestedHost;
    public string? CanonicalHost => Value.CanonicalHost;
    public bool IsLocalEntryPoint => Value.IsLocalEntryPoint;
    public bool RequiresCanonicalRedirect => Value.RequiresCanonicalRedirect;
}
