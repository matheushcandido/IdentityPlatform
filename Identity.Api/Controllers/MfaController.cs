using Identity.Api.Models;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mfa/totp")]
public sealed class MfaController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly TotpService _totpService;
    private readonly ApplicationDbContext _db;

    public MfaController(AuthService authService, TotpService totpService, ApplicationDbContext db)
    {
        _authService = authService;
        _totpService = totpService;
        _db = db;
    }

    [HttpPost("setup")]
    public async Task<IActionResult> Setup()
    {
        var user = await GetCurrentUser();
        if (user is null) return Unauthorized();
        if (user.IsTotpEnabled && !User.HasClaim(Claims.AuthenticationMethodReference, "mfa"))
            return Forbid();

        var secret = _totpService.GenerateSecret();
        _authService.SetTotpSecret(user, secret);
        user.IsTotpEnabled = false;
        await _db.SaveChangesAsync();

        return Ok(new { secret, otpauthUri = _totpService.CreateProvisioningUri(user.Email, secret) });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] TotpCodeRequest request)
    {
        var user = await GetCurrentUser();
        var secret = user is null ? null : _authService.GetTotpSecret(user);
        if (user is null) return Unauthorized();
        if (secret is null) return BadRequest(new { message = "Start TOTP setup before confirming it." });
        if (!_totpService.ValidateCode(secret, request.Code)) return BadRequest(new { message = "Invalid authenticator code." });

        user.IsTotpEnabled = true;
        await _db.SaveChangesAsync();
        return Ok(new { enabled = true });
    }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable([FromBody] TotpCodeRequest request)
    {
        var user = await GetCurrentUser();
        var secret = user is null ? null : _authService.GetTotpSecret(user);
        if (user is null) return Unauthorized();
        if (!user.IsTotpEnabled || secret is null) return BadRequest(new { message = "TOTP is not enabled." });
        if (!_totpService.ValidateCode(secret, request.Code)) return BadRequest(new { message = "Invalid authenticator code." });

        user.TotpSecret = null;
        user.IsTotpEnabled = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Identity.Domain.Entities.User?> GetCurrentUser()
    {
        var subject = User.GetClaim(Claims.Subject) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out var userId) ? await _authService.GetActiveUserAsync(userId) : null;
    }
}
