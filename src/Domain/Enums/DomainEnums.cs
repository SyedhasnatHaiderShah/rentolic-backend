namespace Rentolic.Domain.Enums;

public enum UserStatus
{
    ACTIVE,
    INACTIVE,
    SUSPENDED,
    DELETED
}

public enum UnitStatus
{
    VACANT,
    OCCUPIED,
    MAINTENANCE,
    RESERVED
}

public enum LeaseStatus
{
    DRAFT,
    ACTIVE,
    EXPIRED,
    TERMINATED,
    PENDING
}

public enum RentFrequency
{
    MONTHLY,
    QUARTERLY,
    YEARLY
}

public enum InvoiceStatus
{
    OPEN,
    PAID,
    OVERDUE,
    CANCELLED,
    PARTIAL
}

public enum Priority
{
    LOW,
    MEDIUM,
    HIGH,
    EMERGENCY
}

public enum WorkOrderStatus
{
    NEW,
    ASSIGNED,
    IN_PROGRESS,
    PENDING_APPROVAL,
    APPROVED,
    REJECTED,
    COMPLETED,
    CANCELLED,
    ON_HOLD,
    BIDDING
}
