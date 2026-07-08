using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/assignments")]
    public sealed class AssignmentsController : ControllerBase
    {
        private readonly AssignmentService _assignmentService;

        public AssignmentsController(AssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpPost("users/{userId:guid}/roles/{roleId:guid}")]
        //[Authorize(Policy = "Assignments.Grant")]
        public async Task<IActionResult> AssignRoleToUser(Guid userId, Guid roleId)
        {
            var result = await _assignmentService.AssignRoleToUserAsync(userId, roleId);

            return result switch
            {
                null => NotFound(new { message = "User or Role not found." }),
                false => Conflict(new { message = "Role already assigned to this user." }),
                true => NoContent()
            };
        }

        [HttpDelete("users/{userId:guid}/roles/{roleId:guid}")]
        //[Authorize(Policy = "Assignments.Revoke")]
        public async Task<IActionResult> RemoveRoleFromUser(Guid userId, Guid roleId)
        {
            var result = await _assignmentService.RemoveRoleFromUserAsync(userId, roleId);

            return result switch
            {
                null => NotFound(new { message = "Assignment not found." }),
                _ => NoContent()
            };
        }

        [HttpPost("roles/{roleId:guid}/permissions/{permissionId:guid}")]
        //[Authorize(Policy = "Assignments.Grant")]
        public async Task<IActionResult> AssignPermissionToRole(Guid roleId, Guid permissionId)
        {
            var result = await _assignmentService.AssignPermissionToRoleAsync(roleId, permissionId);

            return result switch
            {
                null => NotFound(new { message = "Role or Permission not found." }),
                false => Conflict(new { message = "Permission already assigned to this role." }),
                true => NoContent()
            };
        }

        [HttpDelete("roles/{roleId:guid}/permissions/{permissionId:guid}")]
        //[Authorize(Policy = "Assignments.Revoke")]
        public async Task<IActionResult> RemovePermissionFromRole(Guid roleId, Guid permissionId)
        {
            var result = await _assignmentService.RemovePermissionFromRoleAsync(roleId, permissionId);

            return result switch
            {
                null => NotFound(new { message = "Assignment not found." }),
                _ => NoContent()
            };
        }
    }
}