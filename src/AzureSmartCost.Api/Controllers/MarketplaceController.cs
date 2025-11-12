using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _marketplaceService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<MarketplaceController> _logger;

    public MarketplaceController(
        IMarketplaceService marketplaceService,
        ITenantService tenantService,
        ILogger<MarketplaceController> logger)
    {
        _marketplaceService = marketplaceService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Landing page endpoint - receives token from Azure Marketplace after purchase
    /// User is redirected here when they click "Configure Account" in Azure Portal
    /// </summary>
    /// <param name="token">Marketplace purchase token</param>
    [HttpGet("landing")]
    [AllowAnonymous]
    public async Task<IActionResult> Landing([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { error = "Token is required" });
            }

            _logger.LogInformation("Marketplace landing page accessed with token");

            // 1. Resolve the token to get subscription details
            var resolved = await _marketplaceService.ResolveSubscriptionAsync(token);

            // 2. Check if subscription already exists
            var existingSubscription = await _marketplaceService.GetMarketplaceSubscriptionByIdAsync(resolved.Id);

            if (existingSubscription != null)
            {
                // Subscription already processed, redirect to dashboard
                _logger.LogInformation("Existing marketplace subscription {SubscriptionId} found, redirecting to dashboard", 
                    resolved.Id);
                
                var dashboardUrl = $"/dashboard?tenantId={existingSubscription.TenantId}";
                return Redirect(dashboardUrl);
            }

            // 3. Create or find tenant
            Tenant? tenant = null;
            var purchaserEmail = resolved.Purchaser?.EmailId ?? resolved.Subscription?.Purchaser?.EmailId ?? "";

            if (!string.IsNullOrEmpty(purchaserEmail))
            {
                // Try to find existing tenant by email
                var allTenants = await _tenantService.GetAllTenantsAsync();
                tenant = allTenants.FirstOrDefault(t => t.ContactEmail == purchaserEmail);
            }

            if (tenant == null)
            {
                // Create new tenant
                var tier = MapPlanToTier(resolved.PlanId);
                
                tenant = new Tenant
                {
                    Name = resolved.SubscriptionName,
                    CompanyName = resolved.SubscriptionName,
                    ContactEmail = purchaserEmail,
                    SubscriptionTier = tier,
                    IsActive = true,
                    IsTrialActive = false, // Marketplace purchase, not a trial
                    AzureTenantId = resolved.Purchaser?.TenantId ?? resolved.Subscription?.Purchaser?.TenantId
                };

                tenant = await _tenantService.CreateTenantAsync(tenant);
                
                _logger.LogInformation("Created new tenant {TenantId} for marketplace subscription {SubscriptionId}", 
                    tenant.Id, resolved.Id);
            }

            // 4. Create marketplace subscription record
            var marketplaceSubscription = new MarketplaceSubscription
            {
                MarketplaceSubscriptionId = resolved.Id,
                MarketplaceSubscriptionName = resolved.SubscriptionName,
                TenantId = tenant.Id,
                PurchaserEmail = purchaserEmail,
                PurchaserTenantId = resolved.Purchaser?.TenantId ?? resolved.Subscription?.Purchaser?.TenantId ?? "",
                OfferId = resolved.OfferId,
                PlanId = resolved.PlanId,
                Quantity = resolved.Quantity,
                Status = "PendingFulfillmentStart"
            };

            await _marketplaceService.SaveMarketplaceSubscriptionAsync(marketplaceSubscription);

            // 5. Activate the subscription in Marketplace
            var activated = await _marketplaceService.ActivateSubscriptionAsync(
                resolved.Id, 
                resolved.PlanId, 
                resolved.Quantity);

            if (activated)
            {
                marketplaceSubscription.Status = "Subscribed";
                marketplaceSubscription.ActivatedAt = DateTime.UtcNow;
                await _marketplaceService.SaveMarketplaceSubscriptionAsync(marketplaceSubscription);
                
                _logger.LogInformation("Activated marketplace subscription {SubscriptionId} for tenant {TenantId}", 
                    resolved.Id, tenant.Id);
            }

            // 6. Redirect to dashboard with onboarding
            var redirectUrl = $"/dashboard?tenantId={tenant.Id}&onboarding=true&source=marketplace";
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing marketplace landing page");
            return StatusCode(500, new { error = "Failed to process marketplace purchase", details = ex.Message });
        }
    }

    /// <summary>
    /// Webhook endpoint - receives lifecycle events from Azure Marketplace
    /// Events: Subscribe, Unsubscribe, ChangePlan, ChangeQuantity, Suspend, Reinstate
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] MarketplaceWebhookEvent webhookEvent)
    {
        try
        {
            if (webhookEvent == null)
            {
                return BadRequest(new { error = "Webhook event is required" });
            }

            _logger.LogInformation("Received marketplace webhook: {Action} for subscription {SubscriptionId}", 
                webhookEvent.Action, webhookEvent.SubscriptionId);

            // Process the webhook event
            await _marketplaceService.ProcessWebhookEventAsync(webhookEvent);

            // Marketplace expects 200 OK
            return Ok(new { status = "processed", eventId = webhookEvent.EventId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing marketplace webhook");
            
            // Still return 200 to avoid retries, but log the error
            return Ok(new { status = "error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get marketplace subscription details for a tenant
    /// </summary>
    [HttpGet("subscription/{tenantId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MarketplaceSubscription>>> GetSubscription(string tenantId)
    {
        try
        {
            var subscription = await _marketplaceService.GetMarketplaceSubscriptionByTenantIdAsync(tenantId);

            if (subscription == null)
            {
                return NotFound(new ApiResponse<MarketplaceSubscription>
                {
                    Success = false,
                    Message = "Marketplace subscription not found"
                });
            }

            // Get latest details from Marketplace API
            if (!string.IsNullOrEmpty(subscription.MarketplaceSubscriptionId))
            {
                var details = await _marketplaceService.GetSubscriptionAsync(subscription.MarketplaceSubscriptionId);
                
                if (details != null)
                {
                    subscription.SaasSubscriptionStatus = details.SaasSubscriptionStatus;
                }
            }

            return Ok(new ApiResponse<MarketplaceSubscription>
            {
                Success = true,
                Data = subscription
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting marketplace subscription for tenant {TenantId}", tenantId);
            
            return StatusCode(500, new ApiResponse<MarketplaceSubscription>
            {
                Success = false,
                Message = "Failed to get marketplace subscription"
            });
        }
    }

    /// <summary>
    /// List all marketplace subscriptions (admin only)
    /// </summary>
    [HttpGet("subscriptions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<SubscriptionDetails>>>> ListSubscriptions()
    {
        try
        {
            var subscriptions = await _marketplaceService.ListSubscriptionsAsync();

            return Ok(new ApiResponse<List<SubscriptionDetails>>
            {
                Success = true,
                Data = subscriptions,
                Message = $"Found {subscriptions.Count} marketplace subscriptions"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing marketplace subscriptions");
            
            return StatusCode(500, new ApiResponse<List<SubscriptionDetails>>
            {
                Success = false,
                Message = "Failed to list marketplace subscriptions"
            });
        }
    }

    /// <summary>
    /// Test endpoint to verify marketplace configuration
    /// </summary>
    [HttpGet("test")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TestConfiguration()
    {
        try
        {
            // Try to get access token
            var token = await _marketplaceService.GetMarketplaceAccessTokenAsync();
            
            var isConfigured = !string.IsNullOrEmpty(token);

            return Ok(new
            {
                configured = isConfigured,
                hasToken = token?.Length > 0,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                configured = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    private string MapPlanToTier(string planId)
    {
        return planId.ToLower() switch
        {
            "free" => "Free",
            "pro" or "professional" => "Pro",
            "enterprise" => "Enterprise",
            _ => "Free"
        };
    }
}
