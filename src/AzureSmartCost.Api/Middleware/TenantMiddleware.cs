using System.Security.Claims;
using AzureSmartCost.Shared.Interfaces;

namespace AzureSmartCost.Api.Middleware;

/// <summary>
/// Middleware para extrair TenantId do JWT token e popular o TenantContext
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        try
        {
            // Extrair claims do JWT token
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var claims = context.User.Claims;
                
                tenantContext.TenantId = claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
                tenantContext.UserId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                tenantContext.UserEmail = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                tenantContext.UserRole = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                _logger.LogInformation(
                    "Tenant context set: TenantId={TenantId}, UserId={UserId}, Role={Role}",
                    tenantContext.TenantId,
                    tenantContext.UserId,
                    tenantContext.UserRole
                );
            }
            else
            {
                // Tentar obter TenantId do header (para chamadas de API pública)
                var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                if (!string.IsNullOrEmpty(tenantIdHeader))
                {
                    tenantContext.TenantId = tenantIdHeader;
                    _logger.LogInformation("Tenant context set from header: TenantId={TenantId}", tenantIdHeader);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting tenant context");
        }

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
