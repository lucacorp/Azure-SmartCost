using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Services;
using AzureSmartCost.Shared.Models;
using AzureSmartCost.Shared.Models.Monitoring;

namespace AzureSmartCost.Api.Controllers
{
    /// <summary>
    /// Controller for advanced monitoring and alerting
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoringController : ControllerBase
    {
        private readonly IMonitoringService _monitoringService;
        private readonly ILogger<MonitoringController> _logger;

        public MonitoringController(IMonitoringService monitoringService, ILogger<MonitoringController> logger)
        {
            _monitoringService = monitoringService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new monitoring alert
        /// </summary>
        /// <param name="alert">Alert configuration</param>
        /// <returns>Created alert with ID</returns>
        [HttpPost("alerts")]
        public async Task<ActionResult<ApiResponse<MonitoringAlert>>> CreateAlert([FromBody] MonitoringAlert alert)
        {
            try
            {
                _logger.LogInformation("Creating monitoring alert {AlertName}", alert.Name);

                if (string.IsNullOrEmpty(alert.Name))
                {
                    return BadRequest(new ApiResponse<MonitoringAlert>
                    {
                        Success = false,
                        Message = "Alert name is required",
                        Data = null
                    });
                }

                var createdAlert = await _monitoringService.CreateAlertAsync(alert);

                return Ok(new ApiResponse<MonitoringAlert>
                {
                    Success = true,
                    Message = "Alert created successfully",
                    Data = createdAlert
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating monitoring alert");
                return StatusCode(500, new ApiResponse<MonitoringAlert>
                {
                    Success = false,
                    Message = "An error occurred while creating the alert",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Update an existing monitoring alert
        /// </summary>
        /// <param name="alertId">Alert ID</param>
        /// <param name="alert">Updated alert configuration</param>
        /// <returns>Updated alert</returns>
        [HttpPut("alerts/{alertId}")]
        public async Task<ActionResult<ApiResponse<MonitoringAlert>>> UpdateAlert(string alertId, [FromBody] MonitoringAlert alert)
        {
            try
            {
                alert.Id = alertId;
                var updatedAlert = await _monitoringService.UpdateAlertAsync(alert);

                return Ok(new ApiResponse<MonitoringAlert>
                {
                    Success = true,
                    Message = "Alert updated successfully",
                    Data = updatedAlert
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<MonitoringAlert>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating monitoring alert {AlertId}", alertId);
                return StatusCode(500, new ApiResponse<MonitoringAlert>
                {
                    Success = false,
                    Message = "An error occurred while updating the alert",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Delete a monitoring alert
        /// </summary>
        /// <param name="alertId">Alert ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("alerts/{alertId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteAlert(string alertId)
        {
            try
            {
                var deleted = await _monitoringService.DeleteAlertAsync(alertId);

                if (!deleted)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Alert not found",
                        Data = false
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Alert deleted successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting monitoring alert {AlertId}", alertId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the alert",
                    Data = false
                });
            }
        }

        /// <summary>
        /// Get all active alerts for a subscription
        /// </summary>
        /// <param name="subscriptionId">Azure subscription ID</param>
        /// <returns>List of active alerts</returns>
        [HttpGet("alerts/subscription/{subscriptionId}")]
        public async Task<ActionResult<ApiResponse<List<MonitoringAlert>>>> GetActiveAlerts(string subscriptionId)
        {
            try
            {
                var alerts = await _monitoringService.GetActiveAlertsAsync(subscriptionId);

                return Ok(new ApiResponse<List<MonitoringAlert>>
                {
                    Success = true,
                    Message = $"Retrieved {alerts.Count} active alerts",
                    Data = alerts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active alerts for subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, new ApiResponse<List<MonitoringAlert>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving alerts",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get performance dashboard data
        /// </summary>
        /// <param name="subscriptionId">Azure subscription ID</param>
        /// <returns>Performance dashboard with metrics and health status</returns>
        [HttpGet("dashboard/{subscriptionId}")]
        public async Task<ActionResult<ApiResponse<PerformanceDashboard>>> GetPerformanceDashboard(string subscriptionId)
        {
            try
            {
                _logger.LogInformation("Generating performance dashboard for subscription {SubscriptionId}", subscriptionId);

                var dashboard = await _monitoringService.GetPerformanceDashboardAsync(subscriptionId);

                return Ok(new ApiResponse<PerformanceDashboard>
                {
                    Success = true,
                    Message = "Performance dashboard generated successfully",
                    Data = dashboard
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance dashboard for subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, new ApiResponse<PerformanceDashboard>
                {
                    Success = false,
                    Message = "An error occurred while generating the performance dashboard",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Execute KQL query against Application Insights
        /// </summary>
        /// <param name="request">KQL query request</param>
        /// <returns>Query results</returns>
        [HttpPost("kql")]
        public async Task<ActionResult<ApiResponse<List<object>>>> ExecuteKqlQuery([FromBody] KqlQueryRequest request)
        {
            try
            {
                _logger.LogInformation("Executing KQL query");

                if (string.IsNullOrEmpty(request.Query))
                {
                    return BadRequest(new ApiResponse<List<object>>
                    {
                        Success = false,
                        Message = "Query is required",
                        Data = null
                    });
                }

                var results = await _monitoringService.ExecuteKqlQueryAsync(request.Query, request.TimeRange);

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = $"Query executed successfully, {results.Count} results",
                    Data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing KQL query");
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "An error occurred while executing the query",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get system health status
        /// </summary>
        /// <returns>Overall system health information</returns>
        [HttpGet("health")]
        public async Task<ActionResult<ApiResponse<SystemHealth>>> GetSystemHealth()
        {
            try
            {
                var health = await _monitoringService.GetSystemHealthAsync();

                return Ok(new ApiResponse<SystemHealth>
                {
                    Success = true,
                    Message = $"System health: {health.Status}",
                    Data = health
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return StatusCode(500, new ApiResponse<SystemHealth>
                {
                    Success = false,
                    Message = "An error occurred while checking system health",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Configure Application Insights integration
        /// </summary>
        /// <param name="config">Application Insights configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("configure-insights")]
        public async Task<ActionResult<ApiResponse<string>>> ConfigureApplicationInsights([FromBody] ApplicationInsightsConfig config)
        {
            try
            {
                _logger.LogInformation("Configuring Application Insights integration");

                await _monitoringService.ConfigureApplicationInsightsAsync(config);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Application Insights configured successfully",
                    Data = "Configuration completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring Application Insights");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while configuring Application Insights",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Send custom telemetry event
        /// </summary>
        /// <param name="request">Telemetry event request</param>
        /// <returns>Success status</returns>
        [HttpPost("telemetry")]
        public async Task<ActionResult<ApiResponse<bool>>> SendTelemetryEvent([FromBody] TelemetryEventRequest request)
        {
            try
            {
                await _monitoringService.SendTelemetryEventAsync(request.EventName, request.Properties, request.Metrics);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Telemetry event sent successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending telemetry event");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while sending telemetry event",
                    Data = false
                });
            }
        }

        /// <summary>
        /// Get alert history for a subscription
        /// </summary>
        /// <param name="subscriptionId">Azure subscription ID</param>
        /// <param name="startDate">Start date for history</param>
        /// <param name="endDate">End date for history</param>
        /// <returns>List of historical alerts</returns>
        [HttpGet("alerts/history/{subscriptionId}")]
        public async Task<ActionResult<ApiResponse<List<AlertSummary>>>> GetAlertHistory(
            string subscriptionId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var history = await _monitoringService.GetAlertHistoryAsync(subscriptionId, start, end);

                return Ok(new ApiResponse<List<AlertSummary>>
                {
                    Success = true,
                    Message = $"Retrieved {history.Count} alert history records",
                    Data = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert history for subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, new ApiResponse<List<AlertSummary>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving alert history",
                    Data = null
                });
            }
        }
    }

    // Request/Response DTOs
    public class KqlQueryRequest
    {
        public string Query { get; set; } = string.Empty;
        public TimeSpan TimeRange { get; set; } = TimeSpan.FromHours(1);
    }

    public class TelemetryEventRequest
    {
        public string EventName { get; set; } = string.Empty;
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
    }
}