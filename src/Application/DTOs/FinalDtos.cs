namespace Rentolic.Application.DTOs;

public class InspectionDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
}

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
}

public class UtilityDto
{
    public Guid Id { get; set; }
    public string MeterType { get; set; } = string.Empty;
    public string MeterNumber { get; set; } = string.Empty;
}

public class MoveWorkflowDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
