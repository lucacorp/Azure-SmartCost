using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Interfaces;

public interface IAzureCostImportService
{
    /// <summary>
    /// Importa dados de custo do Azure Cost Management API para uma subscription específica
    /// </summary>
    Task<CostImportResult> ImportCostsForSubscriptionAsync(
        string tenantId,
        string subscriptionId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Importa dados de custo para todas as subscriptions ativas de um tenant
    /// </summary>
    Task<CostImportResult> ImportCostsForAllSubscriptionsAsync(
        string tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Lista todas as Azure subscriptions disponíveis para o tenant
    /// </summary>
    Task<List<string>> GetAvailableSubscriptionsAsync(string tenantId);
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
