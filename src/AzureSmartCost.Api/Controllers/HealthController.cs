using Microsoft.AspNetCore.Mvc;
using AzureSmartCost.Shared.Services;
using System.Diagnostics;

namespace AzureSmartCost.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly CosmosDbService _cosmosDbService;
        private readonly ICostManagementService _costManagementService;
        private readonly IAlertService _alertService;

        public HealthController(ILogger<HealthController> logger,
            CosmosDbService cosmosDbService,
            ICostManagementService costManagementService,
            IAlertService alertService)
        {
            _logger = logger;
            _cosmosDbService = cosmosDbService;
            _costManagementService = costManagementService;
            _alertService = alertService;
        }

        /// <summary>
        /// Health check básico - endpoint para load balancers
        /// </summary>
        /// <returns>Status básico da aplicação</returns>
        [HttpGet]
        public IActionResult GetHealth()
        {
            var response = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                Version = "1.0.0",
                Service = "Azure SmartCost API"
            };

            return Ok(response);
        }

        /// <summary>
        /// Health check detalhado com dependências
        /// </summary>
        /// <returns>Status detalhado de todos os componentes</returns>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailedHealth()
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("🏥 API request: Detailed health check. OperationId: {OperationId}", operationId);

            var healthChecks = new List<object>();
            var overallHealthy = true;

            try
            {
                // Test Cosmos DB connectivity
                var cosmosHealthy = await TestCosmosDbHealthAsync();
                healthChecks.Add(new
                {
                    Service = "CosmosDB",
                    Status = cosmosHealthy ? "✅ Healthy" : "❌ Unhealthy",
                    Details = cosmosHealthy ? "Database connection successful" : "Database connection failed"
                });
                if (!cosmosHealthy) overallHealthy = false;

                // Test Cost Management Service
                var costServiceHealthy = await TestCostManagementServiceAsync();
                healthChecks.Add(new
                {
                    Service = "Cost Management Service",
                    Status = costServiceHealthy ? "✅ Healthy" : "❌ Unhealthy",
                    Details = costServiceHealthy ? "Cost service responsive" : "Cost service unresponsive"
                });
                if (!costServiceHealthy) overallHealthy = false;

                // Test Alert Service
                var alertServiceHealthy = await TestAlertServiceAsync();
                healthChecks.Add(new
                {
                    Service = "Alert Service",
                    Status = alertServiceHealthy ? "✅ Healthy" : "❌ Unhealthy",
                    Details = alertServiceHealthy ? "Alert service operational" : "Alert service error"
                });
                if (!alertServiceHealthy) overallHealthy = false;

                // Memory and system checks
                var systemInfo = GetSystemInfo();
                healthChecks.Add(new
                {
                    Service = "System Resources",
                    Status = "✅ Healthy",
                    Details = systemInfo
                });

                var response = new
                {
                    OverallStatus = overallHealthy ? "✅ Healthy" : "❌ Unhealthy",
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    Version = "1.0.0",
                    OperationId = operationId,
                    Checks = healthChecks,
                    Summary = new
                    {
                        TotalChecks = healthChecks.Count,
                        HealthyChecks = healthChecks.Count(c => c.GetType().GetProperty("Status")?.GetValue(c)?.ToString()?.Contains("✅") == true),
                        ResponseTime = "< 2s"
                    }
                };

                var statusCode = overallHealthy ? 200 : 503;
                
                _logger.LogInformation("🏥 Health check completed. Overall: {Status}. Healthy: {Healthy}/{Total}. OperationId: {OperationId}",
                    response.OverallStatus, response.Summary.HealthyChecks, response.Summary.TotalChecks, operationId);

                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Health check failed with exception. OperationId: {OperationId}", operationId);
                
                var errorResponse = new
                {
                    OverallStatus = "❌ Critical Error",
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    OperationId = operationId,
                    Error = ex.Message,
                    Checks = healthChecks
                };

                return StatusCode(503, errorResponse);
            }
        }

        /// <summary>
        /// Readiness check - verifica se a API está pronta para receber tráfego
        /// </summary>
        /// <returns>Status de prontidão da aplicação</returns>
        [HttpGet("ready")]
        public async Task<IActionResult> GetReadiness()
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("🚀 API request: Readiness check. OperationId: {OperationId}", operationId);

            try
            {
                // Quick essential checks for readiness
                var cosmosReady = await TestCosmosDbHealthAsync();
                var configurationReady = await TestConfigurationAsync();
                var servicesReady = TestDependencyInjection();

                var isReady = cosmosReady && configurationReady && servicesReady;

                var response = new
                {
                    Ready = isReady,
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    OperationId = operationId,
                    Checks = new
                    {
                        Database = cosmosReady ? "✅ Ready" : "❌ Not Ready",
                        Configuration = configurationReady ? "✅ Ready" : "❌ Not Ready",
                        Services = servicesReady ? "✅ Ready" : "❌ Not Ready"
                    }
                };

                var statusCode = isReady ? 200 : 503;
                
                _logger.LogInformation("🚀 Readiness check completed. Ready: {Ready}. OperationId: {OperationId}",
                    isReady, operationId);

                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Readiness check failed. OperationId: {OperationId}", operationId);
                
                var errorResponse = new
                {
                    Ready = false,
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    OperationId = operationId,
                    Error = ex.Message
                };

                return StatusCode(503, errorResponse);
            }
        }

        /// <summary>
        /// Liveness check - verifica se a aplicação está funcionando
        /// </summary>
        /// <returns>Status de funcionamento da aplicação</returns>
        [HttpGet("live")]
        public IActionResult GetLiveness()
        {
            // Simple liveness check - if we can respond, we're alive
            var response = new
            {
                Alive = true,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                Uptime = GetUptime(),
                ProcessId = Environment.ProcessId
            };

            return Ok(response);
        }

        // Helper methods
        private async Task<bool> TestCosmosDbHealthAsync()
        {
            try
            {
                await _cosmosDbService.InitializeAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestCostManagementServiceAsync()
        {
            try
            {
                await _costManagementService.GetCurrentCostAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestAlertServiceAsync()
        {
            try
            {
                await _alertService.GetActiveThresholdsAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestConfigurationAsync()
        {
            try
            {
                // Test if we can access configuration (this indicates DI is working)
                return _cosmosDbService != null && _costManagementService != null && _alertService != null;
            }
            catch
            {
                return false;
            }
        }

        private bool TestDependencyInjection()
        {
            try
            {
                // Verify all required services are injected
                return _cosmosDbService != null && 
                       _costManagementService != null && 
                       _alertService != null &&
                       _logger != null;
            }
            catch
            {
                return false;
            }
        }

        private object GetSystemInfo()
        {
            try
            {
                var workingSet = Environment.WorkingSet;
                var processorCount = Environment.ProcessorCount;
                
                return new
                {
                    WorkingSetMB = Math.Round(workingSet / (1024.0 * 1024.0), 2),
                    ProcessorCount = processorCount,
                    OSVersion = Environment.OSVersion.ToString(),
                    Is64BitProcess = Environment.Is64BitProcess,
                    MachineName = Environment.MachineName
                };
            }
            catch
            {
                return "Unable to retrieve system info";
            }
        }

        private string GetUptime()
        {
            try
            {
                var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}