using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Services.Implementation;

namespace AzureSmartCost.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly IAzureCostImportService _importService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        IAzureCostImportService importService,
        ILogger<ImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Importa custos de uma subscription específica
    /// </summary>
    [HttpPost("subscription/{subscriptionId}")]
    public async Task<ActionResult<CostImportResult>> ImportSubscription(
        string subscriptionId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var tenantId = User.Claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized("TenantId não encontrado no token");
            }

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            _logger.LogInformation(
                "Iniciando importação para subscription {SubscriptionId} por tenant {TenantId}",
                subscriptionId, tenantId);

            var result = await _importService.ImportCostsForSubscriptionAsync(
                tenantId, subscriptionId, start, end);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acesso negado à subscription {SubscriptionId}", subscriptionId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao importar subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new CostImportResult
            {
                Success = false,
                Message = "Erro interno ao importar custos",
                ErrorDetails = ex.Message
            });
        }
    }

    /// <summary>
    /// Importa custos de todas as subscriptions ativas do tenant
    /// </summary>
    [HttpPost("all")]
    public async Task<ActionResult<CostImportResult>> ImportAllSubscriptions(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var tenantId = User.Claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized("TenantId não encontrado no token");
            }

            _logger.LogInformation(
                "Iniciando importação em lote para tenant {TenantId}",
                tenantId);

            var result = await _importService.ImportCostsForAllSubscriptionsAsync(
                tenantId, startDate, endDate);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao importar todas subscriptions");
            return StatusCode(500, new CostImportResult
            {
                Success = false,
                Message = "Erro interno ao importar custos",
                ErrorDetails = ex.Message
            });
        }
    }

    /// <summary>
    /// Lista subscriptions Azure disponíveis para o tenant
    /// </summary>
    [HttpGet("available-subscriptions")]
    public async Task<ActionResult<List<string>>> GetAvailableSubscriptions()
    {
        try
        {
            var tenantId = User.Claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized("TenantId não encontrado no token");
            }

            var subscriptions = await _importService.GetAvailableSubscriptionsAsync(tenantId);
            return Ok(subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar subscriptions disponíveis");
            return StatusCode(500, "Erro ao listar subscriptions");
        }
    }

    /// <summary>
    /// Verifica status da última importação
    /// </summary>
    [HttpGet("status")]
    public ActionResult<object> GetImportStatus()
    {
        // TODO: Implementar tracking de import jobs
        return Ok(new
        {
            Status = "Implementação pendente",
            Message = "Funcionalidade de tracking será adicionada em breve"
        });
    }
}
