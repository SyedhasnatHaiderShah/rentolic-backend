using Rentolic.Domain.Common;

namespace Rentolic.Domain.Entities;

public class SecurityMainUser : BaseEntity
{
    public Guid LandlordId { get; set; }
    public Guid MainUserId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool Active { get; set; } = true;
}

public class VisitorPermit : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public Guid? UnitId { get; set; }
    public Guid TenantUserId { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? QrCode { get; set; }
}

public enum IncidentSeverity { LOW, MEDIUM, HIGH, CRITICAL }

public class Incident : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public Guid ReportedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.LOW;
    public string Status { get; set; } = "OPEN";
}
