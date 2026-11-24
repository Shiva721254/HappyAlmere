using HappyCity.Domain;

namespace HappyCity.Web.ViewModels;

public readonly record struct SourcePresentation(
    string CleanDescription,
    string? SourceDomain,
    string? OfficialUrl);

public static class DiscoveryPresentation
{
    private const string SourceMarker = " Bron: ";

    public static SourcePresentation ExtractSource(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return new SourcePresentation(string.Empty, null, null);

        var trimmed = description.Trim();
        var markerIndex = trimmed.LastIndexOf(SourceMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex <= 0)
            return new SourcePresentation(trimmed, null, null);

        var candidate = trimmed[(markerIndex + SourceMarker.Length)..].Trim();
        if (candidate.EndsWith('.'))
            candidate = candidate[..^1];

        if (!IsValidPublicDomain(candidate))
            return new SourcePresentation(trimmed, null, null);

        var clean = trimmed[..markerIndex].TrimEnd();
        return new SourcePresentation(clean, candidate, "https://" + candidate);
    }

    public static string CategoryArtClass(string? category) =>
        category?.Trim().ToLowerInvariant() switch
        {
            DiscoveryCategories.Activities => "art-activiteiten",
            DiscoveryCategories.Food => "art-eten",
            DiscoveryCategories.Savings => "art-besparen",
            DiscoveryCategories.Services => "art-diensten",
            DiscoveryCategories.Shopping => "art-winkelen",
            _ => "art-default"
        };

    public static string CategoryDisplayName(string? category) =>
        category?.Trim().ToLowerInvariant() switch
        {
            DiscoveryCategories.Activities => "Activiteiten",
            DiscoveryCategories.Food => "Eten & drinken",
            DiscoveryCategories.Savings => "Besparen",
            DiscoveryCategories.Services => "Diensten",
            DiscoveryCategories.Shopping => "Winkelen",
            _ => category ?? string.Empty
        };

    public static bool IsValidPublicDomain(string? domain)
    {
        if (string.IsNullOrEmpty(domain) || domain.Length is < 4 or > 100)
            return false;

        var labels = domain.Split('.');
        if (labels.Length < 2)
            return false;

        foreach (var label in labels)
        {
            if (label.Length is < 1 or > 63)
                return false;
            if (!IsLowerAlphaNum(label[0]) || !IsLowerAlphaNum(label[^1]))
                return false;
            for (var i = 0; i < label.Length; i++)
            {
                var c = label[i];
                if (!IsLowerAlphaNum(c) && c != '-')
                    return false;
            }
        }

        var tld = labels[^1];
        if (tld.Length is < 2 or > 24)
            return false;
        for (var i = 0; i < tld.Length; i++)
        {
            if (tld[i] is < 'a' or > 'z')
                return false;
        }

        return true;
    }

    private static bool IsLowerAlphaNum(char c) =>
        c is (>= 'a' and <= 'z') or (>= '0' and <= '9');
}
