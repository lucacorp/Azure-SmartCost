using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace AzureSmartCost.Functions
{
    public class AnalyticsService
    {
        private readonly Container _costRecordsContainer;
        private readonly Container _eventsContainer;

        public AnalyticsService(CosmosClient cosmosClient, string databaseName)
        {
            var database = cosmosClient.GetDatabase(databaseName);
            _costRecordsContainer = database.GetContainer("CostRecords");
            _eventsContainer = database.GetContainer("Events");
        }

        public async Task<CostAnalytics> GetCostAnalytics(string subscriptionId, DateTime startDate, DateTime endDate)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.subscriptionId = @subscriptionId AND c.date >= @startDate AND c.date <= @endDate")
                .WithParameter("@subscriptionId", subscriptionId)
                .WithParameter("@startDate", startDate.ToString("yyyy-MM-dd"))
                .WithParameter("@endDate", endDate.ToString("yyyy-MM-dd"));

            var results = new List<CostRecord>();
            using var iterator = _costRecordsContainer.GetItemQueryIterator<CostRecord>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return CalculateAnalytics(results, startDate, endDate);
        }

        public async Task<List<ServiceCostBreakdown>> GetServiceBreakdown(string subscriptionId, DateTime startDate, DateTime endDate)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.subscriptionId = @subscriptionId AND c.date >= @startDate AND c.date <= @endDate")
                .WithParameter("@subscriptionId", subscriptionId)
                .WithParameter("@startDate", startDate.ToString("yyyy-MM-dd"))
                .WithParameter("@endDate", endDate.ToString("yyyy-MM-dd"));

            var results = new List<CostRecord>();
            using var iterator = _costRecordsContainer.GetItemQueryIterator<CostRecord>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results
                .GroupBy(r => r.ServiceName ?? "Unknown")
                .Select(g => new ServiceCostBreakdown
                {
                    ServiceName = g.Key,
                    TotalCost = g.Sum(r => r.Cost),
                    Currency = g.First().Currency ?? "USD",
                    ResourceCount = g.Select(r => r.ResourceId).Distinct().Count(),
                    AverageDailyCost = g.Average(r => r.Cost)
                })
                .OrderByDescending(s => s.TotalCost)
                .ToList();
        }

        public async Task<List<DailyCostTrend>> GetDailyCostTrend(string subscriptionId, DateTime startDate, DateTime endDate)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.subscriptionId = @subscriptionId AND c.date >= @startDate AND c.date <= @endDate")
                .WithParameter("@subscriptionId", subscriptionId)
                .WithParameter("@startDate", startDate.ToString("yyyy-MM-dd"))
                .WithParameter("@endDate", endDate.ToString("yyyy-MM-dd"));

            var results = new List<CostRecord>();
            using var iterator = _costRecordsContainer.GetItemQueryIterator<CostRecord>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results
                .GroupBy(r => r.Date)
                .Select(g => new DailyCostTrend
                {
                    Date = g.Key,
                    TotalCost = g.Sum(r => r.Cost),
                    Currency = g.First().Currency ?? "USD"
                })
                .OrderBy(d => d.Date)
                .ToList();
        }

        public async Task<List<ResourceCostRanking>> GetTopCostResources(string subscriptionId, DateTime startDate, DateTime endDate, int topN = 10)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.subscriptionId = @subscriptionId AND c.date >= @startDate AND c.date <= @endDate")
                .WithParameter("@subscriptionId", subscriptionId)
                .WithParameter("@startDate", startDate.ToString("yyyy-MM-dd"))
                .WithParameter("@endDate", endDate.ToString("yyyy-MM-dd"));

            var results = new List<CostRecord>();
            using var iterator = _costRecordsContainer.GetItemQueryIterator<CostRecord>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results
                .GroupBy(r => new { r.ResourceId, r.ResourceName, r.ServiceName })
                .Select(g => new ResourceCostRanking
                {
                    ResourceId = g.Key.ResourceId,
                    ResourceName = g.Key.ResourceName ?? "Unknown",
                    ServiceName = g.Key.ServiceName ?? "Unknown",
                    TotalCost = g.Sum(r => r.Cost),
                    Currency = g.First().Currency ?? "USD"
                })
                .OrderByDescending(r => r.TotalCost)
                .Take(topN)
                .ToList();
        }

        private CostAnalytics CalculateAnalytics(List<CostRecord> records, DateTime startDate, DateTime endDate)
        {
            if (!records.Any())
            {
                return new CostAnalytics
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalCost = 0,
                    Currency = "USD",
                    DailyAverage = 0,
                    TrendPercentage = 0,
                    TopService = "N/A"
                };
            }

            var totalCost = records.Sum(r => r.Cost);
            var days = (endDate - startDate).Days + 1;
            var dailyAverage = totalCost / days;

            // Calculate trend (compare first half vs second half)
            var midDate = startDate.AddDays(days / 2.0);
            var firstHalfCost = records.Where(r => r.Date < midDate).Sum(r => r.Cost);
            var secondHalfCost = records.Where(r => r.Date >= midDate).Sum(r => r.Cost);
            
            var trendPercentage = firstHalfCost > 0 
                ? ((secondHalfCost - firstHalfCost) / firstHalfCost) * 100 
                : 0;

            var topService = records
                .GroupBy(r => r.ServiceName)
                .OrderByDescending(g => g.Sum(r => r.Cost))
                .FirstOrDefault()?.Key ?? "N/A";

            return new CostAnalytics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalCost = totalCost,
                Currency = records.First().Currency ?? "USD",
                DailyAverage = dailyAverage,
                TrendPercentage = trendPercentage,
                TopService = topService,
                RecordCount = records.Count
            };
        }
    }

    // Models
    public class CostRecord
    {
        public string Id { get; set; }
        public string SubscriptionId { get; set; }
        public DateTime Date { get; set; }
        public string ResourceId { get; set; }
        public string ResourceName { get; set; }
        public string ServiceName { get; set; }
        public double Cost { get; set; }
        public string Currency { get; set; }
        public string PartitionKey { get; set; }
    }

    public class CostAnalytics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double TotalCost { get; set; }
        public string Currency { get; set; }
        public double DailyAverage { get; set; }
        public double TrendPercentage { get; set; }
        public string TopService { get; set; }
        public int RecordCount { get; set; }
    }

    public class ServiceCostBreakdown
    {
        public string ServiceName { get; set; }
        public double TotalCost { get; set; }
        public string Currency { get; set; }
        public int ResourceCount { get; set; }
        public double AverageDailyCost { get; set; }
    }

    public class DailyCostTrend
    {
        public DateTime Date { get; set; }
        public double TotalCost { get; set; }
        public string Currency { get; set; }
    }

    public class ResourceCostRanking
    {
        public string ResourceId { get; set; }
        public string ResourceName { get; set; }
        public string ServiceName { get; set; }
        public double TotalCost { get; set; }
        public string Currency { get; set; }
    }
}
