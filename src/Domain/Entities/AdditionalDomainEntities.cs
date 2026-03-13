using Rentolic.Domain.Common;

namespace Rentolic.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime? ReadAt { get; set; }
}

public class Document : BaseAuditableEntity
{
    public Guid? LandlordId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool IsSharedWithTenant { get; set; }
}

public class Facility : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}

public class FacilityBooking : BaseEntity
{
    public Guid FacilityId { get; set; }
    public Guid BookedBy { get; set; }
    public DateTime BookingDate { get; set; }
    public string Status { get; set; } = "PENDING";
}
