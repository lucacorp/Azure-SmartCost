using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ITenantService _tenantService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<BillingController> _logger;
    private readonly IConfiguration _configuration;

    public BillingController(
        IStripeService stripeService,
        ITenantService tenantService,
        ITenantContext tenantContext,
        ILogger<BillingController> logger,
        IConfiguration configuration)
    {
        _stripeService = stripeService;
        _tenantService = tenantService;
        _tenantContext = tenantContext;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SubscriptionPlan>>> GetPlans()
    {
        try
        {
            var plans = await _stripeService.GetAvailablePlansAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return StatusCode(500, new { message = "Error retrieving plans" });
        }
    }

    /// <summary>
    /// Create Stripe checkout session for subscription
    /// </summary>
    [HttpPost("checkout")]
    [Authorize]
    public async Task<ActionResult<object>> CreateCheckoutSession([FromBody] CheckoutRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(_tenantContext.TenantId))
            {
                return BadRequest(new { message = "No tenant context found" });
            }

            var tenant = await _tenantService.GetTenantByIdAsync(_tenantContext.TenantId);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            // Create Stripe customer if doesn't exist
            if (string.IsNullOrEmpty(tenant.StripeCustomerId))
            {
                var customer = await _stripeService.CreateCustomerAsync(
                    tenant.ContactEmail,
                    tenant.CompanyName,
                    tenant.Id
                );
                
                tenant.StripeCustomerId = customer.Id;
                await _tenantService.UpdateTenantAsync(tenant);
            }

            var plan = await _stripeService.GetPlanByTierAsync(request.Tier);
            if (plan == null)
            {
                return BadRequest(new { message = "Invalid plan tier" });
            }

            if (string.IsNullOrEmpty(plan.StripePriceId))
            {
                return BadRequest(new { message = "This tier does not require payment" });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkoutUrl = await _stripeService.CreateCheckoutSessionAsync(
                tenant.StripeCustomerId,
                plan.StripePriceId,
                $"{baseUrl}/billing/success?session_id={{CHECKOUT_SESSION_ID}}",
                $"{baseUrl}/billing/cancel"
            );

            return Ok(new { checkoutUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session");
            return StatusCode(500, new { message = "Error creating checkout session" });
        }
    }

    /// <summary>
    /// Open Stripe customer portal for subscription management
    /// </summary>
    [HttpPost("portal")]
    [Authorize]
    public async Task<ActionResult<object>> CreateCustomerPortalSession()
    {
        try
        {
            if (string.IsNullOrEmpty(_tenantContext.TenantId))
            {
                return BadRequest(new { message = "No tenant context found" });
            }

            var tenant = await _tenantService.GetTenantByIdAsync(_tenantContext.TenantId);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            if (string.IsNullOrEmpty(tenant.StripeCustomerId))
            {
                return BadRequest(new { message = "No Stripe customer found" });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var portalUrl = await _stripeService.CreateCustomerPortalSessionAsync(
                tenant.StripeCustomerId,
                $"{baseUrl}/billing"
            );

            return Ok(new { portalUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer portal session");
            return StatusCode(500, new { message = "Error creating portal session" });
        }
    }

    /// <summary>
    /// Get current subscription details
    /// </summary>
    [HttpGet("subscription")]
    [Authorize]
    public async Task<ActionResult<object>> GetCurrentSubscription()
    {
        try
        {
            if (string.IsNullOrEmpty(_tenantContext.TenantId))
            {
                return BadRequest(new { message = "No tenant context found" });
            }

            var tenant = await _tenantService.GetTenantByIdAsync(_tenantContext.TenantId);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            object? stripeSubscription = null;
            if (!string.IsNullOrEmpty(tenant.StripeSubscriptionId))
            {
                try
                {
                    var subscription = await _stripeService.GetSubscriptionAsync(tenant.StripeSubscriptionId);
                    stripeSubscription = new
                    {
                        id = subscription.Id,
                        status = subscription.Status,
                        // TODO: Map Stripe subscription period end dates correctly
                        // currentPeriodEnd = ...,
                        cancelAtPeriodEnd = subscription.CancelAtPeriodEnd
                        // trialEnd = ...
                    };
                }
                catch (StripeException)
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found in Stripe", tenant.StripeSubscriptionId);
                }
            }

            var plan = await _stripeService.GetPlanByTierAsync(tenant.SubscriptionTier);

            return Ok(new
            {
                tier = tenant.SubscriptionTier,
                plan,
                isActive = tenant.IsActive,
                isTrial = tenant.IsTrialActive,
                trialEndDate = tenant.TrialEndDate,
                subscriptionStartDate = tenant.SubscriptionStartDate,
                subscriptionEndDate = tenant.SubscriptionEndDate,
                stripeSubscription
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription details");
            return StatusCode(500, new { message = "Error retrieving subscription" });
        }
    }

    /// <summary>
    /// Get invoices for current tenant
    /// </summary>
    [HttpGet("invoices")]
    [Authorize]
    public async Task<ActionResult<object>> GetInvoices([FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrEmpty(_tenantContext.TenantId))
            {
                return BadRequest(new { message = "No tenant context found" });
            }

            var tenant = await _tenantService.GetTenantByIdAsync(_tenantContext.TenantId);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            if (string.IsNullOrEmpty(tenant.StripeCustomerId))
            {
                return Ok(new { invoices = new List<object>() });
            }

            var invoices = await _stripeService.GetCustomerInvoicesAsync(tenant.StripeCustomerId, limit);

            var result = invoices.Select(inv => new
            {
                id = inv.Id,
                number = inv.Number,
                status = inv.Status,
                amount = inv.AmountDue / 100.0, // Stripe amounts are in cents
                currency = inv.Currency,
                created = inv.Created,
                dueDate = inv.DueDate,
                paidAt = inv.StatusTransitions?.PaidAt,
                invoicePdf = inv.InvoicePdf,
                hostedInvoiceUrl = inv.HostedInvoiceUrl
            });

            return Ok(new { invoices = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices");
            return StatusCode(500, new { message = "Error retrieving invoices" });
        }
    }

    /// <summary>
    /// Webhook endpoint for Stripe events
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook received without signature");
                return BadRequest(new { message = "Missing signature" });
            }

            var webhookSecret = _configuration["Stripe:WebhookSecret"] ?? string.Empty;
            var stripeEvent = await _stripeService.ConstructWebhookEventAsync(json, signature, webhookSecret);

            _logger.LogInformation("Received Stripe webhook: {EventType} - {EventId}", stripeEvent.Type, stripeEvent.Id);

            // Process event asynchronously (fire and forget to return 200 quickly)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _stripeService.ProcessWebhookEventAsync(stripeEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing webhook event {EventId}", stripeEvent.Id);
                }
            });

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return BadRequest(new { message = "Invalid webhook signature" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500);
        }
    }
}

public record CheckoutRequest(string Tier);
