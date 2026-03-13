using System;

namespace Rentolic.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime? DeletedAt { get; set; }
}
