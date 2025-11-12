using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CostManagement;
using Azure.ResourceManager.CostManagement.Models;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Services
{
    public class CostManagementService : ICostManagementService
    {
        private readonly ILogger<CostManagementService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ArmClient _armClient;
        private readonly string _subscriptionId;

        public CostManagementService(ILogger<CostManagementService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            _subscriptionId = _configuration["AZURE_SUBSCRIPTION_ID"] 
                ?? throw new InvalidOperationException("AZURE_SUBSCRIPTION_ID configuration not found");

            // Use Managed Identity for authentication in Azure, DefaultAzureCredential for local dev
            var credential = new DefaultAzureCredential();
            _armClient = new ArmClient(credential);
        }

        public async Task<ApiResponse<List<CostRecord>>> GetDailyCostAsync()
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Starting cost data collection. OperationId: {OperationId}", operationId);

            try
            {
                var subscriptionId = _configuration["AZURE_SUBSCRIPTION_ID"];
                
                if (string.IsNullOrWhiteSpace(subscriptionId))
                {
                    var errorMessage = "Azure subscription ID is not configured";
                    _logger.LogError("Configuration error: {ErrorMessage}. OperationId: {OperationId}", 
                        errorMessage, operationId);
                    
                    return ApiResponse<List<CostRecord>>.CreateError(
                        errorMessage, 
                        "Please configure AZURE_SUBSCRIPTION_ID in application settings");
                }

                if (subscriptionId == "seu-subscription-id-aqui")
                {
                    var errorMessage = "Azure subscription ID contains placeholder value";
                    _logger.LogWarning("Configuration warning: {ErrorMessage}. OperationId: {OperationId}", 
                        errorMessage, operationId);
                    
                    return ApiResponse<List<CostRecord>>.CreateError(
                        errorMessage, 
                        "Please update AZURE_SUBSCRIPTION_ID with your actual subscription ID");
                }
                
                _logger.LogInformation("Fetching cost data for subscription: {SubscriptionId}. OperationId: {OperationId}", 
                    subscriptionId, operationId);

                var results = await GenerateMockDataAsync(subscriptionId, operationId);

                _logger.LogInformation("Successfully generated {RecordCount} cost records. OperationId: {OperationId}", 
                    results.Count, operationId);

                return ApiResponse<List<CostRecord>>.CreateSuccess(
                    results, 
                    $"Successfully retrieved {results.Count} cost records");
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument in cost data collection. OperationId: {OperationId}", operationId);
                return ApiResponse<List<CostRecord>>.CreateError(ex, "Invalid configuration or parameters provided");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during cost data collection. OperationId: {OperationId}", operationId);
                return ApiResponse<List<CostRecord>>.CreateError(ex, "Operation cannot be performed with current configuration");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout during cost data collection. OperationId: {OperationId}", operationId);
                return ApiResponse<List<CostRecord>>.CreateError(ex, "Request timeout while fetching cost data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during cost data collection. OperationId: {OperationId}", operationId);
                return ApiResponse<List<CostRecord>>.CreateError(ex, "An unexpected error occurred while fetching cost data");
            }
        }

        private async Task<List<CostRecord>> GenerateMockDataAsync(string subscriptionId, string operationId)
        {
            var results = new List<CostRecord>();
            
            try
            {
                _logger.LogDebug("Generating mock cost data. OperationId: {OperationId}", operationId);

                // Por enquanto, vamos simular dados para testar a estrutura
                // TODO: Implementar integração real com Azure Cost Management API
                var random = new Random();
                var baseDate = DateTime.UtcNow.AddDays(-7);
                var services = new[] { "Azure Functions", "Azure Storage", "Azure Cosmos DB", "Azure Monitor", "Azure Key Vault" };
                var resourceGroups = new[] { "rg-smartcost-prod", "rg-smartcost-dev", "rg-smartcost-test" };

                for (int i = 0; i < 7; i++)
                {
                    var currentDate = baseDate.AddDays(i);
                    
                    // Generate multiple records per day for different services
                    foreach (var service in services)
                    {
                        var costRecord = new CostRecord 
                        { 
                            SubscriptionId = subscriptionId,
                            Date = currentDate.Date, 
                            TotalCost = Math.Round((decimal)(20 + random.NextDouble() * 80), 2),
                            Currency = "USD",
                            ResourceGroup = resourceGroups[random.Next(resourceGroups.Length)],
                            ServiceName = service
                        };
                        
                        costRecord.SetPartitionKey();
                        results.Add(costRecord);
                        
                        _logger.LogTrace("Generated cost record: {Date} | {Service} | ${Cost:F2}. OperationId: {OperationId}", 
                            costRecord.Date.ToString("yyyy-MM-dd"), 
                            costRecord.ServiceName, 
                            costRecord.TotalCost, 
                            operationId);
                    }
                }

                // Simular delay da API real
                await Task.Delay(TimeSpan.FromMilliseconds(100 + random.Next(200)));

                _logger.LogDebug("Mock data generation completed. Records: {RecordCount}. OperationId: {OperationId}", 
                    results.Count, operationId);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating mock data. OperationId: {OperationId}", operationId);
                throw;
            }
        }

        public async Task<ApiResponse<decimal>> GetTotalCostAsync(DateTime startDate, DateTime endDate)
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Getting total cost for date range {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}. OperationId: {OperationId}", 
                startDate, endDate, operationId);

            try
            {
                if (startDate > endDate)
                {
                    var errorMessage = "Start date cannot be after end date";
                    _logger.LogWarning("Validation error: {ErrorMessage}. StartDate: {StartDate}, EndDate: {EndDate}. OperationId: {OperationId}", 
                        errorMessage, startDate, endDate, operationId);
                    
                    return ApiResponse<decimal>.CreateError(errorMessage, 
                        "Please ensure the start date is before or equal to the end date");
                }

                var costDataResponse = await GetDailyCostAsync();
                
                if (!costDataResponse.Success)
                {
                    _logger.LogError("Failed to get cost data for total calculation. OperationId: {OperationId}", operationId);
                    return ApiResponse<decimal>.CreateError("Failed to retrieve cost data for calculation", 
                        costDataResponse.Errors.ToArray());
                }

                var totalCost = 0m;
                if (costDataResponse.Data != null)
                {
                    foreach (var record in costDataResponse.Data)
                    {
                        if (record.Date >= startDate.Date && record.Date <= endDate.Date)
                        {
                            totalCost += record.TotalCost;
                        }
                    }
                }

                _logger.LogInformation("Total cost calculated: ${TotalCost:F2} for period {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}. OperationId: {OperationId}", 
                    totalCost, startDate, endDate, operationId);

                return ApiResponse<decimal>.CreateSuccess(totalCost, 
                    $"Total cost for period {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}: ${totalCost:F2}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total cost. OperationId: {OperationId}", operationId);
                return ApiResponse<decimal>.CreateError(ex, "Error calculating total cost for the specified period");
            }
        }

        public async Task<decimal> GetCurrentCostAsync()
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("🔍 Getting current cost (mock). OperationId: {OperationId}", operationId);

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

                    _logger.LogInformation("✅ Current cost (mock): ${Cost:F2}. OperationId: {OperationId}", todayCost, operationId);
                    return todayCost;
                }

                _logger.LogWarning("⚠️ No cost data available for current calculation (mock). OperationId: {OperationId}", operationId);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting current cost (mock). OperationId: {OperationId}", operationId);
                return 0;
            }
        }

        public async Task<List<CostRecord>> GetCostHistoryAsync(DateTime startDate, DateTime endDate)
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("📊 Getting cost history (mock) from {StartDate} to {EndDate}. OperationId: {OperationId}", 
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

                    _logger.LogInformation("✅ Retrieved {RecordCount} cost records for date range (mock). OperationId: {OperationId}", 
                        filteredData.Count, operationId);
                    return filteredData;
                }

                _logger.LogWarning("⚠️ No cost data available for history query (mock). OperationId: {OperationId}", operationId);
                return new List<CostRecord>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting cost history (mock). OperationId: {OperationId}", operationId);
                return new List<CostRecord>();
            }
        }
    }
}
