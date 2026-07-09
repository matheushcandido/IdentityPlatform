using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

public class Permission
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PermissionType Type { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
