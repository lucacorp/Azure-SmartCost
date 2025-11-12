# Azure SmartCost - Troubleshooting Guide

## 📋 Table of Contents
- [Common Issues](#common-issues)
- [API Issues](#api-issues)
- [Database Issues](#database-issues)
- [Authentication Issues](#authentication-issues)
- [Cache Issues](#cache-issues)
- [Functions Issues](#functions-issues)
- [Frontend Issues](#frontend-issues)
- [Performance Issues](#performance-issues)
- [Deployment Issues](#deployment-issues)
- [Monitoring & Diagnostics](#monitoring--diagnostics)

---

## Common Issues

### Issue: 503 Service Unavailable

**Symptoms**: API returns 503 errors, app is unresponsive

**Possible Causes**:
1. App Service is stopped or restarting
2. App Service plan is overloaded
3. Database connection timeout
4. Redis cache unavailable

**Solutions**:

```bash
# Check App Service status
az webapp show \
  --name "app-smartcost-api-prod" \
  --resource-group "rg-smartcost-prod" \
  --query "state"

# Restart app
az webapp restart \
  --name "app-smartcost-api-prod" \
  --resource-group "rg-smartcost-prod"

# Check logs
az webapp log tail \
  --name "app-smartcost-api-prod" \
  --resource-group "rg-smartcost-prod"

# Check metrics
az monitor metrics list \
  --resource "/subscriptions/.../app-smartcost-api-prod" \
  --metric "CpuPercentage" \
  --start-time "2025-01-15T00:00:00Z" \
  --end-time "2025-01-15T23:59:59Z"
```

**Prevention**:
- Enable autoscaling
- Set up health checks
- Configure Application Insights alerts

---

### Issue: Slow Response Times

**Symptoms**: API responds slowly (>5 seconds)

**Diagnosis**:

```kql
// Application Insights Query
requests
| where timestamp > ago(1h)
| where duration > 5000
| summarize count() by operation_Name, resultCode
| order by count_ desc
```

**Common Bottlenecks**:
1. **Database queries**: Missing indexes, large scans
2. **Cache misses**: Low hit rate, expired data
3. **External API calls**: Azure Cost Management API timeout
4. **Memory pressure**: Garbage collection overhead

**Solutions**:

```bash
# Check cache hit rate
curl -X GET "https://app-smartcost-api-prod.azurewebsites.net/api/cache/stats" \
  -H "Authorization: Bearer $TOKEN" | jq '.hitRate'

# Expected: >90%

# Invalidate stale cache
curl -X DELETE "https://app-smartcost-api-prod.azurewebsites.net/api/cache/invalidate?pattern=tenant:*" \
  -H "Authorization: Bearer $TOKEN"

# Scale up App Service
az appservice plan update \
  --name "plan-smartcost-prod" \
  --resource-group "rg-smartcost-prod" \
  --sku P1V2
```

---

## API Issues

### Issue: 401 Unauthorized

**Symptoms**: API returns 401 even with valid token

**Diagnosis**:

```bash
# Decode JWT token
echo "eyJ0eXAiOiJKV1QiLCJhbGc..." | cut -d '.' -f 2 | base64 -d | jq

# Check expiration
# "exp": 1642248000 (Unix timestamp)
date -d @1642248000
```

**Common Causes**:
1. Token expired (>60 minutes old)
2. Invalid signature (wrong signing key)
3. Missing claims (tenantId, roles)
4. Azure AD configuration mismatch

**Solutions**:

```javascript
// Frontend: Refresh token
async function refreshAccessToken() {
  const refreshToken = localStorage.getItem('refreshToken');
  
  const response = await fetch('https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      client_id: process.env.REACT_APP_AZURE_AD_CLIENT_ID,
      grant_type: 'refresh_token',
      refresh_token: refreshToken,
      scope: 'openid profile email'
    })
  });
  
  const { access_token } = await response.json();
  localStorage.setItem('accessToken', access_token);
}
```

```csharp
// Backend: Validate token
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));

// Check appsettings.json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common", // or specific tenant ID
    "ClientId": "12345678-1234-1234-1234-123456789012",
    "Audience": "api://12345678-1234-1234-1234-123456789012"
  }
}
```

---

### Issue: 429 Too Many Requests

**Symptoms**: Rate limiting errors

**Response**:
```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded. Try again in 3600 seconds.",
    "retryAfter": 3600
  }
}
```

**Solutions**:

```csharp
// Implement exponential backoff
public async Task<T> RetryWithBackoff<T>(Func<Task<T>> action, int maxRetries = 3) {
  for (int i = 0; i < maxRetries; i++) {
    try {
      return await action();
    }
    catch (RateLimitException ex) {
      if (i == maxRetries - 1) throw;
      
      var delay = TimeSpan.FromSeconds(Math.Pow(2, i) * 5); // 5s, 10s, 20s
      await Task.Delay(delay);
    }
  }
}
```

```javascript
// Frontend: Respect retry-after header
async function fetchWithRetry(url, options = {}) {
  const response = await fetch(url, options);
  
  if (response.status === 429) {
    const retryAfter = response.headers.get('Retry-After') || 60;
    console.log(`Rate limited. Retrying in ${retryAfter}s...`);
    
    await new Promise(resolve => setTimeout(resolve, retryAfter * 1000));
    return fetchWithRetry(url, options);
  }
  
  return response;
}
```

---

## Database Issues

### Issue: Cosmos DB High RU Consumption

**Symptoms**: High costs, throttling (429 errors)

**Diagnosis**:

```bash
# Check RU consumption
az monitor metrics list \
  --resource "/subscriptions/.../Microsoft.DocumentDB/databaseAccounts/cosmos-smartcost-prod" \
  --metric "TotalRequestUnits" \
  --interval PT1H \
  --start-time "2025-01-15T00:00:00Z" \
  --end-time "2025-01-15T23:59:59Z"
```

```kql
// Application Insights
dependencies
| where type == "Azure DocumentDB"
| where duration > 1000
| summarize count() by name, resultCode
| order by count_ desc
```

**Common Causes**:
1. Missing indexes
2. Cross-partition queries
3. Large document scans
4. Hot partition key

**Solutions**:

```json
// Update indexing policy
{
  "indexingPolicy": {
    "automatic": true,
    "indexingMode": "consistent",
    "includedPaths": [
      { "path": "/tenantId/?" },
      { "path": "/createdAt/?" },
      { "path": "/subscriptionId/?" }
    ],
    "excludedPaths": [
      { "path": "/metadata/*" }
    ]
  }
}
```

```csharp
// Optimize queries
// ❌ BAD: Cross-partition query
var costs = await container.GetItemQueryIterator<Cost>(
  "SELECT * FROM c WHERE c.date >= '2025-01-01'"
).ReadNextAsync();

// ✅ GOOD: Partition-scoped query
var costs = await container.GetItemQueryIterator<Cost>(
  "SELECT * FROM c WHERE c.date >= '2025-01-01'",
  requestOptions: new QueryRequestOptions { 
    PartitionKey = new PartitionKey("tenant-123") 
  }
).ReadNextAsync();
```

---

### Issue: Connection Timeouts

**Symptoms**: `CosmosException: Request timeout`

**Solutions**:

```csharp
// Increase connection timeout
var cosmosClientOptions = new CosmosClientOptions {
  ConnectionMode = ConnectionMode.Direct, // Faster than Gateway
  MaxRetryAttemptsOnRateLimitedRequests = 9,
  MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
  RequestTimeout = TimeSpan.FromSeconds(60),
  OpenTcpConnectionTimeout = TimeSpan.FromSeconds(10)
};

var cosmosClient = new CosmosClient(connectionString, cosmosClientOptions);
```

```bash
# Check Cosmos DB firewall
az cosmosdb show \
  --name "cosmos-smartcost-prod" \
  --resource-group "rg-smartcost-prod" \
  --query "ipRules"

# Add App Service outbound IPs
APP_IPS=$(az webapp show \
  --name "app-smartcost-api-prod" \
  --resource-group "rg-smartcost-prod" \
  --query "outboundIpAddresses" -o tsv)

az cosmosdb update \
  --name "cosmos-smartcost-prod" \
  --resource-group "rg-smartcost-prod" \
  --ip-range-filter "$APP_IPS"
```

---

## Authentication Issues

### Issue: Azure AD Login Fails

**Symptoms**: "AADSTS" error codes

**Common Errors**:

| Error Code | Meaning | Solution |
|------------|---------|----------|
| AADSTS50058 | Silent sign-in failed | User needs to login interactively |
| AADSTS65001 | User consent required | Add admin consent or user consent |
| AADSTS700016 | Invalid client ID | Check Azure AD app registration |
| AADSTS7000215 | Invalid client secret | Rotate client secret |
| AADSTS90002 | Tenant not found | Check tenant ID |

**Solutions**:

```bash
# Verify app registration
az ad app show --id "12345678-1234-1234-1234-123456789012"

# Grant admin consent
az ad app permission admin-consent \
  --id "12345678-1234-1234-1234-123456789012"

# Reset client secret
az ad app credential reset \
  --id "12345678-1234-1234-1234-123456789012" \
  --years 2
```

---

### Issue: CORS Errors

**Symptoms**: Browser console shows CORS errors

```
Access to fetch at 'https://api.smartcost.com' from origin 
'https://app.smartcost.com' has been blocked by CORS policy
```

**Solutions**:

```csharp
// Program.cs
builder.Services.AddCors(options => {
  options.AddPolicy("AllowFrontend", policy => {
    policy
      .WithOrigins(
        "https://app.smartcost.com",
        "https://app-staging.smartcost.com",
        "http://localhost:3000" // Development
      )
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowCredentials();
  });
});

app.UseCors("AllowFrontend");
```

```bash
# Or via Azure CLI
az webapp cors add \
  --name "app-smartcost-api-prod" \
  --resource-group "rg-smartcost-prod" \
  --allowed-origins "https://app.smartcost.com"
```

---

## Cache Issues

### Issue: Redis Connection Failed

**Symptoms**: `RedisConnectionException: No connection is available`

**Diagnosis**:

```bash
# Test connection
redis-cli -h "redis-smartcost-prod.redis.cache.windows.net" \
  -p 6380 \
  -a "YOUR_ACCESS_KEY" \
  --tls ping

# Expected: PONG
```

**Solutions**:

```csharp
// Configure retry policy
var configOptions = ConfigurationOptions.Parse(connectionString);
configOptions.AbortOnConnectFail = false; // Don't throw on initial failure
configOptions.ConnectTimeout = 5000;
configOptions.SyncTimeout = 5000;
configOptions.ConnectRetry = 3;
configOptions.ReconnectRetryPolicy = new ExponentialRetry(1000);

var redis = ConnectionMultiplexer.Connect(configOptions);
```

```bash
# Check firewall rules
az redis firewall-rules list \
  --name "redis-smartcost-prod" \
  --resource-group "rg-smartcost-prod"

# Add App Service IPs
az redis firewall-rules create \
  --name "app-service" \
  --redis-name "redis-smartcost-prod" \
  --resource-group "rg-smartcost-prod" \
  --start-ip "10.0.0.1" \
  --end-ip "10.0.0.10"
```

---

### Issue: Low Cache Hit Rate

**Symptoms**: Hit rate <70%, slow response times

**Diagnosis**:

```bash
curl -X GET "https://api.smartcost.com/api/cache/stats" \
  -H "Authorization: Bearer $TOKEN"

# {
#   "hitRate": 45.2,  // Should be >90%
#   "hitCount": 1234,
#   "missCount": 2000
# }
```

**Causes**:
1. TTL too short
2. Cache invalidated too frequently
3. Cache keys not consistent
4. Redis memory full (evictions)

**Solutions**:

```csharp
// Increase TTL for stable data
await _cacheService.SetAsync(
  CacheKeys.Tenant(tenantId), 
  tenant, 
  TimeSpan.FromMinutes(60) // Increased from 15
);

// Check memory usage
var stats = await _cacheService.GetStatisticsAsync();
if (stats.UsedMemoryBytes > 0.8 * maxMemory) {
  _logger.LogWarning("Redis memory usage high: {percent}%", 
    stats.UsedMemoryBytes * 100 / maxMemory);
}

// Selective invalidation (don't flush all)
await _cacheService.InvalidatePatternAsync($"tenant:{tenantId}:costs:*");
// Instead of: await _cacheService.FlushAllAsync();
```

---

## Functions Issues

### Issue: Function Not Triggering

**Symptoms**: Timer function doesn't execute on schedule

**Diagnosis**:

```bash
# Check function status
az functionapp function show \
  --name "func-smartcost-prod" \
  --resource-group "rg-smartcost-prod" \
  --function-name "CollectCostData"

# View logs
func azure functionapp logstream "func-smartcost-prod"
```

**Common Causes**:
1. Function app stopped
2. Invalid CRON expression
3. Timeout (default: 5 minutes)
4. Missing configuration

**Solutions**:

```json
// function.json - Fix CRON expression
{
  "bindings": [
    {
      "name": "timer",
      "type": "timerTrigger",
      "schedule": "0 */6 * * *", // Every 6 hours
      "runOnStartup": false,
      "useMonitor": true
    }
  ]
}

// CRON examples:
// "0 */1 * * *"   -> Every hour
// "0 0 * * *"     -> Daily at midnight
// "0 0 * * 0"     -> Weekly on Sunday
// "0 0 1 * *"     -> Monthly on 1st
```

```json
// host.json - Increase timeout
{
  "version": "2.0",
  "functionTimeout": "00:10:00", // 10 minutes
  "logging": {
    "logLevel": {
      "Function": "Information"
    }
  }
}
```

---

### Issue: Function Execution Failed

**Symptoms**: Function runs but fails with exception

**Diagnosis**:

```kql
// Application Insights
traces
| where message contains "CollectCostData"
| where severityLevel >= 3 // Warning or Error
| project timestamp, message, severityLevel
| order by timestamp desc
```

**Solutions**:

```csharp
// Add error handling
[Function("CollectCostData")]
public async Task Run([TimerTrigger("0 */6 * * *")] TimerInfo timer) {
  try {
    _logger.LogInformation("Starting cost collection at {time}", DateTime.UtcNow);
    
    await CollectCostsAsync();
    
    _logger.LogInformation("Cost collection completed successfully");
  }
  catch (Exception ex) {
    _logger.LogError(ex, "Cost collection failed: {error}", ex.Message);
    
    // Send alert
    await _alertService.CreateAlert(new Alert {
      Type = "system",
      Severity = "critical",
      Message = $"Cost collection failed: {ex.Message}"
    });
    
    throw; // Re-throw for retry
  }
}
```

---

## Frontend Issues

### Issue: PWA Not Installing

**Symptoms**: Install prompt doesn't appear

**Diagnosis**:

```javascript
// Check service worker registration
navigator.serviceWorker.getRegistrations().then(registrations => {
  console.log('Service Workers:', registrations.length);
  registrations.forEach(reg => {
    console.log('  Scope:', reg.scope);
    console.log('  Active:', reg.active);
  });
});

// Check manifest
fetch('/manifest.json')
  .then(r => r.json())
  .then(manifest => console.log('Manifest:', manifest));
```

**Common Causes**:
1. Not served over HTTPS
2. Invalid manifest.json
3. Service worker not registered
4. Already installed
5. iOS doesn't support automatic install

**Solutions**:

```javascript
// Debug beforeinstallprompt
window.addEventListener('beforeinstallprompt', (e) => {
  console.log('Install prompt triggered!', e);
  e.preventDefault();
  // Store for later use
});

// Check if already installed
if (window.matchMedia('(display-mode: standalone)').matches) {
  console.log('App is already installed');
}

// iOS detection
const isIOS = /iphone|ipad|ipod/.test(navigator.userAgent.toLowerCase());
if (isIOS) {
  console.log('iOS detected - show manual install instructions');
}
```

**Lighthouse PWA Audit**:
```bash
npm install -g lighthouse

lighthouse https://app.smartcost.com \
  --only-categories=pwa \
  --output=html \
  --output-path=./pwa-report.html
```

---

### Issue: Offline Mode Not Working

**Symptoms**: App shows error when offline

**Diagnosis**:

```javascript
// Chrome DevTools > Application > Service Workers
// Click "Offline" checkbox and reload

// Check cache
caches.keys().then(keys => {
  console.log('Cache Keys:', keys);
  keys.forEach(key => {
    caches.open(key).then(cache => {
      cache.keys().then(requests => {
        console.log(`  ${key}:`, requests.length, 'items');
      });
    });
  });
});
```

**Solutions**:

```javascript
// service-worker.js - Ensure install caches assets
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then(cache => {
      console.log('Caching assets...');
      return cache.addAll(STATIC_ASSETS)
        .then(() => console.log('Assets cached successfully'))
        .catch(err => console.error('Cache failed:', err));
    })
  );
});

// Verify fetch handler
self.addEventListener('fetch', (event) => {
  console.log('Fetch:', event.request.url);
  
  // Add comprehensive offline fallback
  event.respondWith(
    fetch(event.request)
      .catch(() => caches.match(event.request))
      .catch(() => caches.match('/offline.html'))
  );
});
```

---

## Performance Issues

### Issue: Large Bundle Size

**Symptoms**: Slow initial load, large JavaScript files

**Diagnosis**:

```bash
# Analyze bundle
npm run build
npx source-map-explorer 'build/static/js/*.js'
```

**Solutions**:

```javascript
// Code splitting with React.lazy
import React, { lazy, Suspense } from 'react';

const Dashboard = lazy(() => import('./components/Dashboard'));
const Analytics = lazy(() => import('./components/Analytics'));

function App() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <Routes>
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/analytics" element={<Analytics />} />
      </Routes>
    </Suspense>
  );
}

// Tree shaking - import only what you need
// ❌ BAD
import _ from 'lodash';
_.debounce(fn, 300);

// ✅ GOOD
import debounce from 'lodash/debounce';
debounce(fn, 300);
```

---

## Deployment Issues

### Issue: Bicep Deployment Failed

**Symptoms**: `az deployment group create` fails

**Common Errors**:

```bash
# Error: Resource already exists
Code: ResourceAlreadyExists
Message: The resource with name 'cosmos-smartcost-prod' already exists

# Solution: Use unique suffix
az deployment group create \
  --parameters uniqueSuffix=$(openssl rand -hex 3)

# Error: Insufficient permissions
Code: AuthorizationFailed
Message: The client does not have authorization to perform action

# Solution: Check role assignment
az role assignment list --assignee "user@domain.com"
az role assignment create \
  --assignee "user@domain.com" \
  --role "Contributor" \
  --scope "/subscriptions/{subscription-id}/resourceGroups/rg-smartcost-prod"
```

---

## Monitoring & Diagnostics

### Application Insights Queries

```kql
// Top 10 slowest requests
requests
| where timestamp > ago(24h)
| summarize avg(duration), count() by operation_Name
| top 10 by avg_duration desc

// Error rate by endpoint
requests
| where timestamp > ago(1h)
| summarize 
    Total = count(),
    Errors = countif(success == false)
  by operation_Name
| extend ErrorRate = (Errors * 100.0) / Total
| where ErrorRate > 5
| order by ErrorRate desc

// Dependency failures
dependencies
| where timestamp > ago(24h)
| where success == false
| summarize count() by type, name, resultCode
| order by count_ desc

// Custom events tracking
customEvents
| where name == "CostDataCollected"
| summarize count() by bin(timestamp, 1h)
| render timechart
```

### Health Check Endpoint

```bash
# Monitor health
curl https://api.smartcost.com/api/health

# Expected response:
{
  "status": "healthy",
  "services": {
    "database": { "status": "healthy", "responseTime": 12 },
    "cache": { "status": "healthy", "responseTime": 5 }
  }
}

# Set up monitoring
az monitor metrics alert create \
  --name "api-health-alert" \
  --resource-group "rg-smartcost-prod" \
  --scopes "/subscriptions/.../app-smartcost-api-prod" \
  --condition "avg Percentage HTTP 5xx > 5" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action-group "/subscriptions/.../actionGroups/ops-team"
```

---

## Getting Help

### Support Channels
- **Email**: support@smartcost.com
- **Documentation**: https://docs.smartcost.com
- **GitHub Issues**: https://github.com/smartcost/Azure-SmartCost/issues
- **Stack Overflow**: Tag `azure-smartcost`

### Diagnostic Information to Include
When reporting issues, include:
1. Error messages (full stack trace)
2. Application Insights trace ID
3. Timestamp of occurrence
4. Steps to reproduce
5. Environment (dev/staging/prod)
6. Recent changes/deployments

```bash
# Generate diagnostic report
az webapp log download \
  --name "app-smartcost-api-prod" \
  --resource-group "rg-smartcost-prod" \
  --log-file "diagnostics-$(date +%Y%m%d).zip"
```

---

**Last Updated**: January 2025  
**Version**: 2.0  
**Support Team**: Azure SmartCost
