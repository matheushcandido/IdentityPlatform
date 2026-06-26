using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Models;

public sealed class UpdatePermissionRequest
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}
