using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Azure.ResourceManager;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Services
{
    public class CostManagementServiceReal : ICostManagementService
    {
        private readonly ILogger<CostManagementServiceReal> _logger;
        private readonly IConfiguration _configuration;
        private readonly ArmClient _armClient;
        private readonly string _subscriptionId;

        public CostManagementServiceReal(ILogger<CostManagementServiceReal> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            _subscriptionId = _configuration["AZURE_SUBSCRIPTION_ID"] 
                ?? "seu-subscription-id-aqui";

            // Use Managed Identity for authentication in Azure, DefaultAzureCredential for local dev
            var credential = new DefaultAzureCredential();
            _armClient = new ArmClient(credential);
        }

        public async Task<ApiResponse<List<CostRecord>>> GetDailyCostAsync()
        {
            var operationId = Guid.NewGuid().ToString();
            var executionStart = DateTime.UtcNow;

            _logger.LogInformation("Starting Azure Cost Management API query (Enhanced Mock with Real Auth). OperationId: {OperationId}, SubscriptionId: {SubscriptionId}", 
                operationId, _subscriptionId);

            try
            {
                // Test Azure authentication if subscription ID is configured
                if (_subscriptionId != "seu-subscription-id-aqui")
                {
                    try
                    {
                        var subscriptionResource = await _armClient.GetDefaultSubscriptionAsync();
                        
                        _logger.LogInformation("✅ Successfully authenticated with Azure ARM. SubscriptionId: {SubscriptionId}, OperationId: {OperationId}", 
                            subscriptionResource.Data.SubscriptionId, operationId);

                        _logger.LogInformation("🚧 Azure authentication successful but Cost Management API integration is in development. Using enhanced mock data. OperationId: {OperationId}", operationId);
                    }
                    catch (Azure.RequestFailedException ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Azure authentication failed, using mock data. StatusCode: {StatusCode}, OperationId: {OperationId}", 
                            ex.Status, operationId);
                    }
                }
                else
                {
                    _logger.LogInformation("🔧 Using placeholder subscription ID, enhanced mock data mode. OperationId: {OperationId}", operationId);
                }

                // Generate enhanced mock data with realistic patterns
                var enhancedMockData = await GenerateEnhancedMockDataAsync(_subscriptionId, operationId);
                
                var executionTime = DateTime.UtcNow - executionStart;
                _logger.LogInformation("✅ Successfully retrieved {RecordCount} enhanced mock cost records. Duration: {Duration:c}, OperationId: {OperationId}", 
                    enhancedMockData.Count, executionTime, operationId);

                return ApiResponse<List<CostRecord>>.CreateSuccess(enhancedMockData, 
                    $"Enhanced mock data with Azure authentication test: {enhancedMockData.Count} records");
            }
            catch (ArgumentException ex)
            {
                var executionTime = DateTime.UtcNow - executionStart;
                _logger.LogError(ex, "Invalid argument in cost data collection. Duration: {Duration:c}, OperationId: {OperationId}", 
                    executionTime, operationId);
                return ApiResponse<List<CostRecord>>.CreateError("Invalid parameters provided", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                var executionTime = DateTime.UtcNow - executionStart;
                _logger.LogError(ex, "Configuration or operation error during cost data collection. Duration: {Duration:c}, OperationId: {OperationId}", 
                    executionTime, operationId);
                return ApiResponse<List<CostRecord>>.CreateError($"Configuration error: {ex.Message}", "Check application configuration");
            }
            catch (Exception ex)
            {
                var executionTime = DateTime.UtcNow - executionStart;
                _logger.LogError(ex, "Unexpected error during cost data collection. Duration: {Duration:c}, OperationId: {OperationId}", 
                    executionTime, operationId);
                return ApiResponse<List<CostRecord>>.CreateError($"Unexpected error occurred: {ex.Message}", "An unexpected error occurred during cost data retrieval");
            }
        }

        private async Task<List<CostRecord>> GenerateEnhancedMockDataAsync(string subscriptionId, string operationId)
        {
            var results = new List<CostRecord>();
            
            try
            {
                _logger.LogDebug("🎭 Generating enhanced mock cost data with realistic patterns. OperationId: {OperationId}", operationId);

                await Task.Delay(500); // Simulate API call

                var random = new Random(DateTime.Now.Millisecond); // Seed for variation
                
                var services = new[] { 
                    "Virtual Machines", "Storage Accounts", "SQL Database", "App Service", 
                    "Azure Functions", "Cosmos DB", "Key Vault", "Application Insights",
                    "Load Balancer", "Virtual Network", "Azure Monitor", "Logic Apps"
                };
                
                var resourceGroups = new[] { 
                    "rg-production", "rg-development", "rg-testing", 
                    "rg-shared-services", "rg-data-platform", "rg-monitoring"
                };

                // Generate data for the last 14 days with realistic patterns
                for (int i = 0; i < 14; i++)
                {
                    var date = DateTime.UtcNow.Date.AddDays(-i);
                    var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                    var weekendFactor = isWeekend ? 0.3m : 1.0m; // Reduced costs on weekends
                    
                    foreach (var service in services)
                    {
                        var resourceGroup = resourceGroups[random.Next(resourceGroups.Length)];
                        
                        // Different cost patterns per service
                        var baseCost = service switch
                        {
                            "Virtual Machines" => (decimal)(random.NextDouble() * 800 + 200) * weekendFactor,
                            "SQL Database" => (decimal)(random.NextDouble() * 400 + 100) * weekendFactor,
                            "Storage Accounts" => (decimal)(random.NextDouble() * 50 + 5),
                            "Cosmos DB" => (decimal)(random.NextDouble() * 300 + 50),
                            "App Service" => (decimal)(random.NextDouble() * 200 + 30) * weekendFactor,
                            "Azure Functions" => (decimal)(random.NextDouble() * 20 + 2),
                            _ => (decimal)(random.NextDouble() * 100 + 10)
                        };

                        // Add some day-to-day variation
                        var variation = 1.0m + (decimal)(random.NextDouble() * 0.4 - 0.2); // ±20% variation
                        var finalCost = baseCost * variation;

                        var record = new CostRecord
                        {
                            Id = Guid.NewGuid().ToString(),
                            Date = date,
                            ServiceName = service,
                            ResourceGroup = resourceGroup,
                            TotalCost = Math.Round(finalCost, 2),
                            Currency = "USD",
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        results.Add(record);
                    }
                }

                // Add some high-cost anomalies for testing alerts (future feature)
                if (random.Next(1, 4) == 1) // 25% chance
                {
                    var anomalyRecord = new CostRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        Date = DateTime.UtcNow.Date.AddDays(-random.Next(1, 7)),
                        ServiceName = "Virtual Machines",
                        ResourceGroup = "rg-production",
                        TotalCost = (decimal)(random.NextDouble() * 2000 + 1500), // High cost anomaly
                        Currency = "USD",
                        CreatedAt = DateTime.UtcNow
                    };
                    results.Add(anomalyRecord);
                }

                _logger.LogDebug("🎭 Generated {RecordCount} enhanced mock cost records for {DayCount} days with realistic patterns. OperationId: {OperationId}", 
                    results.Count, 14, operationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating enhanced mock data. OperationId: {OperationId}", operationId);
                throw;
            }

            return results;
        }

        public async Task<decimal> GetCurrentCostAsync()
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("🔍 Getting current cost. OperationId: {OperationId}", operationId);

            try
            {
                var costData = await GetDailyCostAsync();
                if (costData.Success && costData.Data != null && costData.Data.Any())
                {
                    var todayCost = costData.Data
                        .Where(c => c.Date.Date == DateTime.UtcNow.Date)
                        .Sum(c => c.TotalCost);

                    if (todayCost == 0) // If no data for today, get latest day's total
                    {
                        todayCost = costData.Data
                            .GroupBy(c => c.Date.Date)
                            .OrderByDescending(g => g.Key)
                            .FirstOrDefault()?.Sum(c => c.TotalCost) ?? 0;
                    }

                    _logger.LogInformation("✅ Current cost: ${Cost:F2}. OperationId: {OperationId}", todayCost, operationId);
                    return todayCost;
                }

                _logger.LogWarning("⚠️ No cost data available for current calculation. OperationId: {OperationId}", operationId);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting current cost. OperationId: {OperationId}", operationId);
                return 0;
            }
        }

        public async Task<List<CostRecord>> GetCostHistoryAsync(DateTime startDate, DateTime endDate)
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("📊 Getting cost history from {StartDate} to {EndDate}. OperationId: {OperationId}", 
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), operationId);

            try
            {
                var costData = await GetDailyCostAsync();
                if (costData.Success && costData.Data != null && costData.Data.Any())
                {
                    var filteredData = costData.Data
                        .Where(c => c.Date.Date >= startDate.Date && c.Date.Date <= endDate.Date)
                        .OrderByDescending(c => c.Date)
                        .ToList();

                    _logger.LogInformation("✅ Retrieved {RecordCount} cost records for date range. OperationId: {OperationId}", 
                        filteredData.Count, operationId);
                    return filteredData;
                }

                _logger.LogWarning("⚠️ No cost data available for history query. OperationId: {OperationId}", operationId);
                return new List<CostRecord>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting cost history. OperationId: {OperationId}", operationId);
                return new List<CostRecord>();
            }
        }
    }
}