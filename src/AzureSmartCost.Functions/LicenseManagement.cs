using System;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Functions.Services;

namespace AzureSmartCost.Functions;

public class LicenseManagement
{
    private readonly ILogger<LicenseManagement> _logger;
    private readonly LicenseService _licenseService;

    public LicenseManagement(ILogger<LicenseManagement> logger, LicenseService licenseService)
    {
        _logger = logger;
        _licenseService = licenseService;
    }

    [Function("ValidateLicense")]
    public async Task<HttpResponseData> ValidateLicense(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "license/validate/{subscriptionId}")] HttpRequestData req,
        string subscriptionId)
    {
        _logger.LogInformation($"Validating license for subscription: {subscriptionId}");

        var (isValid, license, message) = await _licenseService.ValidateLicenseAsync(subscriptionId);

        var response = req.CreateResponse(isValid ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
        await response.WriteAsJsonAsync(new
        {
            isValid,
            message,
            license = license != null ? new
            {
                license.SubscriptionId,
                license.Status,
                license.CreatedAt,
                license.ActivatedAt,
                license.ExpiresAt,
                license.MonthlyFee,
                license.Currency,
                TrialDaysRemaining = license.Status == Models.LicenseStatus.Trial 
                    ? Math.Max(0, (license.CreatedAt.AddDays(license.TrialDays) - DateTime.UtcNow).Days)
                    : 0
            } : null
        });

        return response;
    }

    [Function("CreateLicense")]
    public async Task<HttpResponseData> CreateLicense(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/license")] HttpRequestData req)
    {
        _logger.LogInformation("Creating new license");

        var body = await JsonSerializer.DeserializeAsync<CreateLicenseRequest>(req.Body);
        
        if (body == null || string.IsNullOrEmpty(body.SubscriptionId))
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync("SubscriptionId is required");
            return errorResponse;
        }

        var license = await _licenseService.CreateLicenseAsync(
            body.SubscriptionId,
            body.CustomerEmail ?? "",
            body.CustomerName ?? ""
        );

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(license);
        return response;
    }

    [Function("ActivateLicense")]
    public async Task<HttpResponseData> ActivateLicense(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/license/{subscriptionId}/activate")] HttpRequestData req,
        string subscriptionId)
    {
        _logger.LogInformation($"Activating license for subscription: {subscriptionId}");

        var body = await JsonSerializer.DeserializeAsync<ActivateLicenseRequest>(req.Body);
        var durationMonths = body?.DurationMonths ?? 1;

        var license = await _licenseService.ActivateLicenseAsync(subscriptionId, durationMonths);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            message = $"License activated for {durationMonths} month(s)",
            license
        });

        return response;
    }

    [Function("SuspendLicense")]
    public async Task<HttpResponseData> SuspendLicense(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/license/{subscriptionId}/suspend")] HttpRequestData req,
        string subscriptionId)
    {
        _logger.LogInformation($"Suspending license for subscription: {subscriptionId}");

        var license = await _licenseService.SuspendLicenseAsync(subscriptionId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            message = "License suspended",
            license
        });

        return response;
    }

    [Function("GetAllLicenses")]
    public async Task<HttpResponseData> GetAllLicenses(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/licenses")] HttpRequestData req)
    {
        _logger.LogInformation("Fetching all licenses");

        var licenses = await _licenseService.GetAllLicensesAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(licenses);
        return response;
    }

    private record CreateLicenseRequest(string SubscriptionId, string? CustomerEmail, string? CustomerName);
    private record ActivateLicenseRequest(int DurationMonths);
}
