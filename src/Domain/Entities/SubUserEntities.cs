using Rentolic.Domain.Common;

namespace Rentolic.Domain.Entities;

public class MaintenanceSubUser : BaseEntity
{
    public Guid MainUserId { get; set; }
    public Guid SubUserId { get; set; }
    public Guid? MaintenanceTeamId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Permissions { get; set; } // JSONB
    public bool Active { get; set; } = true;
}

public class SecuritySubUser : BaseEntity
{
    public Guid MainUserId { get; set; }
    public Guid SubUserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Permissions { get; set; } // JSONB
    public bool Active { get; set; } = true;
}

public class LandlordSubUser : BaseEntity
{
    public Guid LandlordId { get; set; }
    public Guid SubUserId { get; set; }
    public string AccessLevel { get; set; } = string.Empty;
    public string? Permissions { get; set; } // JSONB
    public bool Active { get; set; } = true;
}

public class ServiceProviderSubUser : BaseEntity
{
    public Guid MainProviderId { get; set; }
    public Guid SubUserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Permissions { get; set; } // JSONB
    public bool Active { get; set; } = true;
}
