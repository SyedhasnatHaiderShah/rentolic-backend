using Rentolic.Domain.Common;
using Rentolic.Domain.Enums;

namespace Rentolic.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.ACTIVE;

    public Profile? Profile { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class Profile : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
}

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public Guid? ParentRoleId { get; set; }
    public Role? ParentRole { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class Property : BaseAuditableEntity
{
    public Guid? LandlordId { get; set; }
    public User? Landlord { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    public int? TotalUnits { get; set; }

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}

public class Unit : BaseAuditableEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public string UnitNumber { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int? FloorNumber { get; set; }
    public int? Bedrooms { get; set; }
    public decimal? Bathrooms { get; set; }
    public decimal? AreaSqft { get; set; }
    public decimal? RentAmount { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.VACANT;
}
