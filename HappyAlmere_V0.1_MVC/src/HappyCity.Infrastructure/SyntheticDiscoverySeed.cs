using HappyCity.Domain;
using Microsoft.EntityFrameworkCore;

namespace HappyCity.Infrastructure;

// Versioned synthetic fixtures only. Production content must never be managed through HasData.
internal static class SyntheticDiscoverySeed
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<City>().HasData(new City { Id = LegacyIdentifier.Map(1, 1), LegacyId = 1, Slug = "almere", Name = "Almere", CountryCode = "NL", DefaultLanguage = "nl", TimeZone = "Europe/Amsterdam", IsActive = true });
        model.Entity<Category>().HasData(
            new Category { Id = LegacyIdentifier.Map(2, 1), LegacyId = 1, CategoryType = "discovery", IsActive = true, DisplayOrder = 1, Slug = "activiteiten", Name = "Activiteiten" },
            new Category { Id = LegacyIdentifier.Map(2, 2), LegacyId = 2, CategoryType = "discovery", IsActive = true, DisplayOrder = 2, Slug = "eten", Name = "Eten & drinken" },
            new Category { Id = LegacyIdentifier.Map(2, 3), LegacyId = 3, CategoryType = "discovery", IsActive = true, DisplayOrder = 3, Slug = "besparen", Name = "Besparen" },
            new Category { Id = LegacyIdentifier.Map(2, 4), LegacyId = 4, CategoryType = "discovery", IsActive = true, DisplayOrder = 4, Slug = "diensten", Name = "Diensten" },
            new Category { Id = LegacyIdentifier.Map(2, 5), LegacyId = 5, CategoryType = "discovery", IsActive = true, DisplayOrder = 5, Slug = "winkelen", Name = "Winkelen" });
        model.Entity<DiscoveryEntry>().HasData(
            Entry(1, 1, "wandeling", "Een wandeling aan het water", "Inspiratie voor een gratis buitenactiviteit.", "Almere Stad", "☀"),
            Entry(2, 1, "familiedag", "Een familiedag plannen", "Ontdek ideeën voor een dag met kinderen.", "Almere Buiten", "♧"),
            Entry(3, 2, "lunch", "Lokaal lunchen", "Voorbeeld van een categorie voor lunch en restaurants.", "Almere Haven", "♨"),
            Entry(4, 3, "aanbiedingen", "Winkelaanbiedingen", "Hier komen actuele, gecontroleerde acties.", "Almere Stad", "€"),
            Entry(5, 4, "huis-tuin", "Huis en tuin", "Vind straks lokale vakmensen voor onderhoud.", "Almere Poort", "⌂"),
            Entry(6, 5, "buurt", "Ontdek je buurt", "Inspiratie voor lokale winkels en voorzieningen.", "Muziekwijk", "◇"));
    }

    private static DiscoveryEntry Entry(int id, int category, string slug, string title, string description, string district, string icon) =>
        new() { Id = LegacyIdentifier.Map(3, id), LegacyId = id, CityId = LegacyIdentifier.Map(1, 1), CategoryId = LegacyIdentifier.Map(2, category), Slug = slug, Title = title,
            Description = description, District = district, Icon = icon, IsPublished = true };
}
