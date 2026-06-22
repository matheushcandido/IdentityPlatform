using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/me")]
    public sealed class MeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                UserId = User.GetClaim(Claims.Subject),
                Email = User.GetClaim(Claims.Email),
                Scopes = User.GetScopes().ToArray()
            });
        }
    }
}
