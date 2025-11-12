using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using AzureSmartCost.Shared.Services;
using AzureSmartCost.Shared.Models.Monitoring;

namespace AzureSmartCost.Shared.Services.Implementation
{
    /// <summary>
    /// Implementation of monitoring service with Application Insights integration
    /// </summary>
    public class MonitoringService : IMonitoringService
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly ILogger<MonitoringService> _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly List<MonitoringAlert> _alerts = new(); // In-memory storage for demo

        public MonitoringService(
            ICosmosDbService cosmosDbService, 
            ILogger<MonitoringService> logger,
            TelemetryClient telemetryClient)
        {
            _cosmosDbService = cosmosDbService;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        public async Task<MonitoringAlert> CreateAlertAsync(MonitoringAlert alert)
        {
            try
            {
                _logger.LogInformation("Creating monitoring alert {AlertName}", alert.Name);
                
                alert.Id = Guid.NewGuid().ToString();
                alert.CreatedAt = DateTime.UtcNow;
                
                _alerts.Add(alert);
                
                // Send telemetry
                await SendTelemetryEventAsync("AlertCreated", new Dictionary<string, string>
                {
                    ["AlertId"] = alert.Id,
                    ["AlertName"] = alert.Name,
                    ["AlertType"] = alert.Type.ToString(),
                    ["Severity"] = alert.Severity.ToString()
                }, new Dictionary<string, double>());
                
                _logger.LogInformation("Created monitoring alert {AlertId} successfully", alert.Id);
                return alert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating monitoring alert");
                throw;
            }
        }

        public async Task<MonitoringAlert> UpdateAlertAsync(MonitoringAlert alert)
        {
            try
            {
                _logger.LogInformation("Updating monitoring alert {AlertId}", alert.Id);
                
                var existingAlert = _alerts.FirstOrDefault(a => a.Id == alert.Id);
                if (existingAlert == null)
                {
                    throw new ArgumentException($"Alert with ID {alert.Id} not found");
                }
                
                // Update properties
                existingAlert.Name = alert.Name;
                existingAlert.Description = alert.Description;
                existingAlert.Condition = alert.Condition;
                existingAlert.IsEnabled = alert.IsEnabled;
                existingAlert.NotificationChannels = alert.NotificationChannels;
                
                await SendTelemetryEventAsync("AlertUpdated", new Dictionary<string, string>
                {
                    ["AlertId"] = alert.Id,
                    ["AlertName"] = alert.Name
                }, new Dictionary<string, double>());
                
                return existingAlert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating monitoring alert {AlertId}", alert.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAlertAsync(string alertId)
        {
            try
            {
                _logger.LogInformation("Deleting monitoring alert {AlertId}", alertId);
                
                var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
                if (alert == null)
                {
                    return false;
                }
                
                _alerts.Remove(alert);
                
                await SendTelemetryEventAsync("AlertDeleted", new Dictionary<string, string>
                {
                    ["AlertId"] = alertId
                }, new Dictionary<string, double>());
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting monitoring alert {AlertId}", alertId);
                throw;
            }
        }

        public async Task<List<MonitoringAlert>> GetActiveAlertsAsync(string subscriptionId)
        {
            try
            {
                _logger.LogInformation("Getting active alerts for subscription {SubscriptionId}", subscriptionId);
                
                // Filter alerts by subscription and active status
                var activeAlerts = _alerts
                    .Where(a => a.SubscriptionId == subscriptionId && a.IsEnabled)
                    .ToList();
                
                // Simulate some active alerts for demo
                if (!activeAlerts.Any())
                {
                    activeAlerts = await GenerateDefaultAlertsAsync(subscriptionId);
                }
                
                return activeAlerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active alerts for subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        public async Task<PerformanceDashboard> GetPerformanceDashboardAsync(string subscriptionId)
        {
            try
            {
                _logger.LogInformation("Generating performance dashboard for subscription {SubscriptionId}", subscriptionId);
                
                var dashboard = new PerformanceDashboard
                {
                    SubscriptionId = subscriptionId,
                    GeneratedAt = DateTime.UtcNow,
                    Metrics = await GenerateMetricSummariesAsync(subscriptionId),
                    ActiveAlerts = await GetActiveAlertSummariesAsync(subscriptionId),
                    OverallHealth = await GetSystemHealthAsync(),
                    Recommendations = await GenerateRecommendationsAsync(subscriptionId)
                };
                
                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance dashboard for subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        public async Task<List<object>> ExecuteKqlQueryAsync(string query, TimeSpan timeRange)
        {
            try
            {
                _logger.LogInformation("Executing KQL query: {Query}", query);
                
                // In real implementation, this would execute against Application Insights
                // For demo purposes, return mock data
                var results = new List<object>
                {
                    new { timestamp = DateTime.UtcNow.AddMinutes(-10), value = 85.2, resourceName = "VM-01" },
                    new { timestamp = DateTime.UtcNow.AddMinutes(-5), value = 92.1, resourceName = "VM-02" },
                    new { timestamp = DateTime.UtcNow, value = 78.5, resourceName = "VM-03" }
                };
                
                await SendTelemetryEventAsync("KqlQueryExecuted", new Dictionary<string, string>
                {
                    ["Query"] = query,
                    ["TimeRange"] = timeRange.ToString()
                }, new Dictionary<string, double>
                {
                    ["ResultCount"] = results.Count
                });
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing KQL query");
                throw;
            }
        }

        public async Task<SystemHealth> GetSystemHealthAsync()
        {
            try
            {
                _logger.LogInformation("Getting system health status");
                
                // Generate health check results
                var components = new List<HealthComponent>
                {
                    new HealthComponent
                    {
                        Name = "API Services",
                        Status = HealthStatus.Healthy,
                        Description = "All API endpoints responding normally"
                    },
                    new HealthComponent
                    {
                        Name = "CosmosDB",
                        Status = HealthStatus.Healthy,
                        Description = "Database connectivity and performance normal"
                    },
                    new HealthComponent
                    {
                        Name = "Power BI Integration",
                        Status = HealthStatus.Warning,
                        Description = "Some reports experiencing slow loading"
                    },
                    new HealthComponent
                    {
                        Name = "Cost Analytics",
                        Status = HealthStatus.Healthy,
                        Description = "ML models operating within normal parameters"
                    }
                };
                
                // Calculate overall health score
                var healthyCount = components.Count(c => c.Status == HealthStatus.Healthy);
                var warningCount = components.Count(c => c.Status == HealthStatus.Warning);
                var criticalCount = components.Count(c => c.Status == HealthStatus.Critical);
                
                var overallScore = ((healthyCount * 100) + (warningCount * 75) + (criticalCount * 25)) / (double)components.Count;
                var overallStatus = overallScore switch
                {
                    >= 90 => HealthStatus.Healthy,
                    >= 70 => HealthStatus.Warning,
                    _ => HealthStatus.Critical
                };
                
                var systemHealth = new SystemHealth
                {
                    Status = overallStatus,
                    OverallScore = overallScore,
                    Components = components,
                    LastUpdated = DateTime.UtcNow
                };
                
                await SendTelemetryEventAsync("HealthCheckCompleted", new Dictionary<string, string>
                {
                    ["OverallStatus"] = overallStatus.ToString()
                }, new Dictionary<string, double>
                {
                    ["OverallScore"] = overallScore
                });
                
                return systemHealth;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                throw;
            }
        }

        public async Task ConfigureApplicationInsightsAsync(ApplicationInsightsConfig config)
        {
            try
            {
                _logger.LogInformation("Configuring Application Insights integration");
                
                // Configure custom metrics
                foreach (var metric in config.CustomMetrics)
                {
                    _logger.LogInformation("Registering custom metric: {MetricName}", metric.Name);
                }
                
                // Configure alert queries
                foreach (var query in config.AlertQueries)
                {
                    _logger.LogInformation("Setting up alert query: {QueryName}", query.Name);
                }
                
                await SendTelemetryEventAsync("ApplicationInsightsConfigured", new Dictionary<string, string>
                {
                    ["WorkspaceId"] = config.WorkspaceId
                }, new Dictionary<string, double>
                {
                    ["CustomMetricsCount"] = config.CustomMetrics.Count,
                    ["AlertQueriesCount"] = config.AlertQueries.Count
                });
                
                _logger.LogInformation("Application Insights configuration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring Application Insights");
                throw;
            }
        }

        public async Task SendTelemetryEventAsync(string eventName, Dictionary<string, string> properties, Dictionary<string, double> metrics)
        {
            try
            {
                _telemetryClient?.TrackEvent(eventName, properties, metrics);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending telemetry event {EventName}", eventName);
                // Don't rethrow - telemetry failures shouldn't break the application
            }
        }

        public async Task<List<AlertSummary>> GetAlertHistoryAsync(string subscriptionId, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Getting alert history for subscription {SubscriptionId} from {StartDate} to {EndDate}", 
                    subscriptionId, startDate, endDate);
                
                // Filter alerts by subscription and date range
                var alertHistory = _alerts
                    .Where(a => a.SubscriptionId == subscriptionId && 
                               a.LastTriggered.HasValue &&
                               a.LastTriggered.Value >= startDate &&
                               a.LastTriggered.Value <= endDate)
                    .Select(a => new AlertSummary
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Severity = a.Severity,
                        TriggeredAt = a.LastTriggered ?? DateTime.UtcNow,
                        ResourceName = a.ResourceId,
                        Description = a.Description
                    })
                    .OrderByDescending(a => a.TriggeredAt)
                    .ToList();
                
                return alertHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert history for subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        #region Private Helper Methods

        private async Task<List<MonitoringAlert>> GenerateDefaultAlertsAsync(string subscriptionId)
        {
            var defaultAlerts = new List<MonitoringAlert>
            {
                new MonitoringAlert
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "High Cost Alert",
                    Type = AlertType.CostThreshold,
                    Severity = AlertSeverity.Warning,
                    Description = "Alert when daily costs exceed $1000",
                    SubscriptionId = subscriptionId,
                    Condition = new AlertCondition
                    {
                        MetricName = "DailyCost",
                        Operator = ComparisonOperator.GreaterThan,
                        Threshold = 1000,
                        EvaluationWindow = TimeSpan.FromHours(1)
                    },
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    LastTriggered = DateTime.UtcNow.AddHours(-2),
                    TriggerCount = 3
                },
                new MonitoringAlert
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Anomaly Detection",
                    Type = AlertType.AnomalyDetection,
                    Severity = AlertSeverity.Error,
                    Description = "Unusual spending pattern detected",
                    SubscriptionId = subscriptionId,
                    Condition = new AlertCondition
                    {
                        MetricName = "CostAnomaly",
                        Operator = ComparisonOperator.GreaterThan,
                        Threshold = 2.0, // 2 standard deviations
                        EvaluationWindow = TimeSpan.FromMinutes(30)
                    },
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    LastTriggered = DateTime.UtcNow.AddMinutes(-45),
                    TriggerCount = 1
                }
            };

            _alerts.AddRange(defaultAlerts);
            return defaultAlerts;
        }

        private async Task<List<MetricSummary>> GenerateMetricSummariesAsync(string subscriptionId)
        {
            return new List<MetricSummary>
            {
                new MetricSummary
                {
                    Name = "Total Monthly Cost",
                    CurrentValue = 2847.50,
                    PreviousValue = 2654.30,
                    Unit = "USD",
                    Trend = TrendDirection.Up,
                    TimeSeries = GenerateTimeSeriesData(2000, 3000, 30)
                },
                new MetricSummary
                {
                    Name = "Active Resources",
                    CurrentValue = 247,
                    PreviousValue = 251,
                    Unit = "count",
                    Trend = TrendDirection.Down,
                    TimeSeries = GenerateTimeSeriesData(200, 300, 30)
                },
                new MetricSummary
                {
                    Name = "Average Response Time",
                    CurrentValue = 158.3,
                    PreviousValue = 142.7,
                    Unit = "ms",
                    Trend = TrendDirection.Up,
                    TimeSeries = GenerateTimeSeriesData(100, 200, 30)
                }
            };
        }

        private async Task<List<AlertSummary>> GetActiveAlertSummariesAsync(string subscriptionId)
        {
            var activeAlerts = await GetActiveAlertsAsync(subscriptionId);
            
            return activeAlerts
                .Where(a => a.LastTriggered.HasValue && a.LastTriggered.Value > DateTime.UtcNow.AddDays(-1))
                .Select(a => new AlertSummary
                {
                    Id = a.Id,
                    Name = a.Name,
                    Severity = a.Severity,
                    TriggeredAt = a.LastTriggered ?? DateTime.UtcNow,
                    ResourceName = a.ResourceId,
                    Description = a.Description
                })
                .ToList();
        }

        private async Task<List<RecommendedAction>> GenerateRecommendationsAsync(string subscriptionId)
        {
            return new List<RecommendedAction>
            {
                new RecommendedAction
                {
                    Title = "Optimize VM Sizes",
                    Description = "Several VMs are under-utilized and can be resized to save costs",
                    Priority = ActionPriority.High,
                    Steps = new List<string>
                    {
                        "Analyze VM performance metrics",
                        "Identify under-utilized instances",
                        "Resize to appropriate SKUs",
                        "Monitor performance after changes"
                    },
                    EstimatedTime = TimeSpan.FromHours(2)
                },
                new RecommendedAction
                {
                    Title = "Enable Auto-Shutdown",
                    Description = "Configure auto-shutdown for development VMs to reduce costs",
                    Priority = ActionPriority.Medium,
                    Steps = new List<string>
                    {
                        "Identify development VMs",
                        "Configure shutdown schedules",
                        "Set up notifications",
                        "Monitor usage patterns"
                    },
                    EstimatedTime = TimeSpan.FromMinutes(30)
                }
            };
        }

        private List<DataPoint> GenerateTimeSeriesData(double minValue, double maxValue, int pointCount)
        {
            var random = new Random();
            var dataPoints = new List<DataPoint>();
            var now = DateTime.UtcNow;
            
            for (int i = pointCount - 1; i >= 0; i--)
            {
                dataPoints.Add(new DataPoint
                {
                    Timestamp = now.AddHours(-i),
                    Value = random.NextDouble() * (maxValue - minValue) + minValue
                });
            }
            
            return dataPoints;
        }

        #endregion
    }
}