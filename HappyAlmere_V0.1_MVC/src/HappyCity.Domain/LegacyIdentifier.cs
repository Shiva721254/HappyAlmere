namespace HappyCity.Domain;

// Reserved single-source import namespace. New records use Guid.NewGuid() (v4).
public static class LegacyIdentifier
{
    public static Guid Map(int entityTag, int legacyId)
    {
        if (entityTag is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(entityTag));
        if (legacyId < 0) throw new ArgumentOutOfRangeException(nameof(legacyId));
        return Guid.Parse($"{entityTag:x8}-0000-8000-8000-{legacyId:x12}");
    }
}
