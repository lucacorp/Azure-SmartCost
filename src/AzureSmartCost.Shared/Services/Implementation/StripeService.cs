using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stripe;
using Stripe.Checkout;
using Stripe.BillingPortal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Services.Implementation;

public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly ITenantService _tenantService;
    private readonly ILogger<StripeService> _logger;
    private readonly string _stripeSecretKey;
    private readonly string _webhookSecret;

    // Stripe Price IDs - These should be configured in appsettings or Key Vault
    private readonly Dictionary<string, SubscriptionPlan> _plans;

    public StripeService(
        IConfiguration configuration,
        ITenantService tenantService,
        ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _tenantService = tenantService;
        _logger = logger;
        
        _stripeSecretKey = configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe:SecretKey not configured");
        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;
        
        StripeConfiguration.ApiKey = _stripeSecretKey;

        // Initialize plans - In production, these IDs should come from Stripe Dashboard
        _plans = new Dictionary<string, SubscriptionPlan>
        {
            ["Free"] = new SubscriptionPlan
            {
                Id = "plan_free",
                Name = "Free",
                Tier = "Free",
                MonthlyPrice = 0,
                AnnualPrice = 0,
                Currency = "usd",
                StripePriceId = "", // Free tier doesn't need Stripe
                MaxUsers = 5,
                MaxAzureSubscriptions = 1,
                MaxMonthlyApiCalls = 10000,
                HasAdvancedAnalytics = false,
                HasMLPredictions = false,
                HasPowerBIIntegration = false,
                HasCustomBranding = false,
                HasSSOSupport = false,
                HasPrioritySupport = false,
                Description = "Perfect for individuals and small teams getting started",
                Features = new List<string>
                {
                    "Up to 5 users",
                    "1 Azure subscription",
                    "10,000 API calls/month",
                    "Basic cost analytics",
                    "Email support"
                },
                SortOrder = 1
            },
            ["Pro"] = new SubscriptionPlan
            {
                Id = "plan_pro",
                Name = "Professional",
                Tier = "Pro",
                MonthlyPrice = 99,
                AnnualPrice = 990, // 2 months free
                Currency = "usd",
                StripePriceId = configuration["Stripe:ProPriceId"] ?? "price_pro_monthly",
                MaxUsers = 50,
                MaxAzureSubscriptions = 5,
                MaxMonthlyApiCalls = 100000,
                HasAdvancedAnalytics = true,
                HasMLPredictions = false,
                HasPowerBIIntegration = true,
                HasCustomBranding = false,
                HasSSOSupport = false,
                HasPrioritySupport = true,
                Description = "For growing teams that need advanced features",
                Features = new List<string>
                {
                    "Up to 50 users",
                    "5 Azure subscriptions",
                    "100,000 API calls/month",
                    "Advanced cost analytics",
                    "Power BI integration",
                    "Custom reports",
                    "Priority email support"
                },
                IsPopular = true,
                SortOrder = 2
            },
            ["Enterprise"] = new SubscriptionPlan
            {
                Id = "plan_enterprise",
                Name = "Enterprise",
                Tier = "Enterprise",
                MonthlyPrice = 499,
                AnnualPrice = 4990,
                Currency = "usd",
                StripePriceId = configuration["Stripe:EnterprisePriceId"] ?? "price_enterprise_monthly",
                MaxUsers = int.MaxValue,
                MaxAzureSubscriptions = int.MaxValue,
                MaxMonthlyApiCalls = int.MaxValue,
                HasAdvancedAnalytics = true,
                HasMLPredictions = true,
                HasPowerBIIntegration = true,
                HasCustomBranding = true,
                HasSSOSupport = true,
                HasPrioritySupport = true,
                Description = "For large organizations with custom requirements",
                Features = new List<string>
                {
                    "Unlimited users",
                    "Unlimited Azure subscriptions",
                    "Unlimited API calls",
                    "ML-powered predictions",
                    "Custom branding",
                    "SSO/SAML support",
                    "Dedicated account manager",
                    "24/7 phone support",
                    "SLA guarantees"
                },
                SortOrder = 3
            }
        };
    }

    public async Task<Customer> CreateCustomerAsync(string email, string name, string tenantId)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId
                }
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe customer {CustomerId} for tenant {TenantId}", customer.Id, tenantId);
            
            return customer;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating Stripe customer for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Customer> GetCustomerAsync(string customerId)
    {
        var service = new CustomerService();
        return await service.GetAsync(customerId);
    }

    public async Task<Customer> UpdateCustomerAsync(string customerId, Dictionary<string, string> metadata)
    {
        var options = new CustomerUpdateOptions
        {
            Metadata = metadata
        };

        var service = new CustomerService();
        return await service.UpdateAsync(customerId, options);
    }

    public async Task<Subscription> CreateSubscriptionAsync(string customerId, string priceId, string tenantId)
    {
        try
        {
            var options = new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Price = priceId
                    }
                },
                PaymentBehavior = "default_incomplete",
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    SaveDefaultPaymentMethod = "on_subscription"
                },
                Metadata = new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId
                }
            };

            var service = new SubscriptionService();
            var subscription = await service.CreateAsync(options);

            _logger.LogInformation("Created subscription {SubscriptionId} for customer {CustomerId}", subscription.Id, customerId);
            
            return subscription;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating subscription for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Subscription> GetSubscriptionAsync(string subscriptionId)
    {
        var service = new SubscriptionService();
        return await service.GetAsync(subscriptionId);
    }

    public async Task<Subscription> UpdateSubscriptionAsync(string subscriptionId, string newPriceId)
    {
        try
        {
            var subscription = await GetSubscriptionAsync(subscriptionId);
            
            var options = new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = subscription.Items.Data[0].Id,
                        Price = newPriceId
                    }
                },
                ProrationBehavior = "create_prorations"
            };

            var service = new SubscriptionService();
            var updated = await service.UpdateAsync(subscriptionId, options);

            _logger.LogInformation("Updated subscription {SubscriptionId} to price {PriceId}", subscriptionId, newPriceId);
            
            return updated;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<Subscription> CancelSubscriptionAsync(string subscriptionId, bool immediately = false)
    {
        try
        {
            var service = new SubscriptionService();
            
            if (immediately)
            {
                var cancelled = await service.CancelAsync(subscriptionId);
                _logger.LogWarning("Immediately cancelled subscription {SubscriptionId}", subscriptionId);
                return cancelled;
            }
            else
            {
                var options = new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                };
                var updated = await service.UpdateAsync(subscriptionId, options);
                _logger.LogInformation("Subscription {SubscriptionId} will be cancelled at period end", subscriptionId);
                return updated;
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<Subscription> ReactivateSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var options = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = false
            };

            var service = new SubscriptionService();
            var reactivated = await service.UpdateAsync(subscriptionId, options);

            _logger.LogInformation("Reactivated subscription {SubscriptionId}", subscriptionId);
            
            return reactivated;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error reactivating subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public Task<List<SubscriptionPlan>> GetAvailablePlansAsync()
    {
        return Task.FromResult(_plans.Values.OrderBy(p => p.SortOrder).ToList());
    }

    public Task<SubscriptionPlan?> GetPlanByTierAsync(string tier)
    {
        _plans.TryGetValue(tier, out var plan);
        return Task.FromResult(plan);
    }

    public async Task<string> CreateCheckoutSessionAsync(string customerId, string priceId, string successUrl, string cancelUrl)
    {
        try
        {
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                Customer = customerId,
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            };

            var service = new Stripe.Checkout.SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created checkout session {SessionId} for customer {CustomerId}", session.Id, customerId);
            
            return session.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating checkout session for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<string> CreateCustomerPortalSessionAsync(string customerId, string returnUrl)
    {
        try
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created customer portal session for customer {CustomerId}", customerId);
            
            return session.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating customer portal session for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Invoice> GetInvoiceAsync(string invoiceId)
    {
        var service = new InvoiceService();
        return await service.GetAsync(invoiceId);
    }

    public async Task<List<Invoice>> GetCustomerInvoicesAsync(string customerId, int limit = 10)
    {
        var options = new InvoiceListOptions
        {
            Customer = customerId,
            Limit = limit
        };

        var service = new InvoiceService();
        var invoices = await service.ListAsync(options);

        return invoices.Data;
    }

    public Task<Event> ConstructWebhookEventAsync(string json, string signature, string secret)
    {
        try
        {
            var webhookSecret = string.IsNullOrEmpty(secret) ? _webhookSecret : secret;
            var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
            return Task.FromResult(stripeEvent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error constructing webhook event");
            throw;
        }
    }

    public async Task ProcessWebhookEventAsync(Event stripeEvent)
    {
        _logger.LogInformation("Processing Stripe webhook event {EventType} - {EventId}", stripeEvent.Type, stripeEvent.Id);

        try
        {
            switch (stripeEvent.Type)
            {
                case "customer.subscription.created":
                    await HandleSubscriptionCreatedAsync(stripeEvent);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(stripeEvent);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent);
                    break;

                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceededAsync(stripeEvent);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailedAsync(stripeEvent);
                    break;

                case "customer.created":
                    _logger.LogInformation("Customer created: {CustomerId}", stripeEvent.Data.Object);
                    break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventType} - {EventId}", stripeEvent.Type, stripeEvent.Id);
            throw;
        }
    }

    private async Task HandleSubscriptionCreatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var tenantId = subscription.Metadata.GetValueOrDefault("tenant_id");
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("Subscription {SubscriptionId} has no tenant_id in metadata", subscription.Id);
            return;
        }

        var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
        if (tenant == null)
        {
            _logger.LogError("Tenant {TenantId} not found for subscription {SubscriptionId}", tenantId, subscription.Id);
            return;
        }

        tenant.StripeSubscriptionId = subscription.Id;
        tenant.StripeCustomerId = subscription.CustomerId;
        tenant.SubscriptionStartDate = subscription.StartDate;
        tenant.IsActive = subscription.Status == "active" || subscription.Status == "trialing";

        await _tenantService.UpdateTenantAsync(tenant);
        _logger.LogInformation("Updated tenant {TenantId} with subscription {SubscriptionId}", tenantId, subscription.Id);
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var tenantId = subscription.Metadata.GetValueOrDefault("tenant_id");
        if (string.IsNullOrEmpty(tenantId)) return;

        var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
        if (tenant == null) return;

        tenant.IsActive = subscription.Status == "active" || subscription.Status == "trialing";
        
        // TODO: Add subscription end date tracking when cancel_at_period_end is true
        // if (subscription.CancelAtPeriodEnd && subscription.CurrentPeriodEnd.HasValue)
        // {
        //     var periodEnd = DateTimeOffset.FromUnixTimeSeconds(subscription.CurrentPeriodEnd.Value).DateTime;
        //     tenant.SubscriptionEndDate = periodEnd;
        //     _logger.LogWarning("Subscription {SubscriptionId} will end at {EndDate}", subscription.Id, periodEnd);
        // }


        await _tenantService.UpdateTenantAsync(tenant);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var tenantId = subscription.Metadata.GetValueOrDefault("tenant_id");
        if (string.IsNullOrEmpty(tenantId)) return;

        await _tenantService.CancelSubscriptionAsync(tenantId);
        _logger.LogWarning("Cancelled subscription for tenant {TenantId}", tenantId);
    }

    private async Task HandleInvoicePaymentSucceededAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        _logger.LogInformation("Invoice {InvoiceId} payment succeeded for customer {CustomerId}", invoice.Id, invoice.CustomerId);
        
        // You could store invoice details in Cosmos DB here
        await Task.CompletedTask;
    }

    private async Task HandleInvoicePaymentFailedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        _logger.LogError("Invoice {InvoiceId} payment failed for customer {CustomerId}", invoice.Id, invoice.CustomerId);
        
        // You could send notification emails here
        await Task.CompletedTask;
    }
}
