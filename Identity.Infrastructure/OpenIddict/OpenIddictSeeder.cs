using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

public class OpenIddictSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var manager = services.GetRequiredService<IOpenIddictApplicationManager>();

        var postmanDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "postman",
            ClientSecret = "secret",
            DisplayName = "Postman",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            RedirectUris =
            {
                new Uri("https://oauth.pstmn.io/v1/callback"),
                new Uri("https://www.getpostman.com/oauth2/callback")
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://oauth.pstmn.io/v1/callback"),
                new Uri("https://www.getpostman.com/oauth2/callback")
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access",
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        await CreateOrUpdateAsync(manager, "postman", postmanDescriptor);

        var angularDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "angular-portal-client",
            DisplayName = "Identity Portal (Angular)",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            RedirectUris =
            {
                new Uri("http://localhost:4200/callback")
            },
            PostLogoutRedirectUris =
            {
                new Uri("http://localhost:4200")
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access",
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        await CreateOrUpdateAsync(manager, "angular-portal-client", angularDescriptor);
    }

    private static async Task CreateOrUpdateAsync(
        IOpenIddictApplicationManager manager,
        string clientId,
        OpenIddictApplicationDescriptor descriptor)
    {
        var application = await manager.FindByClientIdAsync(clientId);
        if (application == null)
        {
            await manager.CreateAsync(descriptor);
            return;
        }

        await manager.UpdateAsync(application, descriptor);
    }
}