namespace HappyCity.Domain;

// Supported product configuration, not an assertion that other codes are invalid ISO codes.
public static class RegionalPolicy
{
    public static string Country(string value) => value.Trim().ToUpperInvariant() is "NL" ? "NL" :
        throw new ArgumentException("Country is not supported.");
    public static string Language(string value) => value.Trim().ToLowerInvariant() switch
    {
        "nl" => "nl",
        "en" => "en",
        _ => throw new ArgumentException("Language is not supported.")
    };
}
