using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Functions.Services;
using Newtonsoft.Json;

namespace AzureSmartCost.Functions;

public class GetCosts
{
    private readonly ILogger _logger;

    public GetCosts(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GetCosts>();
    }

    [Function("GetCosts")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "costs/{subscriptionId?}")] HttpRequestData req,
        string? subscriptionId = null)
    {
        _logger.LogInformation("GetCosts endpoint chamado");

        try
        {
            // Se não passar subscriptionId na URL, pegar do query string
            if (string.IsNullOrEmpty(subscriptionId))
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                subscriptionId = query["subscriptionId"];
            }

            // Se ainda estiver vazio, usar a subscription configurada
            if (string.IsNullOrEmpty(subscriptionId))
            {
                subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            }

            if (string.IsNullOrEmpty(subscriptionId))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "subscriptionId é obrigatório (via URL, query string ou env variable)"
                });
                return errorResponse;
            }

            // Buscar custos (com cache automático)
            var costService = new CostService(_logger);
            var costs = await costService.GetCostsAsync(subscriptionId);

            // Retornar sucesso
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                data = costs,
                cached = costs.CachedAt > DateTime.UtcNow.AddMinutes(-5), // Se foi cacheado nos últimos 5min
                cacheAge = (int)(DateTime.UtcNow - costs.CachedAt).TotalMinutes
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar custos");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                success = false,
                message = "Erro ao buscar custos: " + ex.Message,
                error = ex.ToString()
            });

            return errorResponse;
        }
    }
}
