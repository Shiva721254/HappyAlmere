using HappyCity.Domain;
using Microsoft.EntityFrameworkCore;

namespace HappyCity.Infrastructure;

public sealed class HappyCityDbContext(DbContextOptions<HappyCityDbContext> options) : DbContext(options)
{
    public DbSet<City> Cities => Set<City>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<DiscoveryEntry> DiscoveryEntries => Set<DiscoveryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var city = modelBuilder.Entity<City>();
        city.ToTable("Cities", table => table.HasCheckConstraint("CK_Cities_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'"));
        city.HasKey(x => x.Id);
        city.Property(x => x.Id).HasIdentityOptions(startValue: 1000);
        city.Property(x => x.Slug).HasMaxLength(80);
        city.Property(x => x.Name).HasMaxLength(100);
        city.HasIndex(x => x.Slug).IsUnique();

        var category = modelBuilder.Entity<Category>();
        category.ToTable("Categories", table => table.HasCheckConstraint("CK_Categories_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'"));
        category.HasKey(x => x.Id);
        category.Property(x => x.Id).HasIdentityOptions(startValue: 1000);
        category.Property(x => x.Slug).HasMaxLength(30);
        category.Property(x => x.Name).HasMaxLength(100);
        category.HasIndex(x => x.Slug).IsUnique();

        var entry = modelBuilder.Entity<DiscoveryEntry>();
        entry.ToTable("DiscoveryEntries", table =>
        {
            table.HasCheckConstraint("CK_DiscoveryEntries_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            table.HasCheckConstraint("CK_DiscoveryEntries_Title", "length(btrim(\"Title\")) > 0");
        });
        entry.HasKey(x => x.Id);
        entry.Property(x => x.Id).HasIdentityOptions(startValue: 1000);
        entry.Property(x => x.Slug).HasMaxLength(120);
        entry.Property(x => x.Title).HasMaxLength(200);
        entry.Property(x => x.Description).HasMaxLength(2000);
        entry.Property(x => x.District).HasMaxLength(100);
        entry.Property(x => x.Icon).HasMaxLength(16);
        entry.HasIndex(x => new { x.CityId, x.Slug }).IsUnique();
        entry.HasIndex(x => new { x.CityId, x.IsPublished, x.CategoryId, x.Id });
        entry.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        entry.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        SyntheticDiscoverySeed.Configure(modelBuilder);
    }
}
