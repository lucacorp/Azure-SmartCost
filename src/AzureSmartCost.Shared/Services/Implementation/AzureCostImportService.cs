using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CostManagement;
using Azure.ResourceManager.CostManagement.Models;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AzureSmartCost.Shared.Services.Implementation;

public class AzureCostImportService : IAzureCostImportService
{
    private readonly ILogger<AzureCostImportService> _logger;
    private readonly ICostDataRepository _costDataRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;

    public AzureCostImportService(
        ILogger<AzureCostImportService> logger,
        ICostDataRepository costDataRepository,
        ISubscriptionRepository subscriptionRepository)
    {
        _logger = logger;
        _costDataRepository = costDataRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<CostImportResult> ImportCostsForSubscriptionAsync(
        string tenantId,
        string subscriptionId,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new CostImportResult
        {
            SubscriptionId = subscriptionId,
            StartDate = startDate,
            EndDate = endDate,
            ImportedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation(
                "Iniciando importação de custos - Subscription: {SubscriptionId}, Período: {Start} a {End}",
                subscriptionId, startDate, endDate);

            // Get subscription from database to verify access
            var subscription = await _subscriptionRepository.GetByAzureIdAsync(subscriptionId);
            if (subscription == null || subscription.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException($"Subscription {subscriptionId} não encontrada ou sem permissão");
            }

            // Use DefaultAzureCredential (supports Managed Identity, Azure CLI, etc.)
            var credential = new DefaultAzureCredential();
            var armClient = new ArmClient(credential);

            // Get subscription resource
            var subscriptionResource = armClient.GetSubscriptionResource(
                new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));

            // Query cost data using Cost Management API
            var queryResult = await QueryCostDataAsync(subscriptionResource, startDate, endDate);

            // Transform and save to Cosmos DB
            var costRecords = await TransformAndSaveCostDataAsync(
                tenantId,
                subscriptionId,
                queryResult);

            result.RecordsImported = costRecords;
            result.Success = true;
            result.Message = $"Importação concluída com sucesso: {costRecords} registros";

            _logger.LogInformation(
                "Importação concluída - {Records} registros importados para subscription {SubscriptionId}",
                costRecords, subscriptionId);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Erro na importação: {ex.Message}";
            result.ErrorDetails = ex.ToString();

            _logger.LogError(ex,
                "Erro ao importar custos - Subscription: {SubscriptionId}",
                subscriptionId);
        }

        return result;
    }

    public async Task<CostImportResult> ImportCostsForAllSubscriptionsAsync(
        string tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var result = new CostImportResult
        {
            StartDate = start,
            EndDate = end,
            ImportedAt = DateTime.UtcNow
        };

        try
        {
            // Get all active subscriptions for tenant
            var subscriptions = await _subscriptionRepository.GetByTenantIdAsync(tenantId);
            var activeSubscriptions = subscriptions.Where(s => s.IsActive).ToList();

            _logger.LogInformation(
                "Iniciando importação para {Count} subscriptions do tenant {TenantId}",
                activeSubscriptions.Count, tenantId);

            var totalRecords = 0;
            var successCount = 0;
            var failureCount = 0;

            foreach (var subscription in activeSubscriptions)
            {
                try
                {
                    var subResult = await ImportCostsForSubscriptionAsync(
                        tenantId,
                        subscription.AzureSubscriptionId,
                        start,
                        end);

                    if (subResult.Success)
                    {
                        totalRecords += subResult.RecordsImported;
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning(
                            "Falha na importação da subscription {SubscriptionId}: {Message}",
                            subscription.AzureSubscriptionId, subResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex,
                        "Erro ao importar subscription {SubscriptionId}",
                        subscription.AzureSubscriptionId);
                }
            }

            result.RecordsImported = totalRecords;
            result.Success = failureCount == 0;
            result.Message = $"Importação concluída: {successCount} sucesso, {failureCount} falhas, {totalRecords} registros";

            _logger.LogInformation(
                "Importação em lote concluída - {Total} registros de {Success}/{Total} subscriptions",
                totalRecords, successCount, activeSubscriptions.Count);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Erro na importação em lote: {ex.Message}";
            result.ErrorDetails = ex.ToString();

            _logger.LogError(ex, "Erro ao importar custos para tenant {TenantId}", tenantId);
        }

        return result;
    }

    private async Task<CostManagementQueryResult> QueryCostDataAsync(
        Azure.ResourceManager.Resources.SubscriptionResource subscription,
        DateTime startDate,
        DateTime endDate)
    {
        // Build query for daily cost breakdown
        var query = new CostManagementQueryDefinition(
            CostManagementExportType.ActualCost,
            CostManagementTimeframeType.Custom,
            new CostManagementQueryTimePeriod(startDate, endDate))
        {
            Dataset = new CostManagementQueryDataset(CostManagementGranularityType.Daily)
            {
                Aggregation =
                {
                    ["totalCost"] = new CostManagementQueryAggregation("PreTaxCost", "Sum")
                },
                Grouping =
                {
                    new CostManagementQueryGrouping("ResourceGroup"),
                    new CostManagementQueryGrouping("ServiceName"),
                    new CostManagementQueryGrouping("ResourceType"),
                    new CostManagementQueryGrouping("MeterCategory")
                }
            }
        };

        // Execute query
        var response = await subscription.UsageByCostManagementQueryAsync(query);
        return response.Value;
    }

    private async Task<int> TransformAndSaveCostDataAsync(
        string tenantId,
        string subscriptionId,
        CostManagementQueryResult queryResult)
    {
        var costDataList = new List<CostData>();

        if (queryResult.Rows == null || !queryResult.Rows.Any())
        {
            _logger.LogWarning("Nenhum dado de custo retornado para subscription {SubscriptionId}", subscriptionId);
            return 0;
        }

        // Get column indexes
        var columns = queryResult.Columns.ToList();
        var costIndex = columns.FindIndex(c => c.Name == "PreTaxCost" || c.Name == "Cost");
        var dateIndex = columns.FindIndex(c => c.Name == "UsageDate" || c.Name == "Date");
        var resourceGroupIndex = columns.FindIndex(c => c.Name == "ResourceGroup");
        var serviceNameIndex = columns.FindIndex(c => c.Name == "ServiceName");
        var resourceTypeIndex = columns.FindIndex(c => c.Name == "ResourceType");
        var meterCategoryIndex = columns.FindIndex(c => c.Name == "MeterCategory");

        foreach (var row in queryResult.Rows)
        {
            try
            {
                var costData = new CostData
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    SubscriptionId = subscriptionId,
                    Date = dateIndex >= 0 ? DateTime.Parse(row[dateIndex].ToString()!) : DateTime.UtcNow.Date,
                    Cost = costIndex >= 0 ? decimal.Parse(row[costIndex].ToString()!) : 0,
                    Currency = "USD", // Cost Management API retorna em USD por padrão
                    ResourceGroup = resourceGroupIndex >= 0 ? row[resourceGroupIndex]?.ToString() ?? "Unknown" : "Unknown",
                    ServiceName = serviceNameIndex >= 0 ? row[serviceNameIndex]?.ToString() ?? "Unknown" : "Unknown",
                    ResourceType = resourceTypeIndex >= 0 ? row[resourceTypeIndex]?.ToString() ?? "Unknown" : "Unknown",
                    MeterCategory = meterCategoryIndex >= 0 ? row[meterCategoryIndex]?.ToString() ?? "Unknown" : "Unknown",
                    Tags = new Dictionary<string, string>
                    {
                        ["ImportedAt"] = DateTime.UtcNow.ToString("O"),
                        ["Source"] = "AzureCostManagementAPI"
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                costDataList.Add(costData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao processar linha de custo: {Row}", JsonSerializer.Serialize(row));
            }
        }

        // Save in batches
        var batchSize = 100;
        var savedCount = 0;

        for (int i = 0; i < costDataList.Count; i += batchSize)
        {
            var batch = costDataList.Skip(i).Take(batchSize).ToList();
            
            foreach (var costData in batch)
            {
                await _costDataRepository.CreateAsync(costData);
                savedCount++;
            }

            _logger.LogDebug("Salvos {Count}/{Total} registros", savedCount, costDataList.Count);
        }

        return savedCount;
    }

    public async Task<List<string>> GetAvailableSubscriptionsAsync(string tenantId)
    {
        try
        {
            var credential = new DefaultAzureCredential();
            var armClient = new ArmClient(credential);

            var subscriptions = new List<string>();

            await foreach (var subscription in armClient.GetSubscriptions().GetAllAsync())
            {
                subscriptions.Add(subscription.Data.SubscriptionId);
            }

            _logger.LogInformation(
                "Encontradas {Count} subscriptions acessíveis para tenant {TenantId}",
                subscriptions.Count, tenantId);

            return subscriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar subscriptions para tenant {TenantId}", tenantId);
            throw;
        }
    }
}

public class CostImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SubscriptionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RecordsImported { get; set; }
    public DateTime ImportedAt { get; set; }
    public string? ErrorDetails { get; set; }
}
