using Rentolic.Domain.Common;

namespace Rentolic.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? Diff { get; set; } // JSON string of changes
    public string? Ip { get; set; }
    public string? Ua { get; set; }
}
