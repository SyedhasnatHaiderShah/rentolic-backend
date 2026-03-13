using Rentolic.Domain.Common;

namespace Rentolic.Domain.Entities;

public class Inspection : BaseAuditableEntity
{
    public Guid PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid InspectorId { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime ScheduledDate { get; set; }
}

public class Subscription : BaseEntity
{
    public Guid UserId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public DateTime ExpiryDate { get; set; }
}

public class UtilityMeter : BaseEntity
{
    public Guid UnitId { get; set; }
    public string MeterType { get; set; } = string.Empty;
    public string MeterNumber { get; set; } = string.Empty;
}

public class MoveWorkflow : BaseEntity
{
    public Guid LeaseId { get; set; }
    public string Type { get; set; } = "MOVE_IN"; // MOVE_IN, MOVE_OUT
    public string Status { get; set; } = "PENDING";
}

public class TenantProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string? NationalId { get; set; }
    public string? EmiratesId { get; set; }
    public string? Nationality { get; set; }
}
