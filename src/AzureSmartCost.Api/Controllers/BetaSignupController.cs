using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace AzureSmartCost.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BetaSignupController : ControllerBase
{
    private readonly ILogger<BetaSignupController> _logger;
    private readonly IConfiguration _configuration;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public BetaSignupController(
        ILogger<BetaSignupController> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        // Cosmos DB desabilitado temporariamente devido a problema com runtimes
        // _cosmosClient = cosmosClient;
        // _container = cosmosClient.GetContainer("SmartCostDB", "BetaSignups");
    }

    [HttpPost]
    public async Task<IActionResult> SignUp([FromBody] BetaSignupRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Nova inscrição beta recebida: {Email}", request.Email);

            var signup = new BetaSignup
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Email = request.Email.ToLowerInvariant(),
                Company = request.Company,
                Role = request.Role,
                AzureSpend = request.AzureSpend,
                Challenge = request.Challenge,
                SignupDate = DateTime.UtcNow,
                Status = "pending",
                Source = "landing-page"
            };

            // Salvar no Cosmos DB - DESABILITADO TEMPORARIAMENTE
            // TODO: Reabilitar quando resolver problema com runtimes no deploy
            /*
            try
            {
                await _container.CreateItemAsync(signup, new PartitionKey(signup.Email));
                _logger.LogInformation("Beta signup salvo com sucesso: {Id} - {Name} ({Email})", 
                    signup.Id, signup.Name, signup.Email);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Erro ao salvar no Cosmos DB - continuando");
                // Não retornar erro para o usuário se salvar falhar
            }
            */
            
            // Log temporário enquanto Cosmos está desabilitado
            _logger.LogWarning("BETA SIGNUP (não salvo no DB): {Id} - {Name} ({Email}) - {Company} - {Role}", 
                signup.Id, signup.Name, signup.Email, signup.Company, signup.Role);

            return Ok(new
            {
                success = true,
                message = "Inscrição realizada com sucesso! Verifique seu email.",
                signupId = signup.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar inscrição beta");
            return StatusCode(500, new
            {
                success = false,
                message = "Erro ao processar inscrição. Tente novamente."
            });
        }
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetSignupCount()
    {
        try
        {
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            var iterator = _container.GetItemQueryIterator<int>(query);
            var response = await iterator.ReadNextAsync();
            var count = response.FirstOrDefault();
            var remaining = Math.Max(0, 50 - count);

            return Ok(new
            {
                total = 50,
                signups = count,
                remaining = remaining
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter contagem de inscrições");
            return StatusCode(500);
        }
    }
}

public class BetaSignupRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Company { get; set; }

    [Required(ErrorMessage = "Cargo é obrigatório")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gasto Azure é obrigatório")]
    public string AzureSpend { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Challenge { get; set; }

    public bool Terms { get; set; } = true;
}

public class BetaSignup
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string Role { get; set; } = string.Empty;
    public string AzureSpend { get; set; } = string.Empty;
    public string? Challenge { get; set; }
    public DateTime SignupDate { get; set; }
    public string Status { get; set; } = "pending"; // pending, approved, activated
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
