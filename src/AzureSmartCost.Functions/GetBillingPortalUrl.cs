using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Stripe;
using Stripe.BillingPortal;

namespace AzureSmartCost.Functions;

public class GetBillingPortalUrl
{
    private readonly ILogger<GetBillingPortalUrl> _logger;

    public GetBillingPortalUrl(ILogger<GetBillingPortalUrl> logger)
    {
        _logger = logger;
        
        // Configure Stripe API key
        StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("Stripe__SecretKey")
            ?? throw new InvalidOperationException("Stripe__SecretKey not configured");
    }

    [Function("GetBillingPortalUrl")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "billing/portal/{customerId}")] HttpRequestData req,
        string customerId)
    {
        _logger.LogInformation("Creating billing portal session for customer: {CustomerId}", customerId);

        try
        {
            if (string.IsNullOrEmpty(customerId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Customer ID is required");
                return badResponse;
            }

            // Get return URL
            var dashboardUrl = Environment.GetEnvironmentVariable("DASHBOARD_URL") 
                ?? "https://blue-flower-0414b9b0f.3.azurestaticapps.net";

            // Create billing portal session
            var options = new SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = $"{dashboardUrl}/billing"
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created billing portal session: {SessionId} for customer: {CustomerId}", 
                session.Id, customerId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                url = session.Url
            });

            return response;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error creating billing portal session");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync($"Stripe error: {ex.Message}");
            return errorResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating billing portal session");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}
