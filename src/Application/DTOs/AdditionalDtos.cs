namespace Rentolic.Application.DTOs;

public class VisitorPermitDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class IncidentDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ServiceListingDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal? BasePrice { get; set; }
}

public class ServiceBookingDto
{
    public Guid Id { get; set; }
    public Guid ServiceListingId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
