using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AzureSmartCost.Functions;
using AzureSmartCost.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        
        // Analytics Service - usa Azure Cost Management API diretamente
        services.AddSingleton<AnalyticsService>();
        
        // License Service - gerenciamento de licenças
        services.AddSingleton<LicenseService>();

        // Configurar logging básico
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    })
    .Build();

host.Run();