using Rentolic.Domain.Common;

namespace Rentolic.Domain.Entities;

public class ServiceProvider : BaseEntity
{
    public string? BusinessName { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string[]? ServicesOffered { get; set; }
    public decimal? Rating { get; set; }
    public bool Approved { get; set; }
}

public class ServiceListing : BaseEntity
{
    public Guid ProviderId { get; set; }
    public ServiceProvider Provider { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal? BasePrice { get; set; }
    public bool Active { get; set; } = true;
}

public class ServiceBooking : BaseEntity
{
    public Guid ServiceListingId { get; set; }
    public ServiceListing ServiceListing { get; set; } = null!;
    public Guid TenantUserId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ProviderId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = "PENDING";
    public decimal? TotalAmount { get; set; }
}
