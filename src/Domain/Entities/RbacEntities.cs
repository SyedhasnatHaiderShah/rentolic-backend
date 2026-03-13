using Rentolic.Domain.Common;

namespace Rentolic.Domain.Entities;

public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsNavigation { get; set; }
    public string? NavPath { get; set; }
    public string? NavIcon { get; set; }
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }
}

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public Guid? GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}

public class PermissionAuditLog : BaseEntity
{
    public string Action { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public Guid? PermissionId { get; set; }
    public string? PermissionCode { get; set; }
    public Guid? PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; }
}
