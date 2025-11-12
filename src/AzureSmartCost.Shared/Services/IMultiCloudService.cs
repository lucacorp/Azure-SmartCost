using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Models.MultiCloud;

namespace AzureSmartCost.Shared.Services
{
    /// <summary>
    /// Service interface for multi-cloud cost management
    /// </summary>
    public interface IMultiCloudService
    {
        // Account Management
        Task<MultiCloudAccount> AddCloudAccountAsync(MultiCloudAccount account);
        Task<List<MultiCloudAccount>> GetCloudAccountsAsync();
        Task<bool> TestConnectionAsync(string accountId);
        Task SyncAllAccountsAsync();

        // Cost Data
        Task<List<UnifiedCostRecord>> GetUnifiedCostDataAsync(DateTime startDate, DateTime endDate);
        Task<MultiCloudDashboard> GetMultiCloudDashboardAsync(DateTime startDate, DateTime endDate);
        
        // Optimization
        Task<List<CloudOptimizationRecommendation>> GetOptimizationRecommendationsAsync();
        Task<List<CrossCloudComparison>> GetCrossCloudComparisonsAsync(string serviceType);

        // Provider Specific
        Task<List<UnifiedCostRecord>> GetAwsCostDataAsync(string accountId, DateTime startDate, DateTime endDate);
        Task<List<UnifiedCostRecord>> GetGcpCostDataAsync(string projectId, DateTime startDate, DateTime endDate);
    }
}