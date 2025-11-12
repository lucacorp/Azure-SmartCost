using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Models.Monitoring;

namespace AzureSmartCost.Shared.Services
{
    /// <summary>
    /// Service interface for advanced monitoring and alerting
    /// </summary>
    public interface IMonitoringService
    {
        /// <summary>
        /// Create a new monitoring alert
        /// </summary>
        Task<MonitoringAlert> CreateAlertAsync(MonitoringAlert alert);
        
        /// <summary>
        /// Update an existing monitoring alert
        /// </summary>
        Task<MonitoringAlert> UpdateAlertAsync(MonitoringAlert alert);
        
        /// <summary>
        /// Delete a monitoring alert
        /// </summary>
        Task<bool> DeleteAlertAsync(string alertId);
        
        /// <summary>
        /// Get all active alerts for a subscription
        /// </summary>
        Task<List<MonitoringAlert>> GetActiveAlertsAsync(string subscriptionId);
        
        /// <summary>
        /// Get performance dashboard data
        /// </summary>
        Task<PerformanceDashboard> GetPerformanceDashboardAsync(string subscriptionId);
        
        /// <summary>
        /// Execute KQL queries against Application Insights
        /// </summary>
        Task<List<object>> ExecuteKqlQueryAsync(string query, TimeSpan timeRange);
        
        /// <summary>
        /// Get system health status
        /// </summary>
        Task<SystemHealth> GetSystemHealthAsync();
        
        /// <summary>
        /// Configure Application Insights integration
        /// </summary>
        Task ConfigureApplicationInsightsAsync(ApplicationInsightsConfig config);
        
        /// <summary>
        /// Send custom telemetry event
        /// </summary>
        Task SendTelemetryEventAsync(string eventName, Dictionary<string, string> properties, Dictionary<string, double> metrics);
        
        /// <summary>
        /// Get alert history for a subscription
        /// </summary>
        Task<List<AlertSummary>> GetAlertHistoryAsync(string subscriptionId, DateTime startDate, DateTime endDate);
    }
}