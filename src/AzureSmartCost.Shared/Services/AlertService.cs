using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Services
{
    public interface IAlertService
    {
        Task<List<CostAlert>> EvaluateAlertsAsync(List<CostRecord> costRecords);
        Task<ApiResponse<bool>> SendAlertAsync(CostAlert alert);
        Task<List<CostThreshold>> GetActiveThresholdsAsync();
    }

    public class AlertService : IAlertService
    {
        private readonly ILogger<AlertService> _logger;
        private readonly IConfiguration _configuration;

        public AlertService(ILogger<AlertService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<CostAlert>> EvaluateAlertsAsync(List<CostRecord> costRecords)
        {
            var operationId = Guid.NewGuid().ToString();
            var triggeredAlerts = new List<CostAlert>();

            _logger.LogInformation("🚨 Starting alert evaluation for {RecordCount} cost records. OperationId: {OperationId}", 
                costRecords.Count, operationId);

            try
            {
                var activeThresholds = await GetActiveThresholdsAsync();
                _logger.LogDebug("Evaluating {ThresholdCount} active thresholds. OperationId: {OperationId}", 
                    activeThresholds.Count, operationId);

                // Group cost records by date for daily analysis
                var dailyCosts = costRecords
                    .GroupBy(r => r.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalCost = g.Sum(r => r.TotalCost),
                        ServiceBreakdown = g.GroupBy(r => r.ServiceName).ToDictionary(sg => sg.Key, sg => sg.Sum(r => r.TotalCost)),
                        ResourceGroupBreakdown = g.GroupBy(r => r.ResourceGroup).ToDictionary(rg => rg.Key, rg => rg.Sum(r => r.TotalCost))
                    })
                    .OrderByDescending(d => d.Date)
                    .ToList();

                foreach (var threshold in activeThresholds)
                {
                    var alertsForThreshold = await EvaluateThresholdAsync(threshold, dailyCosts, operationId);
                    triggeredAlerts.AddRange(alertsForThreshold);
                }

                // Check for anomalies (cost spikes)
                var anomalyAlerts = await DetectCostAnomaliesAsync(dailyCosts, operationId);
                triggeredAlerts.AddRange(anomalyAlerts);

                _logger.LogInformation("🚨 Alert evaluation completed. {AlertCount} alerts triggered. OperationId: {OperationId}", 
                    triggeredAlerts.Count, operationId);

                return triggeredAlerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during alert evaluation. OperationId: {OperationId}", operationId);
                throw;
            }
        }

        public async Task<List<CostThreshold>> GetActiveThresholdsAsync()
        {
            // For now, return predefined thresholds. In production, this would come from a database/config
            var thresholds = new List<CostThreshold>
            {
                new CostThreshold
                {
                    Id = "daily-global-warning",
                    Name = "Daily Global Cost Warning",
                    ThresholdAmount = 500m,
                    AlertLevel = AlertLevel.Warning,
                    AlertType = AlertType.DailyCostThreshold,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new CostThreshold
                {
                    Id = "daily-global-critical",
                    Name = "Daily Global Cost Critical",
                    ThresholdAmount = 1000m,
                    AlertLevel = AlertLevel.Critical,
                    AlertType = AlertType.DailyCostThreshold,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new CostThreshold
                {
                    Id = "vm-service-warning",
                    Name = "Virtual Machines Service Warning",
                    ServiceName = "Virtual Machines",
                    ThresholdAmount = 200m,
                    AlertLevel = AlertLevel.Warning,
                    AlertType = AlertType.DailyCostThreshold,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new CostThreshold
                {
                    Id = "production-rg-critical",
                    Name = "Production Resource Group Critical",
                    ResourceGroup = "rg-production",
                    ThresholdAmount = 300m,
                    AlertLevel = AlertLevel.Critical,
                    AlertType = AlertType.DailyCostThreshold,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                }
            };

            await Task.Delay(10); // Simulate async operation
            return thresholds.Where(t => t.IsEnabled).ToList();
        }

        private async Task<List<CostAlert>> EvaluateThresholdAsync(CostThreshold threshold, 
            IEnumerable<dynamic> dailyCosts, string operationId)
        {
            var alerts = new List<CostAlert>();

            try
            {
                var latestDay = dailyCosts.FirstOrDefault();
                if (latestDay == null)
                {
                    _logger.LogDebug("No cost data available for threshold evaluation. ThresholdId: {ThresholdId}, OperationId: {OperationId}", 
                        threshold.Id, operationId);
                    return alerts;
                }

                decimal currentCost = 0m;
                string contextDescription = "";

                // Calculate cost based on threshold scope
                if (!string.IsNullOrEmpty(threshold.ServiceName))
                {
                    currentCost = latestDay.ServiceBreakdown.ContainsKey(threshold.ServiceName) 
                        ? latestDay.ServiceBreakdown[threshold.ServiceName] 
                        : 0m;
                    contextDescription = $"Service: {threshold.ServiceName}";
                }
                else if (!string.IsNullOrEmpty(threshold.ResourceGroup))
                {
                    currentCost = latestDay.ResourceGroupBreakdown.ContainsKey(threshold.ResourceGroup) 
                        ? latestDay.ResourceGroupBreakdown[threshold.ResourceGroup] 
                        : 0m;
                    contextDescription = $"Resource Group: {threshold.ResourceGroup}";
                }
                else
                {
                    currentCost = latestDay.TotalCost;
                    contextDescription = "Global daily cost";
                }

                if (currentCost > threshold.ThresholdAmount)
                {
                    var percentageOver = ((currentCost - threshold.ThresholdAmount) / threshold.ThresholdAmount) * 100;
                    
                    var alert = new CostAlert
                    {
                        Id = Guid.NewGuid().ToString(),
                        ThresholdId = threshold.Id,
                        Level = threshold.AlertLevel,
                        Type = threshold.AlertType,
                        Title = GetAlertTitle(threshold.AlertLevel, threshold.Name),
                        Message = GetAlertMessage(threshold, currentCost, percentageOver, contextDescription),
                        CurrentCost = currentCost,
                        ThresholdAmount = threshold.ThresholdAmount,
                        PercentageOver = Math.Round(percentageOver, 2),
                        ResourceGroup = threshold.ResourceGroup,
                        ServiceName = threshold.ServiceName,
                        TriggeredAt = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            ["Date"] = latestDay.Date.ToString("yyyy-MM-dd"),
                            ["ThresholdName"] = threshold.Name,
                            ["EvaluationContext"] = contextDescription
                        }
                    };

                    alerts.Add(alert);

                    _logger.LogWarning("🚨 ALERT TRIGGERED: {AlertLevel} - {Title}. Current: ${CurrentCost:F2}, Threshold: ${ThresholdAmount:F2} ({PercentageOver:F1}% over). OperationId: {OperationId}", 
                        alert.Level, alert.Title, currentCost, threshold.ThresholdAmount, percentageOver, operationId);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating threshold {ThresholdId}. OperationId: {OperationId}", 
                    threshold.Id, operationId);
            }

            return alerts;
        }

        private async Task<List<CostAlert>> DetectCostAnomaliesAsync(IEnumerable<dynamic> dailyCosts, string operationId)
        {
            var alerts = new List<CostAlert>();

            try
            {
                var costList = dailyCosts.ToList();
                if (costList.Count < 3) return alerts; // Need at least 3 days for anomaly detection

                var latest = costList[0];
                var recent = costList.Skip(1).Take(7).ToList(); // Last 7 days (excluding today)

                if (!recent.Any()) return alerts;

                var recentAverage = recent.Average(d => (decimal)d.TotalCost);
                var currentCost = (decimal)latest.TotalCost;

                // Check for anomalous spike (current cost > 150% of recent average)
                var threshold = recentAverage * 1.5m;
                
                if (currentCost > threshold && currentCost > 100m) // Also require minimum cost to avoid noise
                {
                    var percentageIncrease = ((currentCost - recentAverage) / recentAverage) * 100;
                    
                    var alert = new CostAlert
                    {
                        Id = Guid.NewGuid().ToString(),
                        ThresholdId = "anomaly-detector",
                        Level = AlertLevel.Warning,
                        Type = AlertType.AnomalyCostSpike,
                        Title = "🔥 ANOMALY DETECTED: Unusual Cost Spike",
                        Message = $"Daily cost spike detected! Current cost ${currentCost:F2} is {percentageIncrease:F1}% higher than recent average of ${recentAverage:F2}. This unusual increase requires investigation.",
                        CurrentCost = currentCost,
                        ThresholdAmount = threshold,
                        PercentageOver = Math.Round(percentageIncrease, 2),
                        TriggeredAt = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            ["Date"] = latest.Date.ToString("yyyy-MM-dd"),
                            ["RecentAverage"] = recentAverage,
                            ["DaysAnalyzed"] = recent.Count,
                            ["AnomalyType"] = "Cost Spike"
                        }
                    };

                    alerts.Add(alert);

                    _logger.LogWarning("🔥 ANOMALY DETECTED: Cost spike of {PercentageIncrease:F1}%. Current: ${CurrentCost:F2}, Recent Avg: ${RecentAverage:F2}. OperationId: {OperationId}", 
                        percentageIncrease, currentCost, recentAverage, operationId);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during anomaly detection. OperationId: {OperationId}", operationId);
            }

            return alerts;
        }

        public async Task<ApiResponse<bool>> SendAlertAsync(CostAlert alert)
        {
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("📧 Sending alert notification. AlertId: {AlertId}, Level: {Level}, OperationId: {OperationId}", 
                    alert.Id, alert.Level, operationId);

                // For now, just log the alert (in production, integrate with email/Teams/Slack)
                await LogAlertDetailsAsync(alert, operationId);

                // Simulate notification delivery
                await Task.Delay(100);

                _logger.LogInformation("✅ Alert notification sent successfully. AlertId: {AlertId}, OperationId: {OperationId}", 
                    alert.Id, operationId);

                return ApiResponse<bool>.CreateSuccess(true, "Alert notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert notification. AlertId: {AlertId}, OperationId: {OperationId}", 
                    alert.Id, operationId);
                return ApiResponse<bool>.CreateError("Failed to send alert notification", ex.Message);
            }
        }

        private async Task LogAlertDetailsAsync(CostAlert alert, string operationId)
        {
            var alertIcon = alert.Level switch
            {
                AlertLevel.Info => "ℹ️",
                AlertLevel.Warning => "⚠️",
                AlertLevel.Critical => "🚨",
                _ => "📢"
            };

            _logger.LogWarning(
                "{Icon} =============  COST ALERT  =============\n" +
                "🏷️  Alert ID: {AlertId}\n" +
                "📊  Level: {Level}\n" +
                "📝  Title: {Title}\n" +
                "💰  Current Cost: ${CurrentCost:F2}\n" +
                "🎯  Threshold: ${ThresholdAmount:F2}\n" +
                "📈  Over by: {PercentageOver:F1}%\n" +
                "📅  Date: {Date}\n" +
                "📋  Message: {Message}\n" +
                "🏗️  Resource Group: {ResourceGroup}\n" +
                "⚙️  Service: {ServiceName}\n" +
                "🕐  Triggered At: {TriggeredAt:yyyy-MM-dd HH:mm:ss UTC}\n" +
                "🔍  Operation ID: {OperationId}\n" +
                "================================================",
                alertIcon, alert.Id, alert.Level, alert.Title, alert.CurrentCost, 
                alert.ThresholdAmount, alert.PercentageOver, alert.TriggeredAt.ToString("yyyy-MM-dd"),
                alert.Message, alert.ResourceGroup ?? "All", alert.ServiceName ?? "All",
                alert.TriggeredAt, operationId
            );

            await Task.CompletedTask;
        }

        private static string GetAlertTitle(AlertLevel level, string thresholdName)
        {
            var icon = level switch
            {
                AlertLevel.Info => "ℹ️",
                AlertLevel.Warning => "⚠️",
                AlertLevel.Critical => "🚨",
                _ => "📢"
            };

            return $"{icon} {level.ToString().ToUpper()}: {thresholdName}";
        }

        private static string GetAlertMessage(CostThreshold threshold, decimal currentCost, 
            decimal percentageOver, string contextDescription)
        {
            var severityText = threshold.AlertLevel switch
            {
                AlertLevel.Critical => "CRITICAL threshold has been exceeded",
                AlertLevel.Warning => "WARNING threshold has been exceeded", 
                _ => "threshold has been exceeded"
            };

            return $"{contextDescription} {severityText}. " +
                   $"Current cost is ${currentCost:F2}, which is {percentageOver:F1}% above the ${threshold.ThresholdAmount:F2} threshold. " +
                   $"Please review your Azure resources and consider cost optimization measures.";
        }
    }
}