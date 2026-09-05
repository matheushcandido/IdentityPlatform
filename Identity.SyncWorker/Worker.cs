using Identity.Connectors;

namespace Identity.SyncWorker;

public class Worker(AzureAdGraphConnector connector, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var accounts = await connector.FullSyncAsync(stoppingToken);
                logger.LogInformation("Sync trouxe {Count} contas do Azure AD", accounts.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro durante sync com Azure AD");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
