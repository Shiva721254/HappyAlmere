using HappyCity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyCity.Infrastructure;

public sealed class HappyCityDbContext(DbContextOptions<HappyCityDbContext> options) : DbContext(options)
{
    public DbSet<City> Cities => Set<City>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<DiscoveryEntry> DiscoveryEntries => Set<DiscoveryEntry>();
    public DbSet<CityDomain> CityDomains => Set<CityDomain>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var city = model.Entity<City>();
        city.ToTable("cities", "catalog", t =>
        {
            t.HasCheckConstraint("cities_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            t.HasCheckConstraint("cities_country", "country_code ~ '^[A-Z]{2}$'");
            t.HasCheckConstraint("cities_language", "default_language ~ '^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$'");
        });
        city.HasKey(x => x.Id);
        city.Property(x => x.Slug).HasMaxLength(80);
        city.Property(x => x.Name).HasMaxLength(100);
        city.Property(x => x.CountryCode).HasMaxLength(2);
        city.Property(x => x.DefaultLanguage).HasMaxLength(10);
        city.Property(x => x.TimeZone).HasMaxLength(100);
        city.HasIndex(x => x.Slug).IsUnique();
        city.HasIndex(x => x.LegacyId).IsUnique();

        var category = model.Entity<Category>();
        category.ToTable("categories", "catalog", t =>
        {
            t.HasCheckConstraint("categories_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            t.HasCheckConstraint("categories_parent", "parent_id IS NULL OR parent_id <> id");
        });
        category.HasKey(x => x.Id);
        category.Property(x => x.Slug).HasMaxLength(30);
        category.Property(x => x.Name).HasMaxLength(100);
        category.Property(x => x.CategoryType).HasMaxLength(30);
        category.HasIndex(x => x.Slug).IsUnique();
        category.HasIndex(x => x.LegacyId).IsUnique();
        category.HasOne<Category>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);

        var entry = model.Entity<DiscoveryEntry>();
        entry.ToTable("discovery_entries", "discovery", t =>
        {
            t.HasCheckConstraint("entries_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            t.HasCheckConstraint("entries_title", "length(btrim(title)) > 0");
        });
        entry.HasKey(x => x.Id);
        entry.HasIndex(x => x.LegacyId).IsUnique();
        entry.Property(x => x.Slug).HasMaxLength(120);
        entry.Property(x => x.Title).HasMaxLength(200);
        entry.Property(x => x.Description).HasMaxLength(2000);
        entry.Property(x => x.District).HasMaxLength(100);
        entry.Property(x => x.Icon).HasMaxLength(16);
        entry.HasIndex(x => new { x.CityId, x.Slug }).IsUnique();
        entry.HasIndex(x => new { x.CityId, x.IsPublished, x.CategoryId, x.LegacyId, x.Id });
        entry.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        entry.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        var domain = model.Entity<CityDomain>();
        domain.ToTable("city_domains", "catalog", t => t.HasCheckConstraint("domains_normalized",
            "hostname = lower(hostname) AND hostname ~ '^([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\\.)+[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$'"));
        domain.HasKey(x => x.Id);
        domain.Property(x => x.Hostname).HasMaxLength(253);
        domain.HasIndex(x => x.Hostname).IsUnique();
        domain.HasIndex(x => x.CityId).IsUnique().HasFilter("is_primary AND is_active");
        domain.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);

        var district = model.Entity<District>();
        district.ToTable("districts", "catalog", t =>
        {
            t.HasCheckConstraint("districts_parent", "parent_id IS NULL OR parent_id <> id");
            t.HasCheckConstraint("districts_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
        });
        district.HasKey(x => x.Id);
        district.HasAlternateKey(x => new { x.CityId, x.Id });
        district.HasIndex(x => new { x.CityId, x.Slug }).IsUnique();
        district.Property(x => x.Name).HasMaxLength(100);
        district.Property(x => x.Slug).HasMaxLength(100);
        district.Property(x => x.ExternalCode).HasMaxLength(100);
        district.Property(x => x.DistrictType).HasMaxLength(30);
        district.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        district.HasOne<District>().WithMany().HasForeignKey(x => new { x.CityId, x.ParentId })
            .HasPrincipalKey(x => new { x.CityId, x.Id }).OnDelete(DeleteBehavior.Restrict);

        var location = model.Entity<Location>();
        location.ToTable("locations", "catalog", t =>
        {
            t.HasCheckConstraint("locations_coordinates", "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL AND latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180)");
            t.HasCheckConstraint("locations_country", "country_code ~ '^[A-Z]{2}$'");
            t.HasCheckConstraint("locations_number", "house_number ~ '^[0-9]+$'");
        });
        location.HasKey(x => x.Id);
        location.Property(x => x.Street).HasMaxLength(200);
        location.Property(x => x.HouseNumber).HasMaxLength(20);
        location.Property(x => x.HouseNumberSuffix).HasMaxLength(20);
        location.Property(x => x.PostalCode).HasMaxLength(20);
        location.Property(x => x.CountryCode).HasMaxLength(2);
        location.Property(x => x.BagIdentifier).HasMaxLength(16);
        location.Property(x => x.Latitude).HasPrecision(9, 6);
        location.Property(x => x.Longitude).HasPrecision(9, 6);
        location.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        location.HasOne<District>().WithMany().HasForeignKey(x => new { x.CityId, x.DistrictId })
            .HasPrincipalKey(x => new { x.CityId, x.Id }).OnDelete(DeleteBehavior.Restrict);

        var translation = model.Entity<CategoryTranslation>();
        translation.ToTable("category_translations", "catalog", t => t.HasCheckConstraint("translations_language", "language_code ~ '^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$'"));
        translation.HasKey(x => new { x.CategoryId, x.LanguageCode });
        translation.Property(x => x.LanguageCode).HasMaxLength(10);
        translation.Property(x => x.Name).HasMaxLength(100);
        translation.Property(x => x.Description).HasMaxLength(2000);
        translation.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        foreach (var entity in model.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(System.Text.RegularExpressions.Regex.Replace(property.Name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant());
                if (property.Name is "CreatedAt" or "UpdatedAt") property.SetDefaultValueSql("CURRENT_TIMESTAMP");
            }
        // Fixtures are owned by the preserved initial migration, not mutable model seeds.
    }
}
