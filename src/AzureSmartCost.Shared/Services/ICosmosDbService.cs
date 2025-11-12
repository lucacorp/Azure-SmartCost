using AzureSmartCost.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureSmartCost.Shared.Services
{
    public interface ICosmosDbService
    {
        Task InitializeAsync();
        Task<CostRecord> SaveCostRecordAsync(CostRecord record);
        Task<List<CostRecord>> SaveCostRecordsAsync(List<CostRecord> records);
        Task<List<CostRecord>> GetCostRecordsByDateRangeAsync(DateTime startDate, DateTime endDate, string? subscriptionId = null);
        Task<List<CostRecord>> GetLatestCostRecordsAsync(int count = 10);
        Task<decimal> GetTotalCostAsync(DateTime startDate, DateTime endDate, string? subscriptionId = null);
    }
}