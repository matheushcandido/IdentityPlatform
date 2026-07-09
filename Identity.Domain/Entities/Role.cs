using Identity.Api.Models;
using Identity.Domain.Enums;

namespace Identity.Domain.Entities;


public class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public RoleType Type { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
