using Rentolic.Domain.Common;
using Rentolic.Domain.Enums;

namespace Rentolic.Domain.Entities;

public class MaintenanceTeam : BaseEntity
{
    public Guid? LandlordId { get; set; }
    public User? Landlord { get; set; }
    public Guid? MainUserId { get; set; }
    public User? MainUser { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string[]? Specialties { get; set; }
    public bool Active { get; set; } = true;
}

public class IssueReport : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }
    public Guid TenantUserId { get; set; }
    public User TenantUser { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Priority Priority { get; set; } = Priority.MEDIUM;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.NEW;
    public string[]? Images { get; set; }
    public Guid? AssignedMaintenanceTeamId { get; set; }
    public MaintenanceTeam? AssignedMaintenanceTeam { get; set; }
    public decimal? CostEstimate { get; set; }
    public decimal? ActualCost { get; set; }
    public bool IsPaid { get; set; }
    public bool IsEmergency { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedAt { get; set; }
}
