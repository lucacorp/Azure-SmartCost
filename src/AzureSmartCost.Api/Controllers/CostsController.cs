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
    public class CostsController : ControllerBase
    {
        private readonly ILogger<CostsController> _logger;
        private readonly CosmosDbService _cosmosDbService;
        private readonly ICostManagementService _costService;

        public CostsController(ILogger<CostsController> logger, 
            CosmosDbService cosmosDbService, 
            ICostManagementService costService)
        {
            _logger = logger;
            _cosmosDbService = cosmosDbService;
            _costService = costService;
        }

        /// <summary>
        /// Obter dados de custo em tempo real
        /// </summary>
        /// <returns>Lista de registros de custo mais recentes</returns>
        [HttpGet("current")]
        [Authorize(Policy = "CanReadCosts")]
        public async Task<ActionResult<ApiResponse<List<CostRecord>>>> GetCurrentCosts()
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("📊 API request: Get current costs. OperationId: {OperationId}", operationId);

            try
            {
                var response = await _costService.GetDailyCostAsync();
                
                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("✅ Successfully retrieved {RecordCount} current cost records via API. OperationId: {OperationId}", 
                        response.Data.Count, operationId);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving current costs via API. OperationId: {OperationId}", operationId);
                return StatusCode(500, ApiResponse<List<CostRecord>>.CreateError("Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Obter histórico de custos armazenado no Cosmos DB
        /// </summary>
        /// <param name="startDate">Data de início (formato: yyyy-MM-dd)</param>
        /// <param name="endDate">Data de fim (formato: yyyy-MM-dd)</param>
        /// <param name="serviceType">Filtro por tipo de serviço (opcional)</param>
        /// <param name="resourceGroup">Filtro por resource group (opcional)</param>
        /// <returns>Lista de registros históricos</returns>
        [HttpGet("history")]
        [Authorize(Policy = "CanReadCosts")]
        public async Task<ActionResult<ApiResponse<List<CostRecord>>>> GetCostHistory(
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            [FromQuery] string? serviceType = null,
            [FromQuery] string? resourceGroup = null)
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("📈 API request: Get cost history. StartDate: {StartDate}, EndDate: {EndDate}, Service: {ServiceType}, RG: {ResourceGroup}. OperationId: {OperationId}", 
                startDate, endDate, serviceType, resourceGroup, operationId);

            try
            {
                // Parse dates or use defaults
                var start = DateTime.TryParse(startDate, out var startParsed) ? startParsed : DateTime.UtcNow.AddDays(-30);
                var end = DateTime.TryParse(endDate, out var endParsed) ? endParsed : DateTime.UtcNow;

                await _cosmosDbService.InitializeAsync();

                var records = await _cosmosDbService.GetCostRecordsByDateRangeAsync(start, end);

                // Apply filters
                if (!string.IsNullOrEmpty(serviceType))
                {
                    records = records.Where(r => r.ServiceName.Contains(serviceType, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(resourceGroup))
                {
                    records = records.Where(r => r.ResourceGroup.Contains(resourceGroup, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                _logger.LogInformation("✅ Successfully retrieved {RecordCount} historical cost records via API. OperationId: {OperationId}", 
                    records.Count, operationId);

                return Ok(ApiResponse<List<CostRecord>>.CreateSuccess(records, 
                    $"Retrieved {records.Count} cost records from {start:yyyy-MM-dd} to {end:yyyy-MM-dd}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving cost history via API. OperationId: {OperationId}", operationId);
                return StatusCode(500, ApiResponse<List<CostRecord>>.CreateError("Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Obter resumo de custos por período
        /// </summary>
        /// <param name="days">Número de dias para análise (padrão: 7)</param>
        /// <returns>Resumo estatístico dos custos</returns>
        [HttpGet("summary")]
        [Authorize(Policy = "CanReadCosts")]
        public async Task<ActionResult<ApiResponse<object>>> GetCostSummary([FromQuery] int days = 7)
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("📊 API request: Get cost summary for {Days} days. OperationId: {OperationId}", days, operationId);

            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var endDate = DateTime.UtcNow;

                await _cosmosDbService.InitializeAsync();
                var records = await _cosmosDbService.GetCostRecordsByDateRangeAsync(startDate, endDate);

                if (!records.Any())
                {
                    return Ok(ApiResponse<object>.CreateSuccess(new { 
                        Message = "No cost data available for the specified period",
                        Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
                    }));
                }

                var summary = new
                {
                    Period = new
                    {
                        StartDate = startDate.ToString("yyyy-MM-dd"),
                        EndDate = endDate.ToString("yyyy-MM-dd"),
                        DaysAnalyzed = days
                    },
                    Overview = new
                    {
                        TotalCost = records.Sum(r => r.TotalCost),
                        AverageDailyCost = records.GroupBy(r => r.Date.Date).Average(g => g.Sum(r => r.TotalCost)),
                        RecordCount = records.Count,
                        Currency = records.FirstOrDefault()?.Currency ?? "USD"
                    },
                    ByService = records.GroupBy(r => r.ServiceName)
                        .Select(g => new
                        {
                            ServiceName = g.Key,
                            TotalCost = g.Sum(r => r.TotalCost),
                            RecordCount = g.Count(),
                            Percentage = Math.Round((g.Sum(r => r.TotalCost) / records.Sum(r => r.TotalCost)) * 100, 2)
                        })
                        .OrderByDescending(s => s.TotalCost)
                        .Take(10),
                    ByResourceGroup = records.GroupBy(r => r.ResourceGroup)
                        .Select(g => new
                        {
                            ResourceGroup = g.Key,
                            TotalCost = g.Sum(r => r.TotalCost),
                            RecordCount = g.Count(),
                            Percentage = Math.Round((g.Sum(r => r.TotalCost) / records.Sum(r => r.TotalCost)) * 100, 2)
                        })
                        .OrderByDescending(rg => rg.TotalCost)
                        .Take(10),
                    DailyTrend = records.GroupBy(r => r.Date.Date)
                        .Select(g => new
                        {
                            Date = g.Key.ToString("yyyy-MM-dd"),
                            TotalCost = g.Sum(r => r.TotalCost),
                            RecordCount = g.Count()
                        })
                        .OrderBy(d => d.Date)
                };

                _logger.LogInformation("✅ Successfully generated cost summary for {Days} days. Total: ${TotalCost:F2}. OperationId: {OperationId}", 
                    days, summary.Overview.TotalCost, operationId);

                return Ok(ApiResponse<object>.CreateSuccess(summary, $"Cost summary for {days} days generated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating cost summary via API. OperationId: {OperationId}", operationId);
                return StatusCode(500, ApiResponse<object>.CreateError("Internal server error", ex.Message));
            }
        }
    }
}