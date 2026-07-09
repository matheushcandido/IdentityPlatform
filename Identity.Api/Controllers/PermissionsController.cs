using Identity.Api.Models;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/permissions")]
    public sealed class PermissionsController : ControllerBase
    {
        private readonly PermissionService _permissionService;

        public PermissionsController(PermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet]
        [Authorize(Policy = "Permissions.Read")]
        public async Task<IActionResult> GetAll()
        {
            var permissions = await _permissionService.GetPermissionsAsync();
            return Ok(permissions);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "Permissions.Read")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var permission = await _permissionService.GetPermissionByIdAsync(id);

            if (permission is null)
                return NotFound();

            return Ok(permission);
        }

        [HttpPost]
        [Authorize(Policy = "Permissions.Create")]
        public async Task<IActionResult> Create([FromBody] CreatePermissionRequest request)
        {
            var permission = await _permissionService.CreatePermissionAsync(request.Name, request.Description, request.Type);

            if (permission is null)
                return Conflict(new { message = "A permission with this name already exists." });

            return CreatedAtAction(nameof(GetById), new { id = permission.Id }, permission);
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Policy = "Permissions.Update")]
        public async Task<IActionResult> Patch(Guid id, [FromBody] UpdatePermissionRequest request)
        {
            var permission = await _permissionService.UpdatePermissionAsync(id, request.Name, request.Description);

            if (permission is null)
                return NotFound();

            return Ok(permission);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "Permissions.Delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _permissionService.DeletePermissionAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}