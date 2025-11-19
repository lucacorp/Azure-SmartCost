using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureSmartCost.Functions
{
    public class BetaSignup
    {
        private readonly ILogger<BetaSignup> _logger;

        public BetaSignup(ILogger<BetaSignup> logger)
        {
            _logger = logger;
        }

        [Function("BetaSignup")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "betasignup")] HttpRequestData req)
        {
            _logger.LogInformation("🚀 Nova requisição de signup recebida");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                // Validação básica
                if (data == null || string.IsNullOrEmpty((string)data.email) || string.IsNullOrEmpty((string)data.name))
                {
                    _logger.LogWarning("❌ Requisição inválida - campos obrigatórios faltando");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Nome e email são obrigatórios"
                    });
                    return badResponse;
                }

                // Criar objeto de signup
                var signupId = Guid.NewGuid().ToString();
                var signup = new
                {
                    id = signupId,
                    name = (string)data.name,
                    email = ((string)data.email).ToLowerInvariant(),
                    company = (string)data.company,
                    role = (string)data.role,
                    azureSpend = (string)data.azureSpend,
                    challenge = (string)data.challenge,
                    signupDate = DateTime.UtcNow,
                    status = "pending",
                    source = "landing-page"
                };

                // LOG COMPLETO no Application Insights
                // TODO: Adicionar Cosmos DB após evento (workaround para runtimes folder issue)
                _logger.LogWarning("✅ BETA SIGNUP RECEBIDO: " +
                    $"ID={signup.id} | " +
                    $"Nome={signup.name} | " +
                    $"Email={signup.email} | " +
                    $"Empresa={signup.company} | " +
                    $"Cargo={signup.role} | " +
                    $"Gasto Azure={signup.azureSpend} | " +
                    $"Desafio={signup.challenge} | " +
                    $"Data={signup.signupDate:yyyy-MM-dd HH:mm:ss}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "Inscrição realizada com sucesso! Entraremos em contato em breve.",
                    signupId = signup.id
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar signup");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { success = false, message = "Erro interno" });
                return errorResponse;
            }
        }
    }
}
