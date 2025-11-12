using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Services
{
    public interface ICostManagementService
    {
        Task<ApiResponse<List<CostRecord>>> GetDailyCostAsync();
        Task<decimal> GetCurrentCostAsync();
        Task<List<CostRecord>> GetCostHistoryAsync(DateTime startDate, DateTime endDate);
    }
}