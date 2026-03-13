using Microsoft.EntityFrameworkCore;
using Rentolic.Domain.Entities;
using Rentolic.Domain.Enums;

namespace Rentolic.Infrastructure.Persistence.DbContext;

public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MaintenanceTeam> MaintenanceTeams => Set<MaintenanceTeam>();
    public DbSet<IssueReport> IssueReports => Set<IssueReport>();
    public DbSet<SecurityMainUser> SecurityMainUsers => Set<SecurityMainUser>();
    public DbSet<VisitorPermit> VisitorPermits => Set<VisitorPermit>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<ServiceProvider> ServiceProviders => Set<ServiceProvider>();
    public DbSet<ServiceListing> ServiceListings => Set<ServiceListing>();
    public DbSet<ServiceBooking> ServiceBookings => Set<ServiceBooking>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<FacilityBooking> FacilityBookings => Set<FacilityBooking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Lease>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.RentFrequency).HasConversion<string>();
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<IssueReport>(entity =>
        {
            entity.Property(e => e.Priority).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Incident>(entity =>
        {
            entity.Property(e => e.Severity).HasConversion<string>();
        });

        // Configuration for many-to-many user-role
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        // Additional relationships
        modelBuilder.Entity<Lease>()
            .HasOne(l => l.TenantUser)
            .WithMany()
            .HasForeignKey(l => l.TenantUserId);

        modelBuilder.Entity<Lease>()
            .HasOne(l => l.LandlordOrg)
            .WithMany()
            .HasForeignKey(l => l.LandlordOrgId);
    }
}
