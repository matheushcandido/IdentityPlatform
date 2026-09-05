using Identity.Connectors;
using Identity.SyncWorker;

var builder = Host.CreateApplicationBuilder(args);

var settings = new AzureAdConnectorSettings(
    TenantId: builder.Configuration["AzureAd:TenantId"]!,
    ClientId: builder.Configuration["AzureAd:ClientId"]!,
    ClientSecret: builder.Configuration["AzureAd:ClientSecret"]!
);

builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<AzureAdGraphConnector>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();