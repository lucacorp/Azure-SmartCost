using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Core;

namespace AzureSmartCost.Functions
{
    public class AnalyticsService
    {
        private readonly HttpClient _httpClient;
        private readonly DefaultAzureCredential _credential;
        private const string CostManagementApiVersion = "2023-11-01";

        public AnalyticsService()
        {
            _httpClient = new HttpClient();
            _credential = new DefaultAzureCredential();
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var tokenRequestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
            var token = await _credential.GetTokenAsync(tokenRequestContext);
            return token.Token;
        }

        public async Task<CostAnalytics> GetCostAnalytics(string subscriptionId, DateTime startDate, DateTime endDate)
        {
            var costs = await QueryCostManagementAsync(subscriptionId, startDate, endDate);
            return CalculateAnalytics(costs, startDate, endDate);
        }

        public async Task<List<ServiceCostBreakdown>> GetServiceBreakdown(string subscriptionId, DateTime startDate, DateTime endDate)
        {
            var costs = await QueryCostManagementAsync(subscriptionId, startDate, endDate, groupBy: "ServiceName");
            
            return costs
                .GroupBy(r => r.ServiceName ?? "Unknown")
                .Select(g => new ServiceCostBreakdown
                {
                    ServiceName = g.Key,
                    TotalCost = g.Sum(r => r.Cost),
                    Currency = g.First().Currency ?? "USD",
                    ResourceCount = g.Select(r => r.ResourceId).Distinct().Count(),
                    AverageDailyCost = g.Count() > 0 ? g.Sum(r => r.Cost) / g.Count() : 0
                })
                .OrderByDescending(s => s.TotalCost)
                .ToList();
        }

        public async Task<List<DailyCostTrend>> GetDailyCostTrend(string subscriptionId, DateTime startDate, DateTime endDate)
        {
            var costs = await QueryCostManagementAsync(subscriptionId, startDate, endDate);
            
            return costs
                .GroupBy(r => r.Date.Date)
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
            var costs = await QueryCostManagementAsync(subscriptionId, startDate, endDate);
            
            return costs
                .GroupBy(r => new { r.ResourceId, r.ResourceName, r.ServiceName })
                .Select(g => new ResourceCostRanking
                {
                    ResourceId = g.Key.ResourceId ?? "Unknown",
                    ResourceName = g.Key.ResourceName ?? "Unknown",
                    ServiceName = g.Key.ServiceName ?? "Unknown",
                    TotalCost = g.Sum(r => r.Cost),
                    Currency = g.First().Currency ?? "USD"
                })
                .OrderByDescending(r => r.TotalCost)
                .Take(topN)
                .ToList();
        }

        private async Task<List<CostRecord>> QueryCostManagementAsync(string subscriptionId, DateTime startDate, DateTime endDate, string? groupBy = null)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                var url = $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.CostManagement/query?api-version={CostManagementApiVersion}";

                var requestBody = new
                {
                    type = "ActualCost",
                    timeframe = "Custom",
                    timePeriod = new
                    {
                        from = startDate.ToString("yyyy-MM-ddT00:00:00Z"),
                        to = endDate.ToString("yyyy-MM-ddT23:59:59Z")
                    },
                    dataset = new
                    {
                        granularity = "Daily",
                        aggregation = new Dictionary<string, object>
                        {
                            { "totalCost", new { name = "Cost", function = "Sum" } }
                        },
                        grouping = string.IsNullOrEmpty(groupBy) 
                            ? new object[] { }
                            : new object[]
                            {
                                new { type = "Dimension", name = "ServiceName" },
                                new { type = "Dimension", name = "ResourceId" }
                            }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Cost Management API error: {response.StatusCode} - {error}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CostManagementResponse>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                return ParseCostManagementResponse(result, subscriptionId);
            }
            catch (Exception ex)
            {
                // Log error and return empty list instead of failing
                Console.WriteLine($"Error querying Cost Management API: {ex.Message}");
                return new List<CostRecord>();
            }
        }

        private List<CostRecord> ParseCostManagementResponse(CostManagementResponse? response, string subscriptionId)
        {
            if (response?.Properties?.Rows == null || !response.Properties.Rows.Any())
                return new List<CostRecord>();

            var records = new List<CostRecord>();
            var columns = response.Properties.Columns ?? new List<Column>();

            foreach (var row in response.Properties.Rows)
            {
                try
                {
                    var record = new CostRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        SubscriptionId = subscriptionId,
                        Date = ParseDate(row, columns),
                        Cost = ParseCost(row, columns),
                        Currency = ParseCurrency(row, columns),
                        ServiceName = ParseServiceName(row, columns),
                        ResourceId = ParseResourceId(row, columns),
                        ResourceName = ParseResourceName(row, columns),
                        PartitionKey = subscriptionId
                    };

                    records.Add(record);
                }
                catch
                {
                    // Skip invalid rows
                    continue;
                }
            }

            return records;
        }

        private DateTime ParseDate(List<object> row, List<Column> columns)
        {
            var dateCol = columns.FindIndex(c => c.Name?.Equals("UsageDate", StringComparison.OrdinalIgnoreCase) == true 
                                               || c.Name?.Equals("Date", StringComparison.OrdinalIgnoreCase) == true);
            if (dateCol >= 0 && dateCol < row.Count && row[dateCol] != null)
            {
                if (DateTime.TryParse(row[dateCol].ToString(), out var date))
                    return date;
            }
            return DateTime.UtcNow;
        }

        private double ParseCost(List<object> row, List<Column> columns)
        {
            var costCol = columns.FindIndex(c => c.Name?.Equals("Cost", StringComparison.OrdinalIgnoreCase) == true
                                               || c.Name?.Equals("PreTaxCost", StringComparison.OrdinalIgnoreCase) == true);
            if (costCol >= 0 && costCol < row.Count && row[costCol] != null)
            {
                if (double.TryParse(row[costCol].ToString(), out var cost))
                    return cost;
            }
            return 0;
        }

        private string ParseCurrency(List<object> row, List<Column> columns)
        {
            var currencyCol = columns.FindIndex(c => c.Name?.Equals("Currency", StringComparison.OrdinalIgnoreCase) == true);
            if (currencyCol >= 0 && currencyCol < row.Count && row[currencyCol] != null)
            {
                return row[currencyCol].ToString() ?? "USD";
            }
            return "USD";
        }

        private string? ParseServiceName(List<object> row, List<Column> columns)
        {
            var serviceCol = columns.FindIndex(c => c.Name?.Equals("ServiceName", StringComparison.OrdinalIgnoreCase) == true);
            if (serviceCol >= 0 && serviceCol < row.Count && row[serviceCol] != null)
            {
                return row[serviceCol].ToString();
            }
            return "Unknown";
        }

        private string? ParseResourceId(List<object> row, List<Column> columns)
        {
            var resourceCol = columns.FindIndex(c => c.Name?.Equals("ResourceId", StringComparison.OrdinalIgnoreCase) == true);
            if (resourceCol >= 0 && resourceCol < row.Count && row[resourceCol] != null)
            {
                return row[resourceCol].ToString();
            }
            return null;
        }

        private string? ParseResourceName(List<object> row, List<Column> columns)
        {
            var resourceId = ParseResourceId(row, columns);
            if (!string.IsNullOrEmpty(resourceId))
            {
                var parts = resourceId.Split('/');
                return parts.Length > 0 ? parts[^1] : null;
            }
            return null;
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
                    TopService = "N/A",
                    RecordCount = 0
                };
            }

            var totalCost = records.Sum(r => r.Cost);
            var days = (endDate - startDate).Days + 1;
            var dailyAverage = days > 0 ? totalCost / days : 0;

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

    // Cost Management API Response Models
    public class CostManagementResponse
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public ResponseProperties? Properties { get; set; }
    }

    public class ResponseProperties
    {
        public List<Column>? Columns { get; set; }
        public List<List<object>>? Rows { get; set; }
    }

    public class Column
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
    }

    // Domain Models
    public class CostRecord
    {
        public string Id { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? ResourceId { get; set; }
        public string? ResourceName { get; set; }
        public string? ServiceName { get; set; }
        public double Cost { get; set; }
        public string Currency { get; set; } = "USD";
        public string PartitionKey { get; set; } = string.Empty;
    }

    public class CostAnalytics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double TotalCost { get; set; }
        public string Currency { get; set; } = "USD";
        public double DailyAverage { get; set; }
        public double TrendPercentage { get; set; }
        public string TopService { get; set; } = "N/A";
        public int RecordCount { get; set; }
    }

    public class ServiceCostBreakdown
    {
        public string ServiceName { get; set; } = string.Empty;
        public double TotalCost { get; set; }
        public string Currency { get; set; } = "USD";
        public int ResourceCount { get; set; }
        public double AverageDailyCost { get; set; }
    }

    public class DailyCostTrend
    {
        public DateTime Date { get; set; }
        public double TotalCost { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class ResourceCostRanking
    {
        public string ResourceId { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public double TotalCost { get; set; }
        public string Currency { get; set; } = "USD";
    }
}
