using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentolic.Domain.Entities;

namespace Rentolic.Infrastructure.Persistence.Configurations;

public class IdentityConfiguration :
    IEntityTypeConfiguration<User>,
    IEntityTypeConfiguration<Profile>,
    IEntityTypeConfiguration<Role>,
    IEntityTypeConfiguration<UserRole>,
    IEntityTypeConfiguration<Permission>,
    IEntityTypeConfiguration<RolePermission>,
    IEntityTypeConfiguration<TenantProfile>,
    IEntityTypeConfiguration<LandlordSubUser>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "public");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.HasIndex(e => e.Email).IsUnique();
        builder.Property(e => e.Status).HasConversion<string>();
    }

    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("Profiles");
        builder.HasKey(e => e.Id);
        builder.HasOne(p => p.User).WithOne().HasForeignKey<Profile>(p => p.UserId);
    }

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "public");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.HasIndex(e => e.Name).IsUnique();
    }

    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", "public");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });
    }

    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(e => e.Id);
    }

    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
    }

    public void Configure(EntityTypeBuilder<TenantProfile> builder)
    {
        builder.ToTable("TenantProfiles");
        builder.HasKey(e => e.Id);
    }

    public void Configure(EntityTypeBuilder<LandlordSubUser> builder)
    {
        builder.ToTable("LandlordSubUsers");
        builder.HasKey(e => e.Id);
    }
}
