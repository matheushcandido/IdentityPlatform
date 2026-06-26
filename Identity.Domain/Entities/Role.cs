namespace Identity.Domain.Entities;

public enum RoleType
{
    Portal = 0,   // interno
    Target = 1    // sistema externo
}

public class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public RoleType Type { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
