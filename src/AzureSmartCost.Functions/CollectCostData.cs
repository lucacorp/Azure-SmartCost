using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AzureSmartCost.Shared.Services;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Functions
{
    public class CollectCostData
    {
        private readonly ILogger<CollectCostData> _logger;
        private readonly CosmosDbService _cosmosDbService;
        private readonly ICostManagementService _costService;
        private readonly IAlertService _alertService;

        public CollectCostData(ILogger<CollectCostData> logger, CosmosDbService cosmosDbService, 
            ICostManagementService costService, IAlertService alertService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cosmosDbService = cosmosDbService ?? throw new ArgumentNullException(nameof(cosmosDbService));
            _costService = costService ?? throw new ArgumentNullException(nameof(costService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        }

        [Function("CollectCostData")]
        public async Task RunAsync([TimerTrigger("0 0 * * * *")] TimerInfo timer)
        {
            var operationId = Guid.NewGuid().ToString();
            var executionStart = DateTime.UtcNow;
            
            _logger.LogInformation("Azure SmartCost - Starting cost data collection. OperationId: {OperationId}, StartTime: {StartTime}", 
                operationId, executionStart);

            try
            {
                // Garantir que o Cosmos DB está inicializado
                _logger.LogInformation("Initializing Cosmos DB connection. OperationId: {OperationId}", operationId);
                await _cosmosDbService.InitializeAsync();
                _logger.LogInformation("Cosmos DB initialization completed. OperationId: {OperationId}", operationId);

                // Coletar dados de custo
                _logger.LogInformation("Starting cost data collection from Azure Cost Management API. OperationId: {OperationId}", operationId);
                var costDataResponse = await _costService.GetDailyCostAsync();

                if (!costDataResponse.Success)
                {
                    _logger.LogError("Failed to collect cost data. Errors: {Errors}. OperationId: {OperationId}", 
                        string.Join(", ", costDataResponse.Errors), operationId);
                    
                    throw new InvalidOperationException($"Cost data collection failed: {costDataResponse.Message}");
                }

                var records = costDataResponse.Data ?? new List<CostRecord>();
                _logger.LogInformation("Collected {RecordCount} cost records from Azure Cost Management API. OperationId: {OperationId}", 
                    records.Count, operationId);

                if (records.Count > 0)
                {
                    // Salvar no Cosmos DB
                    _logger.LogInformation("Saving {RecordCount} records to Cosmos DB. OperationId: {OperationId}", 
                        records.Count, operationId);
                    
                    var savedRecords = await _cosmosDbService.SaveCostRecordsAsync(records);
                    
                    _logger.LogInformation("Successfully saved {SavedCount} cost records to Cosmos DB. OperationId: {OperationId}", 
                        savedRecords.Count, operationId);

                    // Log estatísticas resumidas
                    var totalCost = savedRecords.Sum(r => r.TotalCost);
                    var avgCost = savedRecords.Average(r => r.TotalCost);
                    var dateRange = $"{savedRecords.Min(r => r.Date):yyyy-MM-dd} to {savedRecords.Max(r => r.Date):yyyy-MM-dd}";
                    var uniqueServices = savedRecords.Select(r => r.ServiceName).Distinct().Count();
                    var uniqueResourceGroups = savedRecords.Select(r => r.ResourceGroup).Distinct().Count();
                    
                    _logger.LogInformation("Cost Summary - Total: ${TotalCost:F2}, Average: ${AvgCost:F2}, DateRange: {DateRange}, Services: {ServiceCount}, ResourceGroups: {RgCount}. OperationId: {OperationId}", 
                        totalCost, avgCost, dateRange, uniqueServices, uniqueResourceGroups, operationId);

                    // *** NOVO: AVALIAÇÃO DE ALERTAS ***
                    _logger.LogInformation("🚨 Starting alert evaluation for saved cost records. OperationId: {OperationId}", operationId);
                    
                    var triggeredAlerts = await _alertService.EvaluateAlertsAsync(savedRecords);
                    
                    if (triggeredAlerts.Any())
                    {
                        _logger.LogWarning("🚨 {AlertCount} cost alerts triggered! Processing notifications... OperationId: {OperationId}", 
                            triggeredAlerts.Count, operationId);

                        foreach (var alert in triggeredAlerts)
                        {
                            await _alertService.SendAlertAsync(alert);
                        }

                        _logger.LogWarning("✅ All {AlertCount} alert notifications processed. OperationId: {OperationId}", 
                            triggeredAlerts.Count, operationId);
                    }
                    else
                    {
                        _logger.LogInformation("✅ No cost alerts triggered. All thresholds within acceptable limits. OperationId: {OperationId}", operationId);
                    }

                    // Log por serviço para análise detalhada
                    var serviceGroups = savedRecords.GroupBy(r => r.ServiceName);
                    foreach (var group in serviceGroups)
                    {
                        var serviceCost = group.Sum(r => r.TotalCost);
                        _logger.LogDebug("Service cost breakdown - {ServiceName}: ${ServiceCost:F2} ({RecordCount} records). OperationId: {OperationId}", 
                            group.Key, serviceCost, group.Count(), operationId);
                    }
                }
                else
                {
                    _logger.LogWarning("No cost records were collected from Azure Cost Management API. OperationId: {OperationId}", operationId);
                }

                var executionTime = DateTime.UtcNow - executionStart;
                _logger.LogInformation("Azure SmartCost - Cost data collection completed successfully. Duration: {Duration:c}, OperationId: {OperationId}", 
                    executionTime, operationId);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuration or operation error during cost data collection. OperationId: {OperationId}", operationId);
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during cost data collection. OperationId: {OperationId}", operationId);
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout during cost data collection. OperationId: {OperationId}", operationId);
                throw;
            }
            catch (Exception ex)
            {
                var executionTime = DateTime.UtcNow - executionStart;
                _logger.LogError(ex, "Unexpected error during cost data collection. Duration: {Duration:c}, OperationId: {OperationId}", 
                    executionTime, operationId);
                throw;
            }
        }
    }
}
