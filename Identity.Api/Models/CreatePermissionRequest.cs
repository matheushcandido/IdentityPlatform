using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Models;

public sealed class CreatePermissionRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string Description { get; init; } = string.Empty;
}
