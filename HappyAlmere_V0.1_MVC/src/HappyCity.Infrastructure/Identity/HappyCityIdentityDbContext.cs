using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HappyCity.Infrastructure.Identity;

public sealed class HappyCityIdentityDbContext(DbContextOptions<HappyCityIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users", table =>
            {
                table.HasCheckConstraint("ck_users_uuid_v4", "get_byte(uuid_send(id), 6) >> 4 = 4 AND get_byte(uuid_send(id), 8) >> 6 = 2");
                table.HasCheckConstraint("ck_users_access_failed_count", "access_failed_count >= 0");
                table.HasCheckConstraint("ck_users_normalized_user_name", "length(btrim(normalized_user_name)) > 0");
                table.HasCheckConstraint("ck_users_normalized_email", "length(btrim(normalized_email)) > 0");
            });
            entity.HasKey(x => x.Id).HasName("pk_users");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.UserName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NormalizedUserName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.SecurityStamp).IsRequired();
            entity.Property(x => x.ConcurrencyStamp).IsRequired().IsConcurrencyToken();
            entity.HasIndex(x => x.NormalizedUserName).HasDatabaseName("ux_users_normalized_user_name").IsUnique();
            entity.HasIndex(x => x.NormalizedEmail).HasDatabaseName("ux_users_normalized_email").IsUnique();
        });
        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("roles", table =>
            {
                table.HasCheckConstraint("ck_roles_uuid_v4", "get_byte(uuid_send(id), 6) >> 4 = 4 AND get_byte(uuid_send(id), 8) >> 6 = 2");
                table.HasCheckConstraint("ck_roles_normalized_name", "length(btrim(normalized_name)) > 0");
            });
            entity.HasKey(x => x.Id).HasName("pk_roles");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ConcurrencyStamp).IsRequired().IsConcurrencyToken();
            entity.HasIndex(x => x.NormalizedName).HasDatabaseName("ux_roles_normalized_name").IsUnique();
        });
        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId }).HasName("pk_user_roles");
            entity.HasIndex(x => x.RoleId).HasDatabaseName("ix_user_roles_role_id");
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_user_roles_users");
            entity.HasOne<ApplicationRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_user_roles_roles");
        });
        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("user_claims");
            entity.HasKey(x => x.Id).HasName("pk_user_claims");
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => x.UserId).HasDatabaseName("ix_user_claims_user_id");
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_user_claims_users");
        });
        builder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("role_claims");
            entity.HasKey(x => x.Id).HasName("pk_role_claims");
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => x.RoleId).HasDatabaseName("ix_role_claims_role_id");
            entity.HasOne<ApplicationRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_role_claims_roles");
        });
        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("user_logins");
            entity.HasKey(x => new { x.LoginProvider, x.ProviderKey }).HasName("pk_user_logins");
            entity.Property(x => x.LoginProvider).HasMaxLength(128);
            entity.Property(x => x.ProviderKey).HasMaxLength(128);
            entity.HasIndex(x => x.UserId).HasDatabaseName("ix_user_logins_user_id");
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_user_logins_users");
        });
        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("user_tokens");
            entity.HasKey(x => new { x.UserId, x.LoginProvider, x.Name }).HasName("pk_user_tokens");
            entity.Property(x => x.LoginProvider).HasMaxLength(128);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_user_tokens_users");
        });
        foreach (var entity in builder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
    }
}
