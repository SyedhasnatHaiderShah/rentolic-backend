using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentolic.Domain.Entities;

namespace Rentolic.Infrastructure.Persistence.Configurations;

public class PropertyFinanceConfiguration :
    IEntityTypeConfiguration<Property>,
    IEntityTypeConfiguration<Unit>,
    IEntityTypeConfiguration<Lease>,
    IEntityTypeConfiguration<Invoice>,
    IEntityTypeConfiguration<Payment>,
    IEntityTypeConfiguration<LeasePaymentHistory>,
    IEntityTypeConfiguration<TenantApartmentAssignment>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties", "public");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
    }

    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("units", "public");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Status).HasConversion<string>();
        builder.HasOne(u => u.Property).WithMany().HasForeignKey(u => u.PropertyId);
    }

    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.ToTable("leases", "public");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Status).HasConversion<string>();
        builder.Property(e => e.RentFrequency).HasConversion<string>();
        builder.HasOne(l => l.Unit).WithMany().HasForeignKey(l => l.UnitId);
        builder.HasOne(l => l.TenantUser).WithMany().HasForeignKey(l => l.TenantUserId);
    }

    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasConversion<string>();
    }

    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(e => e.Id);
        // Note: Status is string in entity
    }

    public void Configure(EntityTypeBuilder<LeasePaymentHistory> builder)
    {
        builder.ToTable("LeasePaymentHistories");
        builder.HasKey(e => e.Id);
    }

    public void Configure(EntityTypeBuilder<TenantApartmentAssignment> builder)
    {
        builder.ToTable("TenantApartmentAssignments");
        builder.HasKey(e => e.Id);
    }
}
