using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AzureSmartCost.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureSmartCost.Shared.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly ILogger<CosmosDbService> _logger;
        private readonly CosmosDbSettings _settings;
        private Database? _database;
        private Container? _costRecordsContainer;
        private Container? _eventsContainer;

        public CosmosDbService(CosmosClient cosmosClient, ILogger<CosmosDbService> logger, IOptions<CosmosDbSettings> settings)
        {
            _cosmosClient = cosmosClient;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Cosmos DB...");
                
                // Criar database se não existir
                _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_settings.DatabaseName);
                _logger.LogInformation($"Database '{_settings.DatabaseName}' ready");

                // Criar container para cost records
                _costRecordsContainer = await _database.CreateContainerIfNotExistsAsync(
                    _settings.CostRecordsContainer,
                    "/partitionKey",
                    throughput: 400);
                _logger.LogInformation($"Container '{_settings.CostRecordsContainer}' ready");

                // Criar container para events
                _eventsContainer = await _database.CreateContainerIfNotExistsAsync(
                    _settings.EventsContainer,
                    "/partitionKey",
                    throughput: 400);
                _logger.LogInformation($"Container '{_settings.EventsContainer}' ready");

                _logger.LogInformation("Cosmos DB initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Cosmos DB");
                throw;
            }
        }

        public async Task<CostRecord> SaveCostRecordAsync(CostRecord record)
        {
            try
            {
                if (_costRecordsContainer == null)
                    throw new InvalidOperationException("Cosmos DB not initialized. Call InitializeAsync first.");

                record.SetPartitionKey();
                record.CreatedAt = DateTime.UtcNow;

                var response = await _costRecordsContainer.CreateItemAsync(record, new PartitionKey(record.PartitionKey));
                
                _logger.LogDebug($"Saved cost record {record.Id} with RU charge: {response.RequestCharge}");
                
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save cost record {record.Id}");
                throw;
            }
        }

        public async Task<List<CostRecord>> SaveCostRecordsAsync(List<CostRecord> records)
        {
            var results = new List<CostRecord>();
            
            try
            {
                if (_costRecordsContainer == null)
                    throw new InvalidOperationException("Cosmos DB not initialized. Call InitializeAsync first.");

                _logger.LogInformation($"Saving {records.Count} cost records to Cosmos DB");

                foreach (var record in records)
                {
                    var savedRecord = await SaveCostRecordAsync(record);
                    results.Add(savedRecord);
                }

                _logger.LogInformation($"Successfully saved {results.Count} cost records");
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save {records.Count} cost records");
                throw;
            }
        }

        public async Task<List<CostRecord>> GetCostRecordsByDateRangeAsync(DateTime startDate, DateTime endDate, string? subscriptionId = null)
        {
            try
            {
                if (_costRecordsContainer == null)
                    throw new InvalidOperationException("Cosmos DB not initialized. Call InitializeAsync first.");

                var queryBuilder = "SELECT * FROM c WHERE c.date >= @startDate AND c.date <= @endDate";
                
                var queryDefinition = new QueryDefinition(queryBuilder)
                    .WithParameter("@startDate", startDate)
                    .WithParameter("@endDate", endDate);

                if (!string.IsNullOrEmpty(subscriptionId))
                {
                    queryBuilder += " AND c.subscriptionId = @subscriptionId";
                    queryDefinition = queryDefinition.WithParameter("@subscriptionId", subscriptionId);
                }

                queryBuilder += " ORDER BY c.date DESC";
                queryDefinition = new QueryDefinition(queryBuilder);

                var results = new List<CostRecord>();
                using var iterator = _costRecordsContainer.GetItemQueryIterator<CostRecord>(queryDefinition);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.ToList());
                    
                    _logger.LogDebug($"Query batch returned {response.Count} records with RU charge: {response.RequestCharge}");
                }

                _logger.LogInformation($"Retrieved {results.Count} cost records for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to retrieve cost records for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                throw;
            }
        }

        public async Task<List<CostRecord>> GetLatestCostRecordsAsync(int count = 10)
        {
            try
            {
                if (_costRecordsContainer == null)
                    throw new InvalidOperationException("Cosmos DB not initialized. Call InitializeAsync first.");

                var queryDefinition = new QueryDefinition($"SELECT TOP {count} * FROM c ORDER BY c.createdAt DESC");
                
                var results = new List<CostRecord>();
                using var iterator = _costRecordsContainer.GetItemQueryIterator<CostRecord>(queryDefinition);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                _logger.LogInformation($"Retrieved {results.Count} latest cost records");
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to retrieve latest {count} cost records");
                throw;
            }
        }

        public async Task<decimal> GetTotalCostAsync(DateTime startDate, DateTime endDate, string? subscriptionId = null)
        {
            try
            {
                var records = await GetCostRecordsByDateRangeAsync(startDate, endDate, subscriptionId);
                var totalCost = records.Sum(r => r.TotalCost);

                _logger.LogInformation($"Total cost for period {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}: ${totalCost:F2}");
                
                return totalCost;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to calculate total cost for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                throw;
            }
        }
    }
}