using Rentolic.Domain.Common;
using Rentolic.Domain.Enums;

namespace Rentolic.Domain.Entities;

public class Device : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public DeviceType Type { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public DeviceStatus Status { get; set; } = DeviceStatus.OFFLINE;
}

public class OtpCode : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // login_verification, email_verification, password_reset
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

public class CommunityChannel : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChannelType ChannelType { get; set; } = ChannelType.GENERAL;
    public bool IsModerated { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedBy { get; set; }
}
