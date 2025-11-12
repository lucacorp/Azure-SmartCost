using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AzureSmartCost.Shared.Services;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // Require authentication for all endpoints
    public class AlertsController : ControllerBase
    {
        private readonly ILogger<AlertsController> _logger;
        private readonly IAlertService _alertService;
        private readonly CosmosDbService _cosmosDbService;

        public AlertsController(ILogger<AlertsController> logger, 
            IAlertService alertService,
            CosmosDbService cosmosDbService)
        {
            _logger = logger;
            _alertService = alertService;
            _cosmosDbService = cosmosDbService;
        }

        /// <summary>
        /// Obter thresholds ativos
        /// </summary>
        /// <returns>Lista de thresholds configurados</returns>
        [HttpGet("thresholds")]
        [Authorize(Policy = "CanReadCosts")]
        public async Task<ActionResult<ApiResponse<List<CostThreshold>>>> GetActiveThresholds()
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("🚨 API request: Get active thresholds. OperationId: {OperationId}", operationId);

            try
            {
                var thresholds = await _alertService.GetActiveThresholdsAsync();
                
                _logger.LogInformation("✅ Successfully retrieved {ThresholdCount} active thresholds via API. OperationId: {OperationId}", 
                    thresholds.Count, operationId);

                return Ok(ApiResponse<List<CostThreshold>>.CreateSuccess(thresholds, 
                    $"Retrieved {thresholds.Count} active cost thresholds"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving thresholds via API. OperationId: {OperationId}", operationId);
                return StatusCode(500, ApiResponse<List<CostThreshold>>.CreateError("Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Executar avaliação de alertas em tempo real
        /// </summary>
        /// <param name="days">Número de dias para análise (padrão: 7)</param>
        /// <returns>Lista de alertas disparados</returns>
        [HttpPost("evaluate")]
        [Authorize(Policy = "CanManageAlerts")]
        public async Task<ActionResult<ApiResponse<List<CostAlert>>>> EvaluateAlerts([FromQuery] int days = 7)
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("🚨 API request: Evaluate alerts for {Days} days. OperationId: {OperationId}", days, operationId);

            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var endDate = DateTime.UtcNow;

                await _cosmosDbService.InitializeAsync();
                var costRecords = await _cosmosDbService.GetCostRecordsByDateRangeAsync(startDate, endDate);

                if (!costRecords.Any())
                {
                    return Ok(ApiResponse<List<CostAlert>>.CreateSuccess(new List<CostAlert>(), 
                        "No cost data available for alert evaluation"));
                }

                var alerts = await _alertService.EvaluateAlertsAsync(costRecords);

                _logger.LogInformation("✅ Alert evaluation completed via API. {AlertCount} alerts triggered. OperationId: {OperationId}", 
                    alerts.Count, operationId);

                return Ok(ApiResponse<List<CostAlert>>.CreateSuccess(alerts, 
                    $"Alert evaluation completed. {alerts.Count} alerts triggered"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error evaluating alerts via API. OperationId: {OperationId}", operationId);
                return StatusCode(500, ApiResponse<List<CostAlert>>.CreateError("Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Obter estatísticas de alertas
        /// </summary>
        /// <returns>Resumo das atividades de alerta</returns>
        [HttpGet("statistics")]
        [Authorize(Policy = "CanReadCosts")]
        public async Task<ActionResult<ApiResponse<object>>> GetAlertStatistics()
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("📊 API request: Get alert statistics. OperationId: {OperationId}", operationId);

            try
            {
                var thresholds = await _alertService.GetActiveThresholdsAsync();

                // Get recent cost data for evaluation
                var recentCosts = await _cosmosDbService.GetCostRecordsByDateRangeAsync(
                    DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

                List<CostAlert> recentAlerts = new();
                if (recentCosts.Any())
                {
                    recentAlerts = await _alertService.EvaluateAlertsAsync(recentCosts);
                }

                var statistics = new
                {
                    ThresholdConfiguration = new
                    {
                        TotalThresholds = thresholds.Count,
                        ActiveThresholds = thresholds.Count(t => t.IsEnabled),
                        ByAlertLevel = thresholds.GroupBy(t => t.AlertLevel)
                            .Select(g => new { Level = g.Key.ToString(), Count = g.Count() }),
                        ByAlertType = thresholds.GroupBy(t => t.AlertType)
                            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                    },
                    RecentActivity = new
                    {
                        Last7Days = new
                        {
                            TotalAlerts = recentAlerts.Count,
                            CriticalAlerts = recentAlerts.Count(a => a.Level == AlertLevel.Critical),
                            WarningAlerts = recentAlerts.Count(a => a.Level == AlertLevel.Warning),
                            InfoAlerts = recentAlerts.Count(a => a.Level == AlertLevel.Info)
                        },
                        AlertsByType = recentAlerts.GroupBy(a => a.Type)
                            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() }),
                        MostTriggeredThresholds = recentAlerts
                            .Where(a => !string.IsNullOrEmpty(a.ThresholdId))
                            .GroupBy(a => a.ThresholdId)
                            .Select(g => new
                            {
                                ThresholdId = g.Key,
                                ThresholdName = thresholds.FirstOrDefault(t => t.Id == g.Key)?.Name ?? "Unknown",
                                TriggerCount = g.Count()
                            })
                            .OrderByDescending(x => x.TriggerCount)
                            .Take(5)
                    },
                    HealthStatus = new
                    {
                        Status = recentAlerts.Any(a => a.Level == AlertLevel.Critical) ? "🚨 Critical" :
                                recentAlerts.Any(a => a.Level == AlertLevel.Warning) ? "⚠️ Warning" : "✅ Healthy",
                        LastEvaluated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        DataAvailability = recentCosts.Any() ? "✅ Data Available" : "⚠️ No Recent Data"
                    }
                };

                _logger.LogInformation("✅ Successfully generated alert statistics. Status: {Status}. OperationId: {OperationId}", 
                    statistics.HealthStatus.Status, operationId);

                return Ok(ApiResponse<object>.CreateSuccess(statistics, "Alert statistics generated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating alert statistics via API. OperationId: {OperationId}", operationId);
                return StatusCode(500, ApiResponse<object>.CreateError("Internal server error", ex.Message));
            }
        }
    }
}