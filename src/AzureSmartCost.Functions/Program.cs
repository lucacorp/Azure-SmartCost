using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using AzureSmartCost.Shared.Services;
using AzureSmartCost.Shared.Models;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        
        // Configurar Cosmos DB Settings
        services.Configure<CosmosDbSettings>(
            configuration.GetSection("CosmosDb"));

        // Registrar Cosmos DB Client
        services.AddSingleton<CosmosClient>(sp =>
        {
            var connectionString = configuration.GetConnectionString("CosmosDb") 
                ?? throw new InvalidOperationException("CosmosDb connection string not found");
            
            return new CosmosClient(connectionString, new CosmosClientOptions
            {
                ApplicationName = "AzureSmartCost",
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(10)
            });
        });

        // Registrar serviços
        services.AddSingleton<CosmosDbService>();
        services.AddSingleton<IAlertService, AlertService>();
        
        // Escolher entre serviço real ou mock baseado na configuração
        var useRealCostApi = configuration.GetValue<bool>("USE_REAL_COST_API");
        if (useRealCostApi)
        {
            services.AddSingleton<ICostManagementService, CostManagementServiceReal>();
        }
        else
        {
            services.AddSingleton<ICostManagementService, CostManagementService>();
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