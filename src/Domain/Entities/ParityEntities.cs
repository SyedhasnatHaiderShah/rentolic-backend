using Rentolic.Domain.Common;

namespace Rentolic.Domain.Entities;

public class LeasePaymentHistory : BaseEntity
{
    public Guid LeasePaymentId { get; set; }
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public Guid ChangedBy { get; set; }
}

public class WorkOrderPayment : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public Guid TenantId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "PENDING";
}

public class WorkOrderQuote : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public Guid ProviderId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "PENDING";
}

public class ChannelPost : BaseEntity
{
    public Guid ChannelId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class PostReply : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class DirectMessage : BaseEntity
{
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}

public class TenantApartmentAssignment : BaseEntity
{
    public Guid TenantUserId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid UnitId { get; set; }
    public string Status { get; set; } = "PENDING";
}

public class LeaseDocument : BaseEntity
{
    public Guid LeaseId { get; set; }
    public Guid TenantId { get; set; }
    public Guid LandlordId { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string? HtmlContent { get; set; }
}

public class InsurancePolicy : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
}

public class TerminationRequest : BaseEntity
{
    public Guid LeaseId { get; set; }
    public Guid RequestedBy { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? Reason { get; set; }
}
