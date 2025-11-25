using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AzureSmartCost.Functions.Services;

namespace AzureSmartCost.Functions;

public class TestEmailAlert
{
    private readonly ILogger _logger;
    private readonly IEmailService _emailService;

    public TestEmailAlert(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TestEmailAlert>();
        _emailService = new EmailService(_logger);
    }

    /// <summary>
    /// Endpoint para testar envio de email de alerta manualmente
    /// POST /api/test-email
    /// Body: { "email": "seu@email.com", "alertName": "Teste", "budget": 1000, "currentSpend": 850, "threshold": 80 }
    /// </summary>
    [Function("TestEmailAlert")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "test-email")] HttpRequestData req)
    {
        _logger.LogInformation("🧪 Test Email Alert endpoint chamado");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var testData = JsonConvert.DeserializeObject<TestEmailRequest>(requestBody);

            if (testData == null || string.IsNullOrEmpty(testData.Email))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new 
                { 
                    success = false, 
                    message = "Email é obrigatório",
                    example = new
                    {
                        email = "seu@email.com",
                        alertName = "Orçamento Azure Produção",
                        budget = 1000,
                        currentSpend = 850,
                        threshold = 80
                    }
                });
                return badResponse;
            }

            // Valores padrão para teste
            var alertName = testData.AlertName ?? "Alerta de Orçamento - Teste";
            var budget = testData.Budget > 0 ? testData.Budget : 1000;
            var currentSpend = testData.CurrentSpend >= 0 ? testData.CurrentSpend : 850;
            var threshold = testData.Threshold > 0 ? testData.Threshold : 80;

            _logger.LogInformation("📧 Enviando email de teste para: {Email}", testData.Email);
            _logger.LogInformation("   Alert: {Name}, Budget: R$ {Budget:N2}, Spend: R$ {Spend:N2}, Threshold: {Threshold}%",
                alertName, budget, currentSpend, threshold);

            // Enviar email
            await _emailService.SendBudgetAlertEmailAsync(
                testData.Email,
                alertName,
                budget,
                currentSpend,
                threshold
            );

            var percentage = (currentSpend / budget) * 100;

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = "Email de teste enviado com sucesso!",
                details = new
                {
                    to = testData.Email,
                    alertName,
                    budget = $"R$ {budget:N2}",
                    currentSpend = $"R$ {currentSpend:N2}",
                    percentage = $"{percentage:F1}%",
                    threshold = $"{threshold}%",
                    status = percentage >= threshold ? "ALERTA ATIVADO" : "Abaixo do threshold"
                },
                configuration = new
                {
                    sendGridConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SENDGRID_API_KEY")),
                    smtpConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SMTP_HOST")),
                    fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? "noreply@azuresmartcost.com",
                    fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? "Azure SmartCost"
                }
            });

            _logger.LogInformation("✅ Email de teste enviado com sucesso para {Email}", testData.Email);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao enviar email de teste");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                success = false,
                message = "Erro ao enviar email",
                error = ex.Message,
                configuration = new
                {
                    sendGridConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SENDGRID_API_KEY")),
                    smtpConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SMTP_HOST"))
                }
            });
            
            return errorResponse;
        }
    }
}

public class TestEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string? AlertName { get; set; }
    public decimal Budget { get; set; }
    public decimal CurrentSpend { get; set; }
    public int Threshold { get; set; }
}
