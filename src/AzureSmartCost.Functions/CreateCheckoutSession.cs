using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Stripe;
using Stripe.Checkout;

namespace AzureSmartCost.Functions;

public class CreateCheckoutSession
{
    private readonly ILogger<CreateCheckoutSession> _logger;

    public CreateCheckoutSession(ILogger<CreateCheckoutSession> logger)
    {
        _logger = logger;
        
        // Configure Stripe API key
        StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("Stripe__SecretKey")
            ?? throw new InvalidOperationException("Stripe__SecretKey not configured");
    }

    [Function("CreateCheckoutSession")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "billing/checkout")] HttpRequestData req)
    {
        _logger.LogInformation("Creating Stripe checkout session");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var checkoutRequest = JsonSerializer.Deserialize<CheckoutRequest>(requestBody);

            if (checkoutRequest == null || string.IsNullOrEmpty(checkoutRequest.PriceId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Missing required fields: priceId");
                return badResponse;
            }

            // Get base URL for redirect
            var dashboardUrl = Environment.GetEnvironmentVariable("DASHBOARD_URL") 
                ?? "https://blue-flower-0414b9b0f.3.azurestaticapps.net";

            // Create Stripe checkout session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = checkoutRequest.PriceId,
                        Quantity = 1,
                    },
                },
                SuccessUrl = $"{dashboardUrl}/billing/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{dashboardUrl}/billing/cancel",
                CustomerEmail = checkoutRequest.Email,
                Metadata = new Dictionary<string, string>
                {
                    { "tenant_id", checkoutRequest.TenantId ?? "unknown" },
                    { "azure_subscription_id", checkoutRequest.AzureSubscriptionId ?? "" }
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "tenant_id", checkoutRequest.TenantId ?? "unknown" },
                        { "azure_subscription_id", checkoutRequest.AzureSubscriptionId ?? "" }
                    }
                }
            };

            // Add trial period if specified
            if (checkoutRequest.TrialDays > 0)
            {
                options.SubscriptionData.TrialPeriodDays = checkoutRequest.TrialDays;
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created checkout session: {SessionId} for tenant: {TenantId}", 
                session.Id, checkoutRequest.TenantId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                sessionId = session.Id,
                url = session.Url,
                customerId = session.CustomerId
            });

            return response;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error creating checkout session");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync($"Stripe error: {ex.Message}");
            return errorResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}

public class CheckoutRequest
{
    public string? PriceId { get; set; }
    public string? Email { get; set; }
    public string? TenantId { get; set; }
    public string? AzureSubscriptionId { get; set; }
    public int TrialDays { get; set; }
}
