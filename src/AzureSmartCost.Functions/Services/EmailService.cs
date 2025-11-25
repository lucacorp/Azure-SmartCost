using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AzureSmartCost.Functions.Services;

public interface IEmailService
{
    Task SendBudgetAlertEmailAsync(string toEmail, string alertName, decimal budget, decimal currentSpend, int threshold);
}

public class EmailService : IEmailService
{
    private readonly ILogger _logger;
    private readonly string _sendGridApiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(ILogger logger)
    {
        _logger = logger;
        _sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? "";
        _fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? "noreply@azuresmartcost.com";
        _fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? "Azure SmartCost";
    }

    public async Task SendBudgetAlertEmailAsync(
        string toEmail, 
        string alertName, 
        decimal budget, 
        decimal currentSpend, 
        int threshold)
    {
        try
        {
            if (string.IsNullOrEmpty(_sendGridApiKey))
            {
                _logger.LogWarning("SendGrid API Key não configurada. Email não será enviado.");
                // Fallback para SMTP se configurado
                await SendViaSmtpAsync(toEmail, alertName, budget, currentSpend, threshold);
                return;
            }

            var percentage = (currentSpend / budget) * 100;
            
            var subject = $"⚠️ Azure SmartCost - Alerta de Orçamento: {alertName}";
            
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .alert-box {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .warning {{ background: #f8d7da; border-left: 4px solid #dc3545; }}
        .metric {{ background: white; padding: 15px; margin: 10px 0; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .metric-label {{ font-size: 12px; color: #666; text-transform: uppercase; }}
        .metric-value {{ font-size: 24px; font-weight: bold; color: #0078d4; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .button {{ display: inline-block; padding: 12px 24px; background: #0078d4; color: white; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔔 Alerta de Orçamento</h1>
            <p>Azure SmartCost</p>
        </div>
        <div class='content'>
            <div class='{(percentage >= threshold ? "alert-box warning" : "alert-box")}'>
                <h2>⚠️ {alertName}</h2>
                <p>Seu orçamento atingiu <strong>{percentage:F1}%</strong> do limite configurado.</p>
            </div>

            <div class='metric'>
                <div class='metric-label'>Orçamento Total</div>
                <div class='metric-value'>R$ {budget:N2}</div>
            </div>

            <div class='metric'>
                <div class='metric-label'>Gasto Atual</div>
                <div class='metric-value' style='color: {(percentage >= threshold ? "#dc3545" : "#28a745")}'>R$ {currentSpend:N2}</div>
            </div>

            <div class='metric'>
                <div class='metric-label'>Percentual Utilizado</div>
                <div class='metric-value' style='color: {(percentage >= threshold ? "#dc3545" : "#28a745")}'>{percentage:F1}%</div>
            </div>

            <div class='metric'>
                <div class='metric-label'>Limite de Alerta</div>
                <div class='metric-value' style='color: #ffc107'>{threshold}%</div>
            </div>

            <div style='margin-top: 30px; padding: 20px; background: #e7f3ff; border-radius: 5px;'>
                <h3>💡 Recomendações</h3>
                <ul>
                    {(percentage >= 90 ? "<li>🚨 <strong>Atenção:</strong> Orçamento próximo do limite! Revise seus recursos urgentemente.</li>" : "")}
                    {(percentage >= 80 && percentage < 90 ? "<li>⚠️ Orçamento atingindo o limite. Considere otimizar recursos.</li>" : "")}
                    <li>📊 Acesse o dashboard para análise detalhada de custos</li>
                    <li>🔍 Identifique recursos não utilizados</li>
                    <li>💰 Revise recomendações de otimização</li>
                </ul>
            </div>

            <div style='text-align: center;'>
                <a href='https://blue-flower-0414b9b0f.3.azurestaticapps.net' class='button'>
                    📊 Acessar Dashboard
                </a>
            </div>

            <div style='margin-top: 30px; font-size: 12px; color: #666;'>
                <p><strong>Data/Hora:</strong> {DateTime.Now:dd/MM/yyyy HH:mm} (Horário de Brasília)</p>
            </div>
        </div>
        <div class='footer'>
            <p>Azure SmartCost - Plataforma FinOps Inteligente</p>
            <p>Este é um email automático. Não responda.</p>
        </div>
    </div>
</body>
</html>";

            var plainTextContent = $@"
AZURE SMARTCOST - ALERTA DE ORÇAMENTO

Alerta: {alertName}
Status: {(percentage >= threshold ? "ATENÇÃO - Limite atingido" : "Monitoramento")}

MÉTRICAS:
• Orçamento Total: R$ {budget:N2}
• Gasto Atual: R$ {currentSpend:N2}
• Percentual: {percentage:F1}%
• Limite de Alerta: {threshold}%

RECOMENDAÇÕES:
{(percentage >= 90 ? "• URGENTE: Orçamento próximo do limite!" : "")}
• Acesse o dashboard: https://blue-flower-0414b9b0f.3.azurestaticapps.net
• Revise recursos não utilizados
• Verifique recomendações de otimização

Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm}

--
Azure SmartCost
Plataforma FinOps Inteligente
";

            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            
            var response = await client.SendEmailAsync(msg);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Email de alerta enviado com sucesso para {Email}", toEmail);
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("❌ Erro ao enviar email via SendGrid: {StatusCode} - {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email de alerta para {Email}", toEmail);
            throw;
        }
    }

    private async Task SendViaSmtpAsync(string toEmail, string alertName, decimal budget, decimal currentSpend, int threshold)
    {
        try
        {
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            var smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
            var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER");
            var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
            {
                _logger.LogWarning("SMTP não configurado. Email não será enviado.");
                return;
            }

            var percentage = (currentSpend / budget) * 100;
            var subject = $"⚠️ Azure SmartCost - Alerta de Orçamento: {alertName}";
            var body = $@"
AZURE SMARTCOST - ALERTA DE ORÇAMENTO

Alerta: {alertName}
Orçamento: R$ {budget:N2}
Gasto Atual: R$ {currentSpend:N2}
Percentual: {percentage:F1}%
Limite: {threshold}%

Acesse o dashboard: https://blue-flower-0414b9b0f.3.azurestaticapps.net
";

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            mailMessage.To.Add(toEmail);

            await smtp.SendMailAsync(mailMessage);
            _logger.LogInformation("✅ Email enviado via SMTP para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao enviar email via SMTP (fallback)");
        }
    }
}
