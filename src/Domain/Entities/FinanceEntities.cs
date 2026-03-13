using Rentolic.Domain.Common;
using Rentolic.Domain.Enums;

namespace Rentolic.Domain.Entities;

public class Lease : BaseAuditableEntity
{
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public Guid TenantUserId { get; set; }
    public User TenantUser { get; set; } = null!;
    public Guid LandlordOrgId { get; set; }
    public User LandlordOrg { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public RentFrequency RentFrequency { get; set; }
    public string PaymentFrequencyText { get; set; } = "MONTHLY";
    public decimal? SecurityDeposit { get; set; }
    public string? PaymentMethod { get; set; }
    public bool AutoPayment { get; set; }
    public string MaintenanceResponsibility { get; set; } = "LANDLORD";
    public LeaseStatus Status { get; set; } = LeaseStatus.DRAFT;

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public class Invoice : BaseAuditableEntity
{
    public Guid? LeaseId { get; set; }
    public Lease? Lease { get; set; }
    public Guid? TenantUserId { get; set; }
    public User? TenantUser { get; set; }
    public string Number { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AED";
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.OPEN;
    public string? Meta { get; set; } // JSONB in DB

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class Payment : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AED";
    public string Method { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ProviderPaymentId { get; set; }
    public string? TransactionReference { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentProofUrl { get; set; }
    public string Status { get; set; } = "PENDING";
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
