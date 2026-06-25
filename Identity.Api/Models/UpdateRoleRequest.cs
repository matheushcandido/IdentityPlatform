using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Models;

public sealed class UpdateRoleRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;
}
