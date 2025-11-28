using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using AzureSmartCost.Functions.Models;
using AzureSmartCost.Functions.Services;

namespace AzureSmartCost.Functions;

public class BudgetAlertChecker
{
    private readonly ILogger _logger;
    private readonly Container _alertsContainer;
    private readonly IEmailService _emailService;
    private readonly CostService _costService;

    public BudgetAlertChecker(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<BudgetAlertChecker>();
        _emailService = new EmailService(_logger);
        _costService = new CostService(_logger);
        
        var cosmosEndpoint = Environment.GetEnvironmentVariable("CosmosDb__Endpoint");
        var cosmosKey = Environment.GetEnvironmentVariable("CosmosDb__Key");
        
        if (!string.IsNullOrEmpty(cosmosEndpoint) && !string.IsNullOrEmpty(cosmosKey))
        {
            var cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey);
            _alertsContainer = cosmosClient.GetContainer("SmartCost", "BudgetAlerts");
        }
    }

    /// <summary>
    /// Timer que executa a cada hora para verificar alertas de budget
    /// NCRONTAB format: {second} {minute} {hour} {day} {month} {day-of-week}
    /// 0 0 * * * * = A cada hora no minuto 0
    /// </summary>
    [Function("BudgetAlertChecker")]
    public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("🔍 Budget Alert Checker iniciado em: {Time}", DateTime.Now);

        try
        {
            // Buscar todos os alertas ativos
            var query = new QueryDefinition("SELECT * FROM c WHERE c.type = 'alert' AND c.isActive = true");
            
            var alerts = new List<BudgetAlert>();
            using var iterator = _alertsContainer.GetItemQueryIterator<BudgetAlert>(query);
            
            while (iterator.HasMoreResults)
            {
                var result = await iterator.ReadNextAsync();
                alerts.AddRange(result);
            }

            _logger.LogInformation("📊 Encontrados {Count} alertas ativos para verificar", alerts.Count);

            if (alerts.Count == 0)
            {
                _logger.LogInformation("✅ Nenhum alerta ativo para processar");
                return;
            }

            // Agrupar por subscription para otimizar chamadas à API
            var alertsBySubscription = alerts.GroupBy(a => a.SubscriptionId);

            int emailsSent = 0;
            int alertsChecked = 0;

            foreach (var subscriptionGroup in alertsBySubscription)
            {
                try
                {
                    var subscriptionId = subscriptionGroup.Key;
                    
                    // Buscar custos atuais do mês (primeiro dia do mês até hoje)
                    var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    var endDate = DateTime.Now;
                    var costData = await _costService.GetCostsAsync(subscriptionId, startDate, endDate);
                    var totalSpend = costData?.TotalCost ?? 0;

                    _logger.LogInformation("💰 Subscription {SubId}: Gasto atual R$ {Spend:N2}", 
                        subscriptionId.Substring(0, 8) + "...", totalSpend);

                    foreach (var alert in subscriptionGroup)
                    {
                        alertsChecked++;
                        
                        // Atualizar gasto atual do alerta
                        alert.CurrentSpend = totalSpend;
                        alert.LastCheckedAt = DateTime.UtcNow;

                        var percentage = totalSpend > 0 ? (totalSpend / alert.Amount) * 100 : 0;

                        _logger.LogInformation("   📌 {AlertName}: {Percentage:F1}% ({Current:N2}/{Budget:N2}) - Threshold: {Threshold}%",
                            alert.Name, percentage, totalSpend, alert.Amount, alert.Threshold);

                        // Verificar se deve enviar email
                        if (percentage >= alert.Threshold)
                        {
                            var lastSent = alert.LastNotificationSent ?? DateTime.MinValue;
                            var hoursSinceLastEmail = (DateTime.UtcNow - lastSent).TotalHours;

                            // Enviar no máximo 1 email por dia (24h)
                            if (hoursSinceLastEmail >= 24)
                            {
                                try
                                {
                                    await _emailService.SendBudgetAlertEmailAsync(
                                        alert.Email,
                                        alert.Name,
                                        alert.Amount,
                                        totalSpend,
                                        alert.Threshold
                                    );

                                    alert.LastNotificationSent = DateTime.UtcNow;
                                    emailsSent++;
                                    
                                    _logger.LogInformation("      ✅ Email enviado para {Email}", alert.Email);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "      ❌ Erro ao enviar email para {Email}", alert.Email);
                                }
                            }
                            else
                            {
                                var hoursRemaining = 24 - (int)hoursSinceLastEmail;
                                _logger.LogInformation("      ⏭️ Email já enviado há {Hours}h. Próximo em {Remaining}h", 
                                    (int)hoursSinceLastEmail, hoursRemaining);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("      ✔️ Abaixo do threshold ({Percentage:F1}% < {Threshold}%)", 
                                percentage, alert.Threshold);
                        }

                        // Atualizar alerta no Cosmos
                        try
                        {
                            await _alertsContainer.UpsertItemAsync(alert, new PartitionKey(alert.Id));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao atualizar alerta {AlertId}", alert.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar subscription {SubId}", subscriptionGroup.Key);
                }
            }

            _logger.LogInformation("✅ Verificação completa: {Checked} alertas verificados, {Sent} emails enviados", 
                alertsChecked, emailsSent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro crítico no Budget Alert Checker");
        }

        _logger.LogInformation("🏁 Budget Alert Checker finalizado. Próxima execução em 1 hora");
    }
}
