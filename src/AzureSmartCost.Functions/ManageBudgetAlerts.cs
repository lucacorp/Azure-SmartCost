using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using AzureSmartCost.Functions.Models;
using AzureSmartCost.Functions.Services;

namespace AzureSmartCost.Functions;

public class ManageBudgetAlerts
{
    private readonly ILogger _logger;
    private readonly Container _alertsContainer;
    private readonly IEmailService _emailService;

    public ManageBudgetAlerts(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ManageBudgetAlerts>();
        _emailService = new EmailService(_logger);
        
        var cosmosEndpoint = Environment.GetEnvironmentVariable("CosmosDb__Endpoint");
        var cosmosKey = Environment.GetEnvironmentVariable("CosmosDb__Key");
        
        if (!string.IsNullOrEmpty(cosmosEndpoint) && !string.IsNullOrEmpty(cosmosKey))
        {
            var cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey);
            _alertsContainer = cosmosClient.GetContainer("SmartCost", "BudgetAlerts");
        }
    }

    [Function("GetAlerts")]
    public async Task<HttpResponseData> GetAlerts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "alerts/{subscriptionId}")] HttpRequestData req,
        string subscriptionId)
    {
        _logger.LogInformation("GetAlerts chamado para subscription {SubscriptionId}", subscriptionId);

        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.subscriptionId = @subId AND c.type = 'alert'")
                .WithParameter("@subId", subscriptionId);

            var alerts = new System.Collections.Generic.List<BudgetAlert>();
            using var iterator = _alertsContainer.GetItemQueryIterator<BudgetAlert>(query);
            
            while (iterator.HasMoreResults)
            {
                var result = await iterator.ReadNextAsync();
                alerts.AddRange(result);
            }

            // Verificar e enviar emails se threshold atingido
            await CheckAndSendAlertsAsync(alerts);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { success = true, data = alerts });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar alertas");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { success = false, message = ex.Message });
            return errorResponse;
        }
    }

    [Function("CreateAlert")]
    public async Task<HttpResponseData> CreateAlert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "alerts")] HttpRequestData req)
    {
        _logger.LogInformation("CreateAlert chamado");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("📥 Request body: {Body}", requestBody);
            
            var alert = JsonConvert.DeserializeObject<BudgetAlert>(requestBody);

            _logger.LogInformation("📦 Deserialized alert: Name={Name}, Amount={Amount}, Email={Email}, Threshold={Threshold}", 
                alert?.Name, alert?.Amount, alert?.Email, alert?.Threshold);

            if (alert == null || string.IsNullOrEmpty(alert.SubscriptionId))
            {
                _logger.LogWarning("⚠️ Dados inválidos: alert is null or subscriptionId is empty");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { success = false, message = "Dados inválidos" });
                return badResponse;
            }

            alert.Id = Guid.NewGuid().ToString();
            alert.CreatedAt = DateTime.UtcNow;
            alert.IsActive = true;
            alert.CurrentSpend = 0; // Inicializar com 0

            _logger.LogInformation("💾 Salvando alerta: {AlertId} - Name={Name}, Budget={Amount}, Email={Email}", 
                alert.Id, alert.Name, alert.Amount, alert.Email);

            await _alertsContainer.CreateItemAsync(alert, new PartitionKey(alert.SubscriptionId));

            _logger.LogInformation("✅ Alerta criado: {AlertId} - Budget R$ {Amount}", alert.Id, alert.Amount);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new { success = true, data = alert });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar alerta");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { success = false, message = ex.Message });
            return errorResponse;
        }
    }

    [Function("DeleteAlert")]
    public async Task<HttpResponseData> DeleteAlert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "alerts/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("DeleteAlert chamado para {AlertId}", id);

        try
        {
            // Primeiro buscar o alerta para obter o partition key (subscriptionId)
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);
            
            var iterator = _alertsContainer.GetItemQueryIterator<BudgetAlert>(query);
            BudgetAlert alertToDelete = null;
            
            while (iterator.HasMoreResults)
            {
                var result = await iterator.ReadNextAsync();
                alertToDelete = result.FirstOrDefault();
                break;
            }

            if (alertToDelete == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { success = false, message = "Alerta não encontrado" });
                return notFoundResponse;
            }

            await _alertsContainer.DeleteItemAsync<BudgetAlert>(id, new PartitionKey(alertToDelete.SubscriptionId));

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { success = true, message = "Alerta removido" });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar alerta");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { success = false, message = ex.Message });
            return errorResponse;
        }
    }

    private async Task CheckAndSendAlertsAsync(System.Collections.Generic.List<BudgetAlert> alerts)
    {
        foreach (var alert in alerts)
        {
            if (!alert.IsActive) continue;

            var percentage = (alert.CurrentSpend / alert.Amount) * 100;
            
            // Enviar email se atingiu o threshold E ainda não enviou hoje
            if (percentage >= alert.Threshold)
            {
                var lastSent = alert.LastNotificationSent ?? DateTime.MinValue;
                var hoursSinceLastEmail = (DateTime.UtcNow - lastSent).TotalHours;
                
                // Enviar no máximo 1 email por dia
                if (hoursSinceLastEmail >= 24)
                {
                    try
                    {
                        await _emailService.SendBudgetAlertEmailAsync(
                            alert.Email,
                            alert.Name,
                            alert.Amount,
                            alert.CurrentSpend,
                            alert.Threshold
                        );

                        // Atualizar último envio
                        alert.LastNotificationSent = DateTime.UtcNow;
                        await _alertsContainer.UpsertItemAsync(alert, new PartitionKey(alert.Id));
                        
                        _logger.LogInformation("📧 Email de alerta enviado para {Email} - {AlertName}", alert.Email, alert.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar email de alerta para {Email}", alert.Email);
                    }
                }
            }
        }
    }
}
