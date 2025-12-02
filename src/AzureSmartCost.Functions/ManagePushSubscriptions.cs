using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using WebPush;

namespace AzureSmartCost.Functions;

public class ManagePushSubscriptions
{
    private readonly ILogger<ManagePushSubscriptions> _logger;
    private readonly Container _subscriptionsContainer;
    private readonly VapidDetails _vapidDetails;

    public ManagePushSubscriptions(ILogger<ManagePushSubscriptions> logger)
    {
        _logger = logger;
        
        // Initialize Cosmos DB
        var cosmosEndpoint = Environment.GetEnvironmentVariable("CosmosDb__Endpoint") 
            ?? throw new InvalidOperationException("CosmosDb__Endpoint not configured");
        var cosmosKey = Environment.GetEnvironmentVariable("CosmosDb__Key")
            ?? throw new InvalidOperationException("CosmosDb__Key not configured");
        
        var cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey);
        _subscriptionsContainer = cosmosClient.GetContainer("SmartCost", "PushSubscriptions");
        
        // Initialize VAPID details
        var vapidPublicKey = Environment.GetEnvironmentVariable("VAPID__PublicKey") 
            ?? throw new InvalidOperationException("VAPID__PublicKey not configured");
        var vapidPrivateKey = Environment.GetEnvironmentVariable("VAPID__PrivateKey")
            ?? throw new InvalidOperationException("VAPID__PrivateKey not configured");
        var vapidSubject = Environment.GetEnvironmentVariable("VAPID__Subject") 
            ?? "mailto:support@smartcost.com";
        
        _vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
    }

    [Function("SubscribePushNotifications")]
    public async Task<HttpResponseData> Subscribe(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications/subscribe")] HttpRequestData req)
    {
        _logger.LogInformation("Subscribing to push notifications");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var subscription = JsonSerializer.Deserialize<PushSubscriptionRequest>(requestBody);

            if (subscription == null || string.IsNullOrEmpty(subscription.Endpoint))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid subscription data");
                return badResponse;
            }

            // Create subscription document
            var pushSub = new PushSubscription
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = subscription.TenantId ?? "unknown",
                UserId = subscription.UserId ?? "unknown",
                Endpoint = subscription.Endpoint,
                P256dh = subscription.Keys?.P256dh,
                Auth = subscription.Keys?.Auth,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _subscriptionsContainer.CreateItemAsync(pushSub, new PartitionKey(pushSub.TenantId));

            _logger.LogInformation("Created push subscription: {SubscriptionId} for tenant: {TenantId}", 
                pushSub.Id, pushSub.TenantId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                id = pushSub.Id,
                message = "Subscribed successfully"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to push notifications");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("UnsubscribePushNotifications")]
    public async Task<HttpResponseData> Unsubscribe(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "notifications/unsubscribe/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Unsubscribing from push notifications: {SubscriptionId}", id);

        try
        {
            // Query to find subscription
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);

            var iterator = _subscriptionsContainer.GetItemQueryIterator<PushSubscription>(query);
            
            PushSubscription? subscription = null;
            while (iterator.HasMoreResults)
            {
                var results = await iterator.ReadNextAsync();
                subscription = results.FirstOrDefault();
                if (subscription != null) break;
            }

            if (subscription == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Subscription not found");
                return notFoundResponse;
            }

            // Delete subscription
            await _subscriptionsContainer.DeleteItemAsync<PushSubscription>(id, new PartitionKey(subscription.TenantId));

            _logger.LogInformation("Deleted push subscription: {SubscriptionId}", id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Unsubscribed successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from push notifications");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("SendPushNotification")]
    public async Task<HttpResponseData> Send(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications/send")] HttpRequestData req)
    {
        _logger.LogInformation("Sending push notification");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var notification = JsonSerializer.Deserialize<NotificationRequest>(requestBody);

            if (notification == null || string.IsNullOrEmpty(notification.TenantId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid notification data");
                return badResponse;
            }

            // Get all subscriptions for tenant
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.isActive = true")
                .WithParameter("@tenantId", notification.TenantId);

            var iterator = _subscriptionsContainer.GetItemQueryIterator<PushSubscription>(query);
            var subscriptions = new List<PushSubscription>();
            
            while (iterator.HasMoreResults)
            {
                var results = await iterator.ReadNextAsync();
                subscriptions.AddRange(results);
            }

            _logger.LogInformation("Found {Count} active subscriptions for tenant: {TenantId}", 
                subscriptions.Count, notification.TenantId);

            // Send notification to each subscription
            var webPushClient = new WebPushClient();
            var successCount = 0;
            var failureCount = 0;

            var payload = JsonSerializer.Serialize(new
            {
                title = notification.Title,
                body = notification.Body,
                icon = notification.Icon ?? "/logo192.png",
                badge = notification.Badge ?? "/logo192.png",
                tag = notification.Tag ?? "default",
                data = notification.Data
            });

            foreach (var subscription in subscriptions)
            {
                try
                {
                    var pushSubscription = new WebPush.PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth
                    );

                    await webPushClient.SendNotificationAsync(pushSubscription, payload, _vapidDetails);
                    
                    // Update last used timestamp
                    subscription.LastUsedAt = DateTime.UtcNow;
                    await _subscriptionsContainer.UpsertItemAsync(subscription, new PartitionKey(subscription.TenantId));
                    
                    successCount++;
                }
                catch (WebPushException ex)
                {
                    _logger.LogError(ex, "Failed to send push notification to subscription: {SubscriptionId}", subscription.Id);
                    
                    // If subscription is invalid (410 Gone), mark as inactive
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                    {
                        subscription.IsActive = false;
                        await _subscriptionsContainer.UpsertItemAsync(subscription, new PartitionKey(subscription.TenantId));
                    }
                    
                    failureCount++;
                }
            }

            _logger.LogInformation("Sent push notifications: {Success} succeeded, {Failures} failed", 
                successCount, failureCount);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                sent = successCount,
                failed = failureCount,
                total = subscriptions.Count
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}

public class PushSubscriptionRequest
{
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public string? Endpoint { get; set; }
    public PushKeys? Keys { get; set; }
}

public class PushKeys
{
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
}

public class PushSubscription
{
    public string Id { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public bool IsActive { get; set; }
}

public class NotificationRequest
{
    public string? TenantId { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? Icon { get; set; }
    public string? Badge { get; set; }
    public string? Tag { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}
