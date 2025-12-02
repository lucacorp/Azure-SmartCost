using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Stripe;
using Stripe.Checkout;
using Microsoft.Azure.Cosmos;
using AzureSmartCost.Functions.Models;

namespace AzureSmartCost.Functions;

public class StripeWebhook
{
    private readonly ILogger<StripeWebhook> _logger;
    private readonly Container _tenantsContainer;
    private readonly string _webhookSecret;

    public StripeWebhook(ILogger<StripeWebhook> logger)
    {
        _logger = logger;
        
        // Initialize Cosmos DB
        var cosmosEndpoint = Environment.GetEnvironmentVariable("CosmosDb__Endpoint") 
            ?? throw new InvalidOperationException("CosmosDb__Endpoint not configured");
        var cosmosKey = Environment.GetEnvironmentVariable("CosmosDb__Key")
            ?? throw new InvalidOperationException("CosmosDb__Key not configured");
        
        var cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey);
        _tenantsContainer = cosmosClient.GetContainer("SmartCost", "Tenants");
        
        // Get Stripe webhook secret
        _webhookSecret = Environment.GetEnvironmentVariable("Stripe__WebhookSecret") 
            ?? throw new InvalidOperationException("Stripe__WebhookSecret not configured");
        
        // Configure Stripe API key
        StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("Stripe__SecretKey")
            ?? throw new InvalidOperationException("Stripe__SecretKey not configured");
    }

    [Function("StripeWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "stripe/webhook")] HttpRequestData req)
    {
        _logger.LogInformation("Stripe webhook received");

        try
        {
            // Read the request body
            var json = await new StreamReader(req.Body).ReadToEndAsync();
            
            // Verify webhook signature
            var stripeSignature = req.Headers.Contains("Stripe-Signature") 
                ? req.Headers.GetValues("Stripe-Signature").FirstOrDefault() 
                : null;

            if (string.IsNullOrEmpty(stripeSignature))
            {
                _logger.LogWarning("Missing Stripe-Signature header");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Missing signature");
                return badResponse;
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    _webhookSecret
                );
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Invalid Stripe webhook signature");
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync($"Webhook signature verification failed: {e.Message}");
                return unauthorizedResponse;
            }

            _logger.LogInformation("Processing Stripe event: {EventType} - {EventId}", stripeEvent.Type, stripeEvent.Id);

            // Process the event
            await ProcessStripeEventAsync(stripeEvent);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Webhook processed successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }

    private async Task ProcessStripeEventAsync(Event stripeEvent)
    {
        switch (stripeEvent.Type)
        {
            case Events.CheckoutSessionCompleted:
                await HandleCheckoutSessionCompletedAsync(stripeEvent);
                break;

            case Events.CustomerSubscriptionCreated:
                await HandleSubscriptionCreatedAsync(stripeEvent);
                break;

            case Events.CustomerSubscriptionUpdated:
                await HandleSubscriptionUpdatedAsync(stripeEvent);
                break;

            case Events.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeletedAsync(stripeEvent);
                break;

            case Events.InvoicePaymentSucceeded:
                await HandleInvoicePaymentSucceededAsync(stripeEvent);
                break;

            case Events.InvoicePaymentFailed:
                await HandleInvoicePaymentFailedAsync(stripeEvent);
                break;

            case Events.CustomerCreated:
                await HandleCustomerCreatedAsync(stripeEvent);
                break;

            default:
                _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null)
        {
            _logger.LogWarning("Invalid checkout session object");
            return;
        }

        _logger.LogInformation("Checkout session completed: {SessionId}, Customer: {CustomerId}", 
            session.Id, session.CustomerId);

        // Get tenant ID from metadata
        var tenantId = session.Metadata?.ContainsKey("tenant_id") == true 
            ? session.Metadata["tenant_id"] 
            : null;
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("No tenant_id in checkout session metadata");
            return;
        }

        // Update tenant with Stripe customer ID and subscription ID
        try
        {
            var tenant = await GetTenantAsync(tenantId, tenantId);
            if (tenant != null)
            {
                tenant.StripeCustomerId = session.CustomerId;
                tenant.StripeSubscriptionId = session.SubscriptionId;
                tenant.Status = "Subscribed";
                tenant.LastModifiedAt = DateTime.UtcNow;
                
                await _tenantsContainer.UpsertItemAsync(tenant, new PartitionKey(tenant.MarketplaceSubscriptionId ?? tenantId));
                
                _logger.LogInformation("Updated tenant {TenantId} with Stripe customer {CustomerId}", 
                    tenantId, session.CustomerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant after checkout: {TenantId}", tenantId);
        }
    }

    private async Task HandleSubscriptionCreatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        _logger.LogInformation("Subscription created: {SubscriptionId}, Customer: {CustomerId}, Status: {Status}", 
            subscription.Id, subscription.CustomerId, subscription.Status);

        await UpdateTenantSubscriptionAsync(subscription);
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        _logger.LogInformation("Subscription updated: {SubscriptionId}, Status: {Status}", 
            subscription.Id, subscription.Status);

        await UpdateTenantSubscriptionAsync(subscription);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        _logger.LogInformation("Subscription deleted: {SubscriptionId}, Customer: {CustomerId}", 
            subscription.Id, subscription.CustomerId);

        // Find tenant by Stripe subscription ID
        var tenant = await FindTenantByStripeSubscriptionAsync(subscription.Id);
        if (tenant != null)
        {
            tenant.Status = "Cancelled";
            tenant.CancelledAt = DateTime.UtcNow;
            tenant.LastModifiedAt = DateTime.UtcNow;
            
            await _tenantsContainer.UpsertItemAsync(tenant, new PartitionKey(tenant.MarketplaceSubscriptionId ?? tenant.Id));
            
            _logger.LogInformation("Cancelled tenant {TenantId}", tenant.Id);
        }
    }

    private async Task HandleInvoicePaymentSucceededAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        _logger.LogInformation("Invoice payment succeeded: {InvoiceId}, Amount: {Amount}, Customer: {CustomerId}", 
            invoice.Id, invoice.AmountPaid, invoice.CustomerId);

        // Find tenant and update last payment date
        var tenant = await FindTenantByStripeCustomerAsync(invoice.CustomerId);
        if (tenant != null)
        {
            tenant.LastPaymentAt = DateTime.UtcNow;
            tenant.LastModifiedAt = DateTime.UtcNow;
            
            // Ensure status is active
            if (tenant.Status == "Suspended" || tenant.Status == "PaymentFailed")
            {
                tenant.Status = "Subscribed";
                tenant.SuspendedAt = null;
            }
            
            await _tenantsContainer.UpsertItemAsync(tenant, new PartitionKey(tenant.MarketplaceSubscriptionId ?? tenant.Id));
            
            _logger.LogInformation("Updated tenant {TenantId} after successful payment", tenant.Id);
        }
    }

    private async Task HandleInvoicePaymentFailedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        _logger.LogWarning("Invoice payment failed: {InvoiceId}, Customer: {CustomerId}", 
            invoice.Id, invoice.CustomerId);

        // Find tenant and mark as payment failed
        var tenant = await FindTenantByStripeCustomerAsync(invoice.CustomerId);
        if (tenant != null)
        {
            tenant.Status = "PaymentFailed";
            tenant.LastModifiedAt = DateTime.UtcNow;
            
            await _tenantsContainer.UpsertItemAsync(tenant, new PartitionKey(tenant.MarketplaceSubscriptionId ?? tenant.Id));
            
            _logger.LogWarning("Marked tenant {TenantId} as payment failed", tenant.Id);
            
            // TODO: Send notification email to customer
        }
    }

    private async Task HandleCustomerCreatedAsync(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Customer;
        if (customer == null) return;

        _logger.LogInformation("Customer created: {CustomerId}, Email: {Email}", 
            customer.Id, customer.Email);
    }

    private async Task UpdateTenantSubscriptionAsync(Subscription subscription)
    {
        var tenant = await FindTenantByStripeCustomerAsync(subscription.CustomerId);
        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for Stripe customer: {CustomerId}", subscription.CustomerId);
            return;
        }

        tenant.StripeSubscriptionId = subscription.Id;
        tenant.Status = MapStripeStatusToTenantStatus(subscription.Status);
        tenant.LastModifiedAt = DateTime.UtcNow;

        // Update plan based on Stripe price ID
        if (subscription.Items?.Data?.Count > 0)
        {
            var priceId = subscription.Items.Data[0].Price.Id;
            tenant.PlanId = MapStripePriceToPlan(priceId);
        }

        // Handle trial period
        if (subscription.TrialEnd.HasValue && subscription.TrialEnd.Value > DateTime.UtcNow)
        {
            tenant.TrialEndDate = subscription.TrialEnd.Value;
        }

        await _tenantsContainer.UpsertItemAsync(tenant, new PartitionKey(tenant.MarketplaceSubscriptionId ?? tenant.Id));
        
        _logger.LogInformation("Updated tenant {TenantId} subscription status to {Status}", tenant.Id, tenant.Status);
    }

    private string MapStripeStatusToTenantStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "active" => "Subscribed",
            "trialing" => "Trial",
            "past_due" => "PaymentFailed",
            "canceled" => "Cancelled",
            "unpaid" => "Suspended",
            _ => "Unknown"
        };
    }

    private string MapStripePriceToPlan(string priceId)
    {
        // TODO: Map Stripe price IDs to plan names
        // This should match your Stripe product configuration
        var priceEnv = Environment.GetEnvironmentVariable("Stripe__Prices") ?? "";
        
        if (priceEnv.Contains($"basic:{priceId}")) return "basic";
        if (priceEnv.Contains($"pro:{priceId}")) return "pro";
        if (priceEnv.Contains($"enterprise:{priceId}")) return "enterprise";
        
        return "basic"; // Default
    }

    private async Task<Tenant?> GetTenantAsync(string id, string partitionKey)
    {
        try
        {
            var response = await _tenantsContainer.ReadItemAsync<Tenant>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<Tenant?> FindTenantByStripeCustomerAsync(string stripeCustomerId)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.stripeCustomerId = @customerId")
                .WithParameter("@customerId", stripeCustomerId);

            var iterator = _tenantsContainer.GetItemQueryIterator<Tenant>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var tenant = response.FirstOrDefault();
                if (tenant != null)
                    return tenant;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding tenant by Stripe customer ID: {CustomerId}", stripeCustomerId);
            return null;
        }
    }

    private async Task<Tenant?> FindTenantByStripeSubscriptionAsync(string stripeSubscriptionId)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.stripeSubscriptionId = @subscriptionId")
                .WithParameter("@subscriptionId", stripeSubscriptionId);

            var iterator = _tenantsContainer.GetItemQueryIterator<Tenant>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var tenant = response.FirstOrDefault();
                if (tenant != null)
                    return tenant;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding tenant by Stripe subscription ID: {SubscriptionId}", stripeSubscriptionId);
            return null;
        }
    }
}
