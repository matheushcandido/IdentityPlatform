namespace Identity.Api.Models;

public sealed class UserResponse
{
    public Guid Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public string[] Roles { get; init; } = [];

    public string[] Permissions { get; init; } = [];
}
