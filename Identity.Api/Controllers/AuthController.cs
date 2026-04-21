using Identity.Api.Authentication;
using Identity.Api.Models;
using Identity.Api.Services;
using Identity.Domain.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

[AllowAnonymous]
public class AuthController : Controller
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var authentication = await HttpContext.AuthenticateAsync(AuthenticationSchemes.InteractiveCookie);
        if (!authentication.Succeeded)
        {
            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = $"{Request.Path}{Request.QueryString}"
                },
                AuthenticationSchemes.InteractiveCookie);
        }

        var userId = authentication.Principal?.FindFirstValue(Claims.Subject);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            await HttpContext.SignOutAsync(AuthenticationSchemes.InteractiveCookie);

            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = $"{Request.Path}{Request.QueryString}"
                },
                AuthenticationSchemes.InteractiveCookie);
        }

        var user = await _authService.GetUserByIdAsync(parsedUserId);
        if (user == null)
        {
            await HttpContext.SignOutAsync(AuthenticationSchemes.InteractiveCookie);

            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = $"{Request.Path}{Request.QueryString}"
                },
                AuthenticationSchemes.InteractiveCookie);
        }

        var principal = CreatePrincipal(user, request.GetScopes());
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var userId = result.Principal?.GetClaim(Claims.Subject);
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return InvalidGrant("The authorization data is no longer valid.");
            }

            var user = await _authService.GetUserByIdAsync(parsedUserId);
            if (user == null || !user.IsActive)
            {
                return InvalidGrant("The refresh token is no longer valid.");
            }

            var principal = CreatePrincipal(user, result.Principal!.GetScopes());
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("Grant type not supported.");
    }

    [HttpGet("~/account/login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        return Content(BuildLoginPage(returnUrl), "text/html");
    }

    [HttpPost("~/account/login")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        {
            return Content(BuildLoginPage(input.ReturnUrl, "Email and password are required."), "text/html");
        }

        var user = await _authService.ValidateUser(input.Email, input.Password);
        if (user == null || !user.IsActive)
        {
            return Content(BuildLoginPage(input.ReturnUrl, "Invalid credentials."), "text/html");
        }

        var identity = new ClaimsIdentity(
            AuthenticationSchemes.InteractiveCookie,
            Claims.Email,
            Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, user.Id.ToString()));
        identity.AddClaim(new Claim(Claims.Email, user.Email));

        await HttpContext.SignInAsync(
            AuthenticationSchemes.InteractiveCookie,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return LocalRedirect(GetSafeReturnUrl(input.ReturnUrl));
    }

    [HttpGet("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthenticationSchemes.InteractiveCookie);
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private IActionResult InvalidGrant(string description)
    {
        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
    }

    private static ClaimsPrincipal CreatePrincipal(User user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Email,
            Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, user.Id.ToString()));
        identity.AddClaim(new Claim(Claims.Email, user.Email));

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        principal.SetResources("resource_server");
        principal.SetDestinations(static claim => claim.Type switch
        {
            Claims.Email when claim.Subject.HasScope(Scopes.Email) =>
            [
                Destinations.AccessToken
            ],
            Claims.Subject =>
            [
                Destinations.AccessToken
            ],
            _ => []
        });

        return principal;
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
    }

    private static string BuildLoginPage(string? returnUrl, string? error = null)
    {
        var safeReturnUrl = System.Net.WebUtility.HtmlEncode(returnUrl ?? "/");
        var errorMarkup = string.IsNullOrWhiteSpace(error)
            ? string.Empty
            : $"<p style=\"color:#b91c1c;margin-bottom:16px;\">{System.Net.WebUtility.HtmlEncode(error)}</p>";

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Identity Login</title>
</head>
<body style="font-family:Segoe UI,Arial,sans-serif;background:#f8fafc;color:#0f172a;">
    <main style="max-width:420px;margin:80px auto;padding:32px;background:#ffffff;border:1px solid #e2e8f0;border-radius:16px;">
        <h1 style="margin-top:0;">Sign in</h1>
        <p style="color:#475569;">Use your account to continue the authorization flow.</p>
        {{errorMarkup}}
        <form method="post" action="/account/login">
            <input type="hidden" name="ReturnUrl" value="{{safeReturnUrl}}" />
            <label for="email" style="display:block;margin-bottom:8px;">Email</label>
            <input id="email" name="Email" type="email" autocomplete="username" required style="width:100%;padding:12px;margin-bottom:16px;border:1px solid #cbd5e1;border-radius:10px;" />
            <label for="password" style="display:block;margin-bottom:8px;">Password</label>
            <input id="password" name="Password" type="password" autocomplete="current-password" required style="width:100%;padding:12px;margin-bottom:20px;border:1px solid #cbd5e1;border-radius:10px;" />
            <button type="submit" style="width:100%;padding:12px;background:#0f172a;color:#ffffff;border:none;border-radius:10px;">Continue</button>
        </form>
    </main>
</body>
</html>
""";
    }
}
