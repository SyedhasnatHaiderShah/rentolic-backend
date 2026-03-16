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

    // New Entities
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<PermissionAuditLog> PermissionAuditLogs => Set<PermissionAuditLog>();
    public DbSet<MaintenanceSubUser> MaintenanceSubUsers => Set<MaintenanceSubUser>();
    public DbSet<SecuritySubUser> SecuritySubUsers => Set<SecuritySubUser>();
    public DbSet<LandlordSubUser> LandlordSubUsers => Set<LandlordSubUser>();
    public DbSet<ServiceProviderSubUser> ServiceProviderSubUsers => Set<ServiceProviderSubUser>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<CommunityChannel> CommunityChannels => Set<CommunityChannel>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<UtilityMeter> UtilityMeters => Set<UtilityMeter>();
    public DbSet<MoveWorkflow> MoveWorkflows => Set<MoveWorkflow>();
    public DbSet<TenantProfile> TenantProfiles => Set<TenantProfile>();
    public DbSet<LeasePaymentHistory> LeasePaymentHistories => Set<LeasePaymentHistory>();
    public DbSet<WorkOrderPayment> WorkOrderPayments => Set<WorkOrderPayment>();
    public DbSet<WorkOrderQuote> WorkOrderQuotes => Set<WorkOrderQuote>();
    public DbSet<ChannelPost> ChannelPosts => Set<ChannelPost>();
    public DbSet<PostReply> PostReplies => Set<PostReply>();
    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();
    public DbSet<TenantApartmentAssignment> TenantApartmentAssignments => Set<TenantApartmentAssignment>();
    public DbSet<LeaseDocument> LeaseDocuments => Set<LeaseDocument>();
    public DbSet<InsurancePolicy> InsurancePolicies => Set<InsurancePolicy>();
    public DbSet<TerminationRequest> TerminationRequests => Set<TerminationRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enum Conversions
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Unit>(entity => entity.Property(e => e.Status).HasConversion<string>());
        modelBuilder.Entity<Lease>(entity => {
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.RentFrequency).HasConversion<string>();
        });
        modelBuilder.Entity<Invoice>(entity => entity.Property(e => e.Status).HasConversion<string>());
        modelBuilder.Entity<IssueReport>(entity => {
            entity.Property(e => e.Priority).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });
        modelBuilder.Entity<Incident>(entity => entity.Property(e => e.Severity).HasConversion<string>());
        modelBuilder.Entity<Device>(entity => {
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });
        modelBuilder.Entity<CommunityChannel>(entity => entity.Property(e => e.ChannelType).HasConversion<string>());

        // RBAC Many-to-Many
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        modelBuilder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Relationships
        modelBuilder.Entity<Lease>().HasOne(l => l.TenantUser).WithMany().HasForeignKey(l => l.TenantUserId);
        modelBuilder.Entity<Lease>().HasOne(l => l.LandlordOrg).WithMany().HasForeignKey(l => l.LandlordOrgId);

        // Profiles unique per user
        modelBuilder.Entity<Profile>().HasIndex(p => p.UserId).IsUnique();
    }
}
