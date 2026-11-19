using HappyCity.Domain;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HappyCity.Application;

public sealed record DiscoveryPage(IReadOnlyList<DiscoveryItem> Items, string? NextCursor);

// Navigation only: this is not an authorization token or a snapshot guarantee.
public sealed record DiscoveryCursor(int Version, string Binding, int? LegacyId, Guid Id)
{
    private static string Bind(DiscoveryFilter filter, int size) => Convert.ToHexString(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(new { filter.CitySlug, filter.Category, filter.Query, Size = size })));

    public static DiscoveryCursor? Parse(string? value, DiscoveryFilter filter, int size)
    {
        if (size is < 1 or > DiscoveryFilter.MaximumResults) throw new ArgumentException("Invalid page size.");
        if (value is null) return null;
        if (value.Length is 0 or > 512) throw new ArgumentException("Invalid cursor.");
        try
        {
            var cursor = JsonSerializer.Deserialize<DiscoveryCursor>(Convert.FromBase64String(value));
            if (cursor is null || cursor.Version != 1 || cursor.Binding != Bind(filter, size) ||
                cursor.Id == Guid.Empty || cursor.LegacyId < 0 ||
                (cursor.LegacyId is int legacy && cursor.Id != LegacyIdentifier.Map(3, legacy))) throw new ArgumentException("Invalid cursor.");
            return cursor;
        }
        catch (Exception e) when (e is FormatException or JsonException) { throw new ArgumentException("Invalid cursor.", e); }
    }

    public static string Encode(DiscoveryFilter filter, int size, int? legacyId, Guid id) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new DiscoveryCursor(1, Bind(filter,size),legacyId,id)));
}
