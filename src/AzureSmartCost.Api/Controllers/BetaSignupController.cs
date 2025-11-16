using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace AzureSmartCost.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BetaSignupController : ControllerBase
{
    private readonly ILogger<BetaSignupController> _logger;
    // TODO: Add repository/service for storing beta signups

    public BetaSignupController(ILogger<BetaSignupController> logger)
    {
        _logger = logger;
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

            // TODO: Save to Cosmos DB
            // await _betaSignupRepository.CreateAsync(signup);

            // TODO: Send welcome email
            // await _emailService.SendBetaWelcomeEmailAsync(signup.Email, signup.Name);

            // TODO: Add to Discord/Slack
            // await _discordService.NotifyNewBetaSignupAsync(signup);

            _logger.LogInformation("Inscrição beta processada com sucesso: {Id}", signup.Id);

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
            // TODO: Get real count from database
            var count = 8; // Placeholder
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
