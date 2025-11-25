using System;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AzureSmartCost.Functions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        
        // Cosmos DB Client
        var cosmosConnectionString = configuration["CosmosDb:ConnectionString"] 
            ?? Environment.GetEnvironmentVariable("CosmosDb");
        
        if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            services.AddSingleton<CosmosClient>(sp =>
            {
                return new CosmosClient(cosmosConnectionString);
            });

            // Analytics Service
            var databaseName = configuration["CosmosDb:DatabaseName"] ?? "SmartCostDB";
            services.AddSingleton<AnalyticsService>(sp =>
            {
                var cosmosClient = sp.GetRequiredService<CosmosClient>();
                return new AnalyticsService(cosmosClient, databaseName);
            });
        }

        // Configurar logging básico
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    })
    .Build();

host.Run();