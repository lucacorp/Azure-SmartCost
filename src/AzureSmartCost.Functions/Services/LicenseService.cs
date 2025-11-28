using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using AzureSmartCost.Functions.Models;

namespace AzureSmartCost.Functions.Services;

public class LicenseService
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly string _containerName;
    private Container? _container;

    public LicenseService(IConfiguration configuration)
    {
        var endpoint = configuration["CosmosDb__Endpoint"] 
            ?? throw new InvalidOperationException("CosmosDb__Endpoint not configured");
        
        _cosmosClient = new CosmosClient(endpoint, new DefaultAzureCredential());
        _databaseName = configuration["CosmosDb__DatabaseName"] ?? "SmartCostDB";
        _containerName = "Licenses";
    }

    private async Task<Container> GetContainerAsync()
    {
        if (_container == null)
        {
            var database = _cosmosClient.GetDatabase(_databaseName);
            
            // Create container if it doesn't exist
            var containerProperties = new ContainerProperties
            {
                Id = _containerName,
                PartitionKeyPath = "/SubscriptionId"
            };
            
            var containerResponse = await database.CreateContainerIfNotExistsAsync(
                containerProperties,
                throughput: 400
            );
            
            _container = containerResponse.Container;
        }
        
        return _container;
    }

    public async Task<(bool IsValid, License? License, string Message)> ValidateLicenseAsync(string subscriptionId)
    {
        try
        {
            var container = await GetContainerAsync();
            
            var response = await container.ReadItemAsync<License>(
                subscriptionId,
                new PartitionKey(subscriptionId)
            );
            
            var license = response.Resource;
            
            // Check if trial
            if (license.Status == LicenseStatus.Trial)
            {
                var trialEnd = license.CreatedAt.AddDays(license.TrialDays);
                if (DateTime.UtcNow > trialEnd)
                {
                    license.Status = LicenseStatus.Expired;
                    await container.UpsertItemAsync(license, new PartitionKey(subscriptionId));
                    return (false, license, $"Trial period expired on {trialEnd:yyyy-MM-dd}. Please activate your license.");
                }
                
                var daysLeft = (trialEnd - DateTime.UtcNow).Days;
                return (true, license, $"Trial active. {daysLeft} days remaining.");
            }
            
            // Check if active
            if (license.Status == LicenseStatus.Active)
            {
                if (license.ExpiresAt.HasValue && DateTime.UtcNow > license.ExpiresAt.Value)
                {
                    license.Status = LicenseStatus.Expired;
                    await container.UpsertItemAsync(license, new PartitionKey(subscriptionId));
                    return (false, license, $"License expired on {license.ExpiresAt.Value:yyyy-MM-dd}. Please renew.");
                }
                
                return (true, license, "License active");
            }
            
            return (false, license, $"License status: {license.Status}");
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Auto-create trial license for new subscription
            var newLicense = new License
            {
                id = subscriptionId,
                SubscriptionId = subscriptionId,
                Status = LicenseStatus.Trial,
                CreatedAt = DateTime.UtcNow,
                TrialDays = 14,
                MonthlyFee = 40.00m
            };
            
            var container = await GetContainerAsync();
            await container.CreateItemAsync(newLicense, new PartitionKey(subscriptionId));
            
            return (true, newLicense, $"Trial license created. 14 days remaining.");
        }
        catch (Exception ex)
        {
            return (false, null, $"License validation error: {ex.Message}");
        }
    }

    public async Task<License> CreateLicenseAsync(string subscriptionId, string customerEmail, string customerName)
    {
        var license = new License
        {
            id = subscriptionId,
            SubscriptionId = subscriptionId,
            CustomerEmail = customerEmail,
            CustomerName = customerName,
            Status = LicenseStatus.Trial,
            CreatedAt = DateTime.UtcNow,
            TrialDays = 14,
            MonthlyFee = 40.00m
        };
        
        var container = await GetContainerAsync();
        await container.CreateItemAsync(license, new PartitionKey(subscriptionId));
        
        return license;
    }

    public async Task<License> ActivateLicenseAsync(string subscriptionId, int durationMonths = 1)
    {
        var container = await GetContainerAsync();
        
        var response = await container.ReadItemAsync<License>(
            subscriptionId,
            new PartitionKey(subscriptionId)
        );
        
        var license = response.Resource;
        license.Status = LicenseStatus.Active;
        license.ActivatedAt = DateTime.UtcNow;
        license.ExpiresAt = DateTime.UtcNow.AddMonths(durationMonths);
        
        await container.UpsertItemAsync(license, new PartitionKey(subscriptionId));
        
        return license;
    }

    public async Task<License> SuspendLicenseAsync(string subscriptionId)
    {
        var container = await GetContainerAsync();
        
        var response = await container.ReadItemAsync<License>(
            subscriptionId,
            new PartitionKey(subscriptionId)
        );
        
        var license = response.Resource;
        license.Status = LicenseStatus.Suspended;
        
        await container.UpsertItemAsync(license, new PartitionKey(subscriptionId));
        
        return license;
    }

    public async Task<IEnumerable<License>> GetAllLicensesAsync()
    {
        var container = await GetContainerAsync();
        
        var query = new QueryDefinition("SELECT * FROM c ORDER BY c.CreatedAt DESC");
        var iterator = container.GetItemQueryIterator<License>(query);
        
        var licenses = new List<License>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            licenses.AddRange(response);
        }
        
        return licenses;
    }
}
