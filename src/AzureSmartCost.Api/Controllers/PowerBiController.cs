using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AzureSmartCost.Shared.Services;
using AzureSmartCost.Shared.Models;
using System.Globalization;

namespace AzureSmartCost.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // Require authentication for all endpoints
    public class PowerBiController : ControllerBase
    {
        private readonly ILogger<PowerBiController> _logger;
        private readonly ICostManagementService _costManagementService;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IPowerBiService _powerBiService;
        private readonly IConfiguration _configuration;

        public PowerBiController(ILogger<PowerBiController> logger, 
            ICostManagementService costManagementService,
            ICosmosDbService cosmosDbService,
            IPowerBiService powerBiService,
            IConfiguration configuration)
        {
            _logger = logger;
            _costManagementService = costManagementService;
            _cosmosDbService = cosmosDbService;
            _powerBiService = powerBiService;
            _configuration = configuration;
        }

        /// <summary>
        /// Get Power BI cost data in structured format
        /// </summary>
        [HttpGet("cost-data")]
        [Authorize(Policy = "CanReadCosts")]
        public async Task<ActionResult<PowerBiCostData>> GetPowerBiCostData(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? subscriptionId = null)
        {
            try
            {
                _logger.LogInformation("🔍 Getting Power BI cost data from {StartDate} to {EndDate}", 
                    startDate, endDate);

                // Set default date range if not provided
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                // Get cost data from Cosmos DB using date-range query
                var costRecords = await _cosmosDbService.GetCostRecordsByDateRangeAsync(start, end, subscriptionId);
                
                // Filter by date range and subscription if provided
                var filteredRecords = costRecords.Where(r => 
                    r.Date >= start && 
                    r.Date <= end &&
                    (string.IsNullOrEmpty(subscriptionId) || r.SubscriptionId == subscriptionId))
                    .ToList();

                // Transform data for Power BI format
                var powerBiData = new PowerBiCostData
                {
                    CostRecords = filteredRecords.Select(record => new PowerBiCostRecord
                    {
                        // Date dimensions for Power BI hierarchies
                        Date = record.Date,
                        Year = record.Date.Year,
                        Quarter = $"Q{(record.Date.Month - 1) / 3 + 1}",
                        Month = record.Date.ToString("yyyy-MM"),
                        MonthName = record.Date.ToString("MMMM", CultureInfo.InvariantCulture),
                        WeekOfYear = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                            record.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday),
                        DayOfWeek = record.Date.ToString("dddd", CultureInfo.InvariantCulture),
                        
                        // Cost information
                        Cost = record.Cost,
                        Currency = record.Currency,
                        ServiceName = record.ServiceName,
                        ResourceGroupName = record.ResourceGroupName,
                        ResourceName = record.ResourceName,
                        Location = record.Location,
                        SubscriptionId = record.SubscriptionId,
                        SubscriptionName = record.SubscriptionName ?? "Unknown",
                        
                        // Additional dimensions
                        MeterCategory = record.MeterCategory ?? "Unknown",
                        MeterSubCategory = record.MeterSubCategory ?? "Unknown",
                        MeterName = record.MeterName ?? "Unknown",
                        UnitOfMeasure = record.UnitOfMeasure ?? "Unknown",
                        ConsumedQuantity = record.ConsumedQuantity ?? 0m,
                        
                        // Calculated fields for Power BI
                        CostCategory = record.Cost switch
                        {
                            < 10 => "Low",
                            < 100 => "Medium", 
                            < 1000 => "High",
                            _ => "Very High"
                        },
                        
                        IsWeekend = record.Date.DayOfWeek == DayOfWeek.Saturday || 
                                   record.Date.DayOfWeek == DayOfWeek.Sunday,
                        
                        // Tags for filtering
                        Tags = record.Tags?.Select(t => $"{t.Key}:{t.Value}").ToArray() ?? Array.Empty<string>()
                    }).ToList(),
                    
                    // Summary information
                    TotalCost = filteredRecords.Sum(r => r.Cost),
                    RecordCount = filteredRecords.Count,
                    DateRange = new PowerBiDateRange
                    {
                        StartDate = start,
                        EndDate = end,
                        DaysIncluded = (int)(end - start).TotalDays + 1
                    },
                    
                    // Generate date dimension table for relationships
                    DateDimension = GenerateDateDimension(start, end),
                    
                    // Service breakdown
                    ServiceBreakdown = filteredRecords
                        .GroupBy(r => r.ServiceName)
                        .Select(g => new PowerBiServiceBreakdown
                        {
                            ServiceName = g.Key,
                            TotalCost = g.Sum(r => r.Cost),
                            RecordCount = g.Count(),
                            AvgDailyCost = g.Sum(r => r.Cost) / Math.Max(1, (end - start).Days),
                            TopResourceGroup = g.GroupBy(r => r.ResourceGroupName)
                                                .OrderByDescending(rg => rg.Sum(r => r.Cost))
                                                .FirstOrDefault()?.Key ?? "Unknown"
                        }).ToList(),
                    
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("✅ Generated Power BI data with {RecordCount} records, total cost: {TotalCost:C}", 
                    powerBiData.RecordCount, powerBiData.TotalCost);

                return Ok(powerBiData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting Power BI cost data");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to retrieve Power BI cost data",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get Power BI cost trends and forecasting data
        /// </summary>
        [HttpGet("cost-trends")]
        [Authorize(Policy = "CanReadCosts")]
        public async Task<ActionResult<PowerBiTrendsData>> GetPowerBiCostTrends(
            [FromQuery] string granularity = "daily",
            [FromQuery] int days = 30)
        {
            try
            {
                _logger.LogInformation("📈 Getting Power BI cost trends with {Granularity} granularity for {Days} days", 
                    granularity, days);

                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-days);

                // Get cost data from Cosmos DB using date-range query
                var costRecords = await _cosmosDbService.GetCostRecordsByDateRangeAsync(startDate, endDate);
                var filteredRecords = costRecords.Where(r => r.Date >= startDate && r.Date <= endDate).ToList();

                var trendsData = new PowerBiTrendsData
                {
                    Granularity = granularity,
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow
                };

                // Group data based on granularity
                switch (granularity.ToLowerInvariant())
                {
                    case "daily":
                        trendsData.Trends = filteredRecords
                            .GroupBy(r => r.Date.Date)
                            .Select(g => new PowerBiTrendPoint
                            {
                                Period = g.Key.ToString("yyyy-MM-dd"),
                                PeriodDate = g.Key,
                                TotalCost = g.Sum(r => r.Cost),
                                RecordCount = g.Count(),
                                AvgCostPerRecord = g.Count() > 0 ? g.Sum(r => r.Cost) / g.Count() : 0,
                                UniqueServices = g.Select(r => r.ServiceName).Distinct().Count(),
                                TopService = g.GroupBy(r => r.ServiceName)
                                             .OrderByDescending(s => s.Sum(r => r.Cost))
                                             .FirstOrDefault()?.Key ?? "Unknown"
                            })
                            .OrderBy(t => t.PeriodDate)
                            .ToList();
                        break;

                    case "weekly":
                        trendsData.Trends = filteredRecords
                            .GroupBy(r => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                                r.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                            .Select(g => new PowerBiTrendPoint
                            {
                                Period = $"Week {g.Key}",
                                PeriodDate = g.Min(r => r.Date),
                                TotalCost = g.Sum(r => r.Cost),
                                RecordCount = g.Count(),
                                AvgCostPerRecord = g.Count() > 0 ? g.Sum(r => r.Cost) / g.Count() : 0,
                                UniqueServices = g.Select(r => r.ServiceName).Distinct().Count(),
                                TopService = g.GroupBy(r => r.ServiceName)
                                             .OrderByDescending(s => s.Sum(r => r.Cost))
                                             .FirstOrDefault()?.Key ?? "Unknown"
                            })
                            .OrderBy(t => t.PeriodDate)
                            .ToList();
                        break;

                    case "monthly":
                        trendsData.Trends = filteredRecords
                            .GroupBy(r => new { r.Date.Year, r.Date.Month })
                            .Select(g => new PowerBiTrendPoint
                            {
                                Period = $"{g.Key.Year}-{g.Key.Month:00}",
                                PeriodDate = new DateTime(g.Key.Year, g.Key.Month, 1),
                                TotalCost = g.Sum(r => r.Cost),
                                RecordCount = g.Count(),
                                AvgCostPerRecord = g.Count() > 0 ? g.Sum(r => r.Cost) / g.Count() : 0,
                                UniqueServices = g.Select(r => r.ServiceName).Distinct().Count(),
                                TopService = g.GroupBy(r => r.ServiceName)
                                             .OrderByDescending(s => s.Sum(r => r.Cost))
                                             .FirstOrDefault()?.Key ?? "Unknown"
                            })
                            .OrderBy(t => t.PeriodDate)
                            .ToList();
                        break;
                }

                // Calculate variance and growth metrics
                for (int i = 1; i < trendsData.Trends.Count; i++)
                {
                    var current = trendsData.Trends[i];
                    var previous = trendsData.Trends[i - 1];
                    
                    current.CostVariance = current.TotalCost - previous.TotalCost;
                    current.CostVariancePercentage = previous.TotalCost > 0 
                        ? (current.TotalCost - previous.TotalCost) / previous.TotalCost * 100 
                        : 0;
                }

                // Summary statistics
                if (trendsData.Trends.Any())
                {
                    trendsData.Summary = new PowerBiTrendsSummary
                    {
                        TotalCost = trendsData.Trends.Sum(t => t.TotalCost),
                        AvgPeriodCost = trendsData.Trends.Average(t => t.TotalCost),
                        MaxPeriodCost = trendsData.Trends.Max(t => t.TotalCost),
                        MinPeriodCost = trendsData.Trends.Min(t => t.TotalCost),
                        TotalRecords = trendsData.Trends.Sum(t => t.RecordCount),
                        PeriodsWithIncrease = trendsData.Trends.Count(t => t.CostVariance > 0),
                        PeriodsWithDecrease = trendsData.Trends.Count(t => t.CostVariance < 0),
                        OverallTrend = DetermineTrend(trendsData.Trends)
                    };
                }

                _logger.LogInformation("✅ Generated Power BI trends data with {TrendCount} trend points", 
                    trendsData.Trends.Count);

                return Ok(trendsData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting Power BI cost trends");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to retrieve Power BI cost trends",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get Power BI embed configuration for reports
        /// </summary>
        [HttpGet("embed-config")]
        [Authorize(Policy = "CanReadCosts")]
        public async Task<ActionResult<PowerBiEmbedConfig>> GetPowerBiEmbedConfig(
            [FromQuery] string reportId,
            [FromQuery] string? workspaceId = null)
        {
            try
            {
                _logger.LogInformation("🔧 Getting Power BI embed config for report {ReportId}", reportId);

                if (string.IsNullOrEmpty(reportId))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Report ID is required"
                    });
                }

                // Use configured workspace ID or default
                var workspace = workspaceId ?? _configuration["PowerBI:WorkspaceId"] ?? "default-workspace";

                // Get embed configuration from Power BI service
                var embedConfig = await _powerBiService.GetEmbedTokenAsync(reportId, workspace);

                if (embedConfig == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to get embed configuration"
                    });
                }

                _logger.LogInformation("✅ Generated Power BI embed config for report {ReportId}", reportId);

                return Ok(embedConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting Power BI embed config");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to get Power BI embed configuration",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Trigger Power BI dataset refresh
        /// </summary>
        [HttpPost("refresh-dataset")]
        [Authorize(Policy = "CanWriteCosts")]
        public async Task<ActionResult<ApiResponse<object>>> RefreshPowerBiDataset(
            [FromQuery] string? datasetId = null,
            [FromQuery] string? workspaceId = null)
        {
            try
            {
                _logger.LogInformation("🔄 Triggering Power BI dataset refresh");

                // Use configured IDs or defaults
                var dataset = datasetId ?? _configuration["PowerBI:DatasetId"] ?? "default-dataset";
                var workspace = workspaceId ?? _configuration["PowerBI:WorkspaceId"] ?? "default-workspace";

                // Trigger dataset refresh
                var success = await _powerBiService.RefreshDatasetAsync(dataset, workspace);

                if (!success)
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to trigger dataset refresh",
                        Errors = new List<string>()
                    });
                }

                _logger.LogInformation("✅ Successfully triggered Power BI dataset refresh");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Dataset refresh triggered successfully",
                    Data = new { DatasetId = dataset, WorkspaceId = workspace, RefreshTime = DateTime.UtcNow }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error refreshing Power BI dataset");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to refresh Power BI dataset",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get Power BI report templates available for SmartCost
        /// </summary>
        [HttpGet("templates")]
        [Authorize(Policy = "CanReadCosts")]
        public ActionResult<IEnumerable<PowerBiReport>> GetPowerBiTemplates()
        {
            try
            {
                _logger.LogInformation("📋 Getting available Power BI report templates");

                var templates = new List<PowerBiReport>
                {
                    SmartCostPowerBiReports.GetExecutiveDashboard(),
                    SmartCostPowerBiReports.GetDetailedCostAnalysis(),
                    SmartCostPowerBiReports.GetCostOptimizationReport(),
                    SmartCostPowerBiReports.GetBudgetAnalysisReport()
                };

                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting Power BI templates");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to retrieve Power BI templates",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Generate date dimension table for Power BI relationships
        /// </summary>
        private List<PowerBiDateDimension> GenerateDateDimension(DateTime startDate, DateTime endDate)
        {
            var dateDimensions = new List<PowerBiDateDimension>();
            
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dateDimensions.Add(new PowerBiDateDimension
                {
                    Date = date,
                    Year = date.Year,
                    Quarter = $"Q{(date.Month - 1) / 3 + 1}",
                    Month = date.ToString("yyyy-MM"),
                    MonthName = date.ToString("MMMM", CultureInfo.InvariantCulture),
                    WeekOfYear = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                        date, CalendarWeekRule.FirstDay, DayOfWeek.Monday),
                    DayOfWeek = date.ToString("dddd", CultureInfo.InvariantCulture),
                    DayOfMonth = date.Day,
                    DayOfYear = date.DayOfYear,
                    IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday,
                    IsHoliday = false, // Could be enhanced to include holiday logic
                    FiscalYear = date.Month >= 4 ? date.Year : date.Year - 1, // Assuming April start
                    FiscalQuarter = date.Month switch
                    {
                        4 or 5 or 6 => "Q1",
                        7 or 8 or 9 => "Q2", 
                        10 or 11 or 12 => "Q3",
                        _ => "Q4"
                    }
                });
            }

            return dateDimensions;
        }

        /// <summary>
        /// Determine overall trend direction
        /// </summary>
        private string DetermineTrend(List<PowerBiTrendPoint> trends)
        {
            if (trends.Count < 2) return "Insufficient Data";

            var increases = trends.Count(t => t.CostVariance > 0);
            var decreases = trends.Count(t => t.CostVariance < 0);
            var stable = trends.Count(t => t.CostVariance == 0);

            if (increases > decreases && increases > stable) return "Increasing";
            if (decreases > increases && decreases > stable) return "Decreasing";
            return "Stable";
        }
    }
}