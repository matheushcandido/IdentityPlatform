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
using QRCoder;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

[AllowAnonymous]
public class AuthController : Controller
{
    private const string PermissionClaimType = "permission";
    private readonly AuthService _authService;
    private readonly TotpService _totpService;
    private readonly ApplicationDbContext _db;

    public AuthController(AuthService authService, TotpService totpService, ApplicationDbContext db)
    {
        _authService = authService;
        _totpService = totpService;
        _db = db;
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

        var user = await _authService.GetUserWithAccessAsync(parsedUserId);
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

        var mfaAuthenticated = authentication.Principal?.HasClaim(Claims.AuthenticationMethodReference, "mfa") == true;
        if (user.IsTotpEnabled && !mfaAuthenticated)
        {
            await HttpContext.SignOutAsync(AuthenticationSchemes.InteractiveCookie);
            return Challenge(new AuthenticationProperties { RedirectUri = $"{Request.Path}{Request.QueryString}" }, AuthenticationSchemes.InteractiveCookie);
        }

        var principal = CreatePrincipal(user, request.GetScopes(), mfaAuthenticated);
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

            var user = await _authService.GetUserWithAccessAsync(parsedUserId);
            if (user == null || !user.IsActive)
            {
                return InvalidGrant("The refresh token is no longer valid.");
            }

            var mfaAuthenticated = result.Principal!.HasClaim(Claims.AuthenticationMethodReference, "mfa");
            if (user.IsTotpEnabled && !mfaAuthenticated)
                return InvalidGrant("Multi-factor authentication is required.");

            var principal = CreatePrincipal(user, result.Principal.GetScopes(), mfaAuthenticated);
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

        await StartMfaChallengeAsync(user);
        var returnUrl = Uri.EscapeDataString(GetSafeReturnUrl(input.ReturnUrl));
        return LocalRedirect(user.IsTotpEnabled
            ? $"/account/mfa/verify?returnUrl={returnUrl}"
            : $"/account/mfa/setup?returnUrl={returnUrl}");
    }

    [HttpGet("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthenticationSchemes.InteractiveCookie);
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/account/mfa/setup")]
    public async Task<IActionResult> SetupMfa([FromQuery] string? returnUrl = null)
    {
        var user = await GetMfaPendingUserAsync();
        if (user is null) return MfaChallengeExpired();
        if (user.IsTotpEnabled) return LocalRedirect($"/account/mfa/verify?returnUrl={Uri.EscapeDataString(GetSafeReturnUrl(returnUrl))}");

        var secret = _totpService.GenerateSecret();
        _authService.SetTotpSecret(user, secret);
        await _db.SaveChangesAsync();
        return Content(BuildMfaSetupPage(user, returnUrl), "text/html");
    }

    [HttpGet("~/account/mfa/qrcode")]
    public async Task<IActionResult> MfaQrCode()
    {
        var user = await GetMfaPendingUserAsync();
        var secret = user is null ? null : _authService.GetTotpSecret(user);
        if (secret is null) return NotFound();

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(_totpService.CreateProvisioningUri(user!.Email, secret), QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data).GetGraphic(5);
        return Content(svg, "image/svg+xml");
    }

    [HttpPost("~/account/mfa/setup/complete")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CompleteMfaSetup([FromForm] string? returnUrl)
    {
        if (await GetMfaPendingUserAsync() is null) return MfaChallengeExpired();
        return LocalRedirect($"/account/mfa/verify?returnUrl={Uri.EscapeDataString(GetSafeReturnUrl(returnUrl))}");
    }

    [HttpGet("~/account/mfa/verify")]
    public async Task<IActionResult> VerifyMfa([FromQuery] string? returnUrl = null)
    {
        var user = await GetMfaPendingUserAsync();
        if (user is null) return MfaChallengeExpired();
        return Content(BuildMfaVerificationPage(user, returnUrl), "text/html");
    }

    [HttpPost("~/account/mfa/verify")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> VerifyMfa([FromForm] string? code, [FromForm] string? returnUrl)
    {
        var user = await GetMfaPendingUserAsync();
        var secret = user is null ? null : _authService.GetTotpSecret(user);
        if (user is null || secret is null) return MfaChallengeExpired();
        if (!_totpService.ValidateCode(secret, code))
            return Content(BuildMfaVerificationPage(user, returnUrl, "Invalid authenticator code."), "text/html");

        if (!user.IsTotpEnabled)
        {
            user.IsTotpEnabled = true;
            await _db.SaveChangesAsync();
        }

        await CompleteInteractiveLoginAsync(user);
        return LocalRedirect(GetSafeReturnUrl(returnUrl));
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

    private static ClaimsPrincipal CreatePrincipal(User user, IEnumerable<string> scopes, bool mfaAuthenticated)
    {
        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Email,
            Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, user.Id.ToString()));
        identity.AddClaim(new Claim(Claims.Email, user.Email));
        if (mfaAuthenticated)
            identity.AddClaim(new Claim(Claims.AuthenticationMethodReference, "mfa"));
        foreach (var role in user.UserRoles.Select(userRole => userRole.Role.Name).Distinct())
        {
            identity.AddClaim(new Claim(Claims.Role, role));
        }

        var permissions = user.UserRoles
            .SelectMany(userRole => userRole.Role.RolePermissions)
            .Select(rolePermission => rolePermission.Permission.Name)
            .Distinct();

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(PermissionClaimType, permission));
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        principal.SetResources("resource_server");
        principal.SetDestinations(static claim => claim.Type switch
        {
            Claims.Email when claim.Subject?.HasScope(Scopes.Email) == true =>
            [
                Destinations.AccessToken,
        Destinations.IdentityToken
            ],
            Claims.Subject =>
            [
                Destinations.AccessToken,
        Destinations.IdentityToken
            ],
            Claims.Role or PermissionClaimType or Claims.AuthenticationMethodReference =>
            [
                Destinations.AccessToken,
        Destinations.IdentityToken
            ],
            _ => []
        });

        return principal;
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
    }

    private async Task StartMfaChallengeAsync(User user)
    {
        var identity = new ClaimsIdentity(AuthenticationSchemes.MfaPendingCookie);
        identity.AddClaim(new Claim(Claims.Subject, user.Id.ToString()));
        await HttpContext.SignInAsync(
            AuthenticationSchemes.MfaPendingCookie,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5), IsPersistent = false });
    }

    private async Task<User?> GetMfaPendingUserAsync()
    {
        var authentication = await HttpContext.AuthenticateAsync(AuthenticationSchemes.MfaPendingCookie);
        var subject = authentication.Principal?.FindFirstValue(Claims.Subject);
        return authentication.Succeeded && Guid.TryParse(subject, out var userId)
            ? await _authService.GetActiveUserAsync(userId)
            : null;
    }

    private ChallengeResult MfaChallengeExpired() => Challenge(
        new AuthenticationProperties { RedirectUri = $"{Request.Path}{Request.QueryString}" },
        AuthenticationSchemes.InteractiveCookie);

    private async Task CompleteInteractiveLoginAsync(User user)
    {
        var identity = new ClaimsIdentity(AuthenticationSchemes.InteractiveCookie, Claims.Email, Claims.Role);
        identity.AddClaim(new Claim(Claims.Subject, user.Id.ToString()));
        identity.AddClaim(new Claim(Claims.Email, user.Email));
        identity.AddClaim(new Claim(Claims.AuthenticationMethodReference, "mfa"));

        await HttpContext.SignInAsync(
            AuthenticationSchemes.InteractiveCookie,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, IssuedUtc = DateTimeOffset.UtcNow });
        await HttpContext.SignOutAsync(AuthenticationSchemes.MfaPendingCookie);
    }

    private static string BuildMfaSetupPage(User user, string? returnUrl)
    {
        return $$"""
<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Set up MFA</title></head>
<body style="font-family:Segoe UI,Arial,sans-serif;background:#f8fafc;color:#0f172a;"><main style="max-width:560px;margin:80px auto;padding:32px;background:#fff;border:1px solid #e2e8f0;border-radius:16px;">
<h1>Set up your authenticator</h1><p>Scan this QR code with Google Authenticator, Microsoft Authenticator or another compatible app.</p>
<img src="/account/mfa/qrcode" width="250" height="250" alt="Authenticator setup QR code" />
<form method="post" action="/account/mfa/setup/complete"><input type="hidden" name="returnUrl" value="{{System.Net.WebUtility.HtmlEncode(returnUrl ?? "/")}}" /><button type="submit">I added the account</button></form>
</main></body></html>
""";
    }

    private static string BuildMfaVerificationPage(User user, string? returnUrl, string? error = null)
    {
        var errorMarkup = string.IsNullOrWhiteSpace(error) ? string.Empty : $"<p style=\"color:#b91c1c;\">{System.Net.WebUtility.HtmlEncode(error)}</p>";
        return $$"""
<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Verify authenticator</title></head>
<body style="font-family:Segoe UI,Arial,sans-serif;background:#f8fafc;color:#0f172a;"><main style="max-width:420px;margin:80px auto;padding:32px;background:#fff;border:1px solid #e2e8f0;border-radius:16px;">
<h1>Verify your identity</h1><p>Enter the current code from your authenticator for {{System.Net.WebUtility.HtmlEncode(user.Email)}}.</p>{{errorMarkup}}
<form method="post" action="/account/mfa/verify"><input type="hidden" name="returnUrl" value="{{System.Net.WebUtility.HtmlEncode(returnUrl ?? "/")}}" />
<label for="code">Authenticator code</label><input id="code" name="code" inputmode="numeric" autocomplete="one-time-code" pattern="[0-9]{6}" maxlength="6" required style="display:block;width:100%;padding:12px;margin:8px 0 16px;box-sizing:border-box;" />
<button type="submit">Verify</button></form>
</main></body></html>
""";
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
