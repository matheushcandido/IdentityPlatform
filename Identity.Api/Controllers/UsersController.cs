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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            var user = await _userService.CreateUser(request.Email, request.Password);
            if (user == null)
            {
                return Conflict(new { message = "A user with this email already exists." });
            }

            return Created($"/api/users/{user.Id}", new { user.Id, user.Email, user.IsActive });
        }
    }
}
