using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AzureSmartCost.Functions
{
    public class GetPowerBiConfig
    {
        private readonly ILogger _logger;

        public GetPowerBiConfig(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetPowerBiConfig>();
        }

        [Function("GetPowerBiConfig")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "powerbi/embed-config")] HttpRequestData req)
        {
            _logger.LogInformation("📊 Power BI embed config request received");

            try
            {
                // Get query parameters
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var reportType = query["reportType"] ?? "executive";

                // Mock Power BI embed configuration
                var embedConfig = new
                {
                    success = true,
                    reportId = reportType switch
                    {
                        "executive" => "mock-executive-dashboard-id",
                        "detailed" => "mock-detailed-analysis-id",
                        "optimization" => "mock-cost-optimization-id",
                        "budget" => "mock-budget-analysis-id",
                        _ => "mock-executive-dashboard-id"
                    },
                    embedUrl = $"https://app.powerbi.com/reportEmbed?reportId=mock-{reportType}-id",
                    accessToken = "mock-token-for-demo-purposes",
                    tokenType = "Embed",
                    tokenExpiry = DateTime.UtcNow.AddHours(1).ToString("o"),
                    workspaceId = "mock-workspace-id",
                    message = "⚠️ Power BI Demo Mode - Configure Azure AD App Registration para modo produção",
                    configured = false,
                    demoMode = true,
                    instructions = new
                    {
                        step1 = "Criar Azure AD App Registration",
                        step2 = "Adicionar permissões Power BI Service",
                        step3 = "Criar Power BI Workspace",
                        step4 = "Configurar variáveis: POWERBI_CLIENT_ID, POWERBI_CLIENT_SECRET, POWERBI_WORKSPACE_ID",
                        documentation = "Veja POWERBI_SETUP.md para instruções completas"
                    }
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                await response.WriteStringAsync(JsonSerializer.Serialize(embedConfig, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating Power BI embed config");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    error = ex.Message
                }));
                return errorResponse;
            }
        }

        [Function("GetPowerBiTemplates")]
        public async Task<HttpResponseData> GetTemplates(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "powerbi/templates")] HttpRequestData req)
        {
            _logger.LogInformation("📋 Power BI templates request received");

            var templates = new
            {
                success = true,
                templates = new[]
                {
                    new
                    {
                        id = "executive-dashboard",
                        name = "Executive Dashboard",
                        description = "Visão geral executiva de custos e métricas principais",
                        reportType = "executive",
                        icon = "📊"
                    },
                    new
                    {
                        id = "detailed-analysis",
                        name = "Detailed Cost Analysis",
                        description = "Análise detalhada de custos por serviço e recurso",
                        reportType = "detailed",
                        icon = "📈"
                    },
                    new
                    {
                        id = "cost-optimization",
                        name = "Cost Optimization",
                        description = "Recomendações de otimização e economia de custos",
                        reportType = "optimization",
                        icon = "💰"
                    },
                    new
                    {
                        id = "budget-analysis",
                        name = "Budget Analysis",
                        description = "Análise orçamentária e previsões de gastos",
                        reportType = "budget",
                        icon = "🎯"
                    }
                },
                demoMode = true,
                message = "Modo demo - Configure Power BI para relatórios reais"
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(templates, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));

            return response;
        }
    }
}
