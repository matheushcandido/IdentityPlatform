using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Models;

public sealed class UpdateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
