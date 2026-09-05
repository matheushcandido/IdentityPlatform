using Azure.Identity;
using Identity.Connectors.Interfaces;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Connectors
{
    public class AzureAdGraphConnector : IConnectorAdapter
    {
        private readonly GraphServiceClient _graphClient;

        public AzureAdGraphConnector(AzureAdConnectorSettings settings)
        {
            var credential = new ClientSecretCredential(
                settings.TenantId, settings.ClientId, settings.ClientSecret);

            _graphClient = new GraphServiceClient(credential,
                new[] { "https://graph.microsoft.com/.default" });
        }

        public async Task<IReadOnlyList<ExternalAccount>> FullSyncAsync(CancellationToken ct = default)
        {
            var result = await _graphClient.Users.GetAsync(cfg =>
            {
                cfg.QueryParameters.Select = new[] { "id", "mail", "displayName", "accountEnabled" };
            }, ct);

            return result?.Value?.Select(u => new ExternalAccount(
                ExternalId: u.Id!,
                Email: u.Mail ?? "",
                DisplayName: u.DisplayName ?? "",
                IsActive: u.AccountEnabled ?? false
            )).ToList() ?? [];
        }

        public Task<string> ProvisionAsync(CanonicalIdentity identity, CancellationToken ct = default)
            => throw new NotImplementedException(); // próximo passo

        public Task DeprovisionAsync(string externalId, CancellationToken ct = default)
            => throw new NotImplementedException(); // próximo passo
    }

    public record AzureAdConnectorSettings(string TenantId, string ClientId, string ClientSecret);
}
