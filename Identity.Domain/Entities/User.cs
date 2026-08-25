namespace Identity.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    // Protected with ASP.NET Core Data Protection before being persisted.
    public string? TotpSecret { get; set; }

    public bool IsTotpEnabled { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
