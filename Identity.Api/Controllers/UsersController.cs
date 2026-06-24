using Identity.Api.Models;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Policy = "Users.Read")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetUsersAsync();

            return Ok(users.Select(ToResponse));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "Users.Read")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(ToResponse(user));
        }

        [HttpPost]
        [Authorize(Policy = "Users.Create")]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            var user = await _userService.CreateUser(request.Email, request.Password);
            if (user == null)
            {
                return Conflict(new { message = "A user with this email already exists." });
            }

            return Created($"/api/users/{user.Id}", new { user.Id, user.Email, user.IsActive });
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Policy = "Users.Update")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            var result = await _userService.UpdateUserEmailAsync(id, request.Email);

            return result.Status switch
            {
                UserMutationStatus.Success => Ok(ToResponse(result.User!)),
                UserMutationStatus.NotFound => NotFound(),
                UserMutationStatus.EmailAlreadyExists => Conflict(new { message = "A user with this email already exists." }),
                _ => throw new InvalidOperationException("Unexpected user update result.")
            };
        }

        [HttpPost("{id:guid}/disable")]
        [Authorize(Policy = "Users.Disable")]
        public async Task<IActionResult> Disable(Guid id)
        {
            var result = await _userService.DisableUserAsync(id);

            return result.Status switch
            {
                UserMutationStatus.Success => Ok(ToResponse(result.User!)),
                UserMutationStatus.NotFound => NotFound(),
                _ => throw new InvalidOperationException("Unexpected user disable result.")
            };
        }

        private static UserResponse ToResponse(Identity.Domain.Entities.User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                IsActive = user.IsActive,
                Roles = user.UserRoles
                    .Select(userRole => userRole.Role.Name)
                    .Distinct()
                    .OrderBy(role => role)
                    .ToArray(),
                Permissions = user.UserRoles
                    .SelectMany(userRole => userRole.Role.RolePermissions)
                    .Select(rolePermission => rolePermission.Permission.Name)
                    .Distinct()
                    .OrderBy(permission => permission)
                    .ToArray()
            };
        }
    }
}
