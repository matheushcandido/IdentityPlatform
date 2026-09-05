using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Connectors.Interfaces
{
    public interface IConnectorAdapter
    {
        Task<IReadOnlyList<ExternalAccount>> FullSyncAsync(CancellationToken ct = default);
        Task<string> ProvisionAsync(CanonicalIdentity identity, CancellationToken ct = default);
        Task DeprovisionAsync(string externalId, CancellationToken ct = default);
    }

    public record ExternalAccount(
        string ExternalId,
        string Email,
        string DisplayName,
        bool IsActive
    );

    public record CanonicalIdentity(
        string Email,
        string DisplayName
    );
}
