using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentolic.Domain.Entities;

namespace Rentolic.Infrastructure.Persistence.Configurations;

public class MaintenanceSecurityConfiguration :
    IEntityTypeConfiguration<MaintenanceTeam>,
    IEntityTypeConfiguration<IssueReport>,
    IEntityTypeConfiguration<MaintenanceSubUser>,
    IEntityTypeConfiguration<SecurityMainUser>,
    IEntityTypeConfiguration<SecuritySubUser>,
    IEntityTypeConfiguration<VisitorPermit>,
    IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<MaintenanceTeam> builder)
    {
        builder.ToTable("MaintenanceTeams");
        builder.HasKey(e => e.Id);
    }

    public void Configure(EntityTypeBuilder<IssueReport> builder)
    {
        builder.ToTable("issue_reports", "public");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Priority).HasConversion<string>();
        builder.Property(e => e.Status).HasConversion<string>();
    }

    public void Configure(EntityTypeBuilder<MaintenanceSubUser> builder)
    {
        builder.ToTable("MaintenanceSubUsers");
        builder.HasKey(e => e.Id);
    }

    public void Configure(EntityTypeBuilder<SecurityMainUser> builder)
    {
        builder.ToTable("SecurityMainUsers");
        builder.HasKey(e => e.Id);
    }

    public void Configure(EntityTypeBuilder<SecuritySubUser> builder)
    {
        builder.ToTable("SecuritySubUsers");
        builder.HasKey(e => e.Id);
    }

    public void Configure(EntityTypeBuilder<VisitorPermit> builder)
    {
        builder.ToTable("VisitorPermits");
        builder.HasKey(e => e.Id);
    }

    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents", "public");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Severity).HasConversion<string>();
    }
}
