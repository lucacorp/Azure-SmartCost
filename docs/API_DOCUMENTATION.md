# Azure SmartCost - API Documentation

## 📋 Table of Contents
- [Overview](#overview)
- [Authentication](#authentication)
- [Base URL](#base-url)
- [Endpoints](#endpoints)
  - [Tenants](#tenants)
  - [Costs](#costs)
  - [Budgets](#budgets)
  - [Alerts](#alerts)
  - [Analytics](#analytics)
  - [Marketplace](#marketplace)
  - [Cache](#cache)
  - [Health](#health)
- [Error Handling](#error-handling)
- [Rate Limiting](#rate-limiting)
- [Webhooks](#webhooks)

---

## Overview

Azure SmartCost API is a RESTful API that provides comprehensive Azure cost management and FinOps capabilities.

**API Version**: v1  
**Protocol**: HTTPS  
**Format**: JSON  
**Authentication**: Bearer Token (Azure AD) or API Key

---

## Authentication

### Azure AD OAuth 2.0 (Recommended)

```http
POST https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id={client-id}
&client_secret={client-secret}
&scope=https://smartcost.azurewebsites.net/.default
```

**Response:**
```json
{
  "token_type": "Bearer",
  "expires_in": 3599,
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGc..."
}
```

**Usage:**
```http
GET /api/tenants
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

### API Key (Alternative)

```http
GET /api/tenants
X-API-Key: sk_live_abcdef123456
```

---

## Base URL

| Environment | URL |
|-------------|-----|
| Production | `https://api.smartcost.com` |
| Staging | `https://api-staging.smartcost.com` |
| Development | `http://localhost:5000` |

---

## Endpoints

### Tenants

#### GET /api/tenants
Get all tenants for the authenticated user.

**Authorization**: Required (User role)

**Response:**
```json
{
  "tenants": [
    {
      "id": "tenant-123",
      "name": "Contoso Ltd",
      "subscriptionTier": "premium",
      "azureAdTenantId": "12345678-1234-1234-1234-123456789012",
      "subscriptions": [
        {
          "id": "sub-456",
          "subscriptionId": "87654321-4321-4321-4321-876543210987",
          "name": "Production",
          "isActive": true
        }
      ],
      "createdAt": "2025-01-01T00:00:00Z",
      "updatedAt": "2025-01-15T10:30:00Z"
    }
  ]
}
```

#### GET /api/tenants/{id}
Get tenant by ID.

**Parameters:**
- `id` (path, required): Tenant ID

**Response:**
```json
{
  "id": "tenant-123",
  "name": "Contoso Ltd",
  "subscriptionTier": "premium",
  "azureAdTenantId": "12345678-1234-1234-1234-123456789012",
  "subscriptions": [...],
  "users": [
    {
      "userId": "user-789",
      "email": "admin@contoso.com",
      "role": "admin",
      "lastLogin": "2025-01-15T09:00:00Z"
    }
  ],
  "settings": {
    "currency": "USD",
    "timezone": "America/New_York",
    "alertsEnabled": true
  }
}
```

#### POST /api/tenants
Create new tenant.

**Authorization**: Admin

**Request Body:**
```json
{
  "name": "Contoso Ltd",
  "azureAdTenantId": "12345678-1234-1234-1234-123456789012",
  "subscriptionTier": "free",
  "settings": {
    "currency": "USD",
    "timezone": "UTC"
  }
}
```

**Response:** `201 Created`
```json
{
  "id": "tenant-123",
  "name": "Contoso Ltd",
  "createdAt": "2025-01-15T10:00:00Z"
}
```

#### PUT /api/tenants/{id}
Update tenant.

**Request Body:**
```json
{
  "name": "Contoso Corporation",
  "settings": {
    "alertsEnabled": false
  }
}
```

**Response:** `200 OK`

#### DELETE /api/tenants/{id}
Delete tenant.

**Response:** `204 No Content`

---

### Costs

#### GET /api/costs
Get cost data for tenant.

**Query Parameters:**
- `tenantId` (required): Tenant ID
- `subscriptionId` (optional): Filter by Azure subscription
- `startDate` (required): Start date (ISO 8601)
- `endDate` (required): End date (ISO 8601)
- `granularity` (optional): `daily` | `monthly` (default: `daily`)
- `groupBy` (optional): `resourceGroup` | `service` | `location`

**Example:**
```http
GET /api/costs?tenantId=tenant-123&startDate=2025-01-01&endDate=2025-01-31&granularity=daily
```

**Response:**
```json
{
  "costs": [
    {
      "date": "2025-01-15",
      "totalCost": 1247.89,
      "currency": "USD",
      "breakdown": {
        "compute": 856.32,
        "storage": 124.67,
        "network": 89.45,
        "database": 177.45
      },
      "byResourceGroup": {
        "rg-production": 987.65,
        "rg-development": 260.24
      }
    }
  ],
  "summary": {
    "totalCost": 38567.23,
    "avgDailyCost": 1244.75,
    "trend": "increasing",
    "changePercent": 12.5
  }
}
```

#### GET /api/costs/forecast
Get cost forecast.

**Query Parameters:**
- `tenantId` (required)
- `subscriptionId` (optional)
- `days` (optional): Forecast period (default: 30)

**Response:**
```json
{
  "forecast": [
    {
      "date": "2025-02-01",
      "predictedCost": 1289.45,
      "confidenceInterval": {
        "lower": 1150.23,
        "upper": 1428.67
      }
    }
  ],
  "monthlyEstimate": 38683.50,
  "variance": 0.15
}
```

---

### Budgets

#### GET /api/budgets
Get all budgets for tenant.

**Response:**
```json
{
  "budgets": [
    {
      "id": "budget-456",
      "tenantId": "tenant-123",
      "name": "Monthly Production Budget",
      "amount": 50000.00,
      "period": "monthly",
      "startDate": "2025-01-01",
      "endDate": "2025-12-31",
      "currentSpend": 38567.23,
      "utilization": 77.13,
      "status": "warning",
      "thresholds": [
        { "percentage": 50, "notified": true },
        { "percentage": 80, "notified": false },
        { "percentage": 100, "notified": false }
      ]
    }
  ]
}
```

#### POST /api/budgets
Create budget.

**Request Body:**
```json
{
  "tenantId": "tenant-123",
  "name": "Q1 Development Budget",
  "amount": 10000.00,
  "period": "quarterly",
  "startDate": "2025-01-01",
  "endDate": "2025-03-31",
  "thresholds": [50, 80, 90, 100],
  "notificationEmails": ["admin@contoso.com"]
}
```

**Response:** `201 Created`

#### PUT /api/budgets/{id}
Update budget.

#### DELETE /api/budgets/{id}
Delete budget.

---

### Alerts

#### GET /api/alerts
Get all alerts.

**Query Parameters:**
- `tenantId` (required)
- `status` (optional): `active` | `resolved` | `all` (default: `active`)
- `severity` (optional): `critical` | `warning` | `info`
- `type` (optional): `budget` | `anomaly` | `threshold`

**Response:**
```json
{
  "alerts": [
    {
      "id": "alert-789",
      "tenantId": "tenant-123",
      "type": "budget",
      "severity": "critical",
      "title": "Budget threshold exceeded",
      "message": "Monthly budget has reached 95% utilization",
      "currentValue": 47500.00,
      "threshold": 45000.00,
      "createdAt": "2025-01-15T14:30:00Z",
      "status": "active",
      "acknowledgedBy": null,
      "resolvedAt": null
    }
  ]
}
```

#### POST /api/alerts
Create custom alert.

**Request Body:**
```json
{
  "tenantId": "tenant-123",
  "type": "threshold",
  "name": "Daily Cost Alert",
  "condition": {
    "metric": "dailyCost",
    "operator": "greaterThan",
    "value": 2000.00
  },
  "notifications": {
    "email": ["ops@contoso.com"],
    "webhook": "https://hooks.slack.com/services/...",
    "pushNotification": true
  }
}
```

#### PUT /api/alerts/{id}/acknowledge
Acknowledge alert.

**Response:** `200 OK`

#### PUT /api/alerts/{id}/resolve
Resolve alert.

---

### Analytics

#### GET /api/analytics/trends
Get cost trends analysis.

**Query Parameters:**
- `tenantId` (required)
- `period` (optional): `7d` | `30d` | `90d` (default: `30d`)

**Response:**
```json
{
  "trends": {
    "overall": {
      "direction": "increasing",
      "changePercent": 15.3,
      "movingAverage": 1244.75
    },
    "byService": [
      {
        "service": "Virtual Machines",
        "trend": "stable",
        "changePercent": 2.1
      },
      {
        "service": "SQL Database",
        "trend": "increasing",
        "changePercent": 45.7
      }
    ]
  },
  "recommendations": [
    {
      "type": "rightsizing",
      "resource": "vm-prod-web-01",
      "currentCost": 450.00,
      "potentialSavings": 180.00,
      "confidence": "high"
    }
  ]
}
```

#### GET /api/analytics/anomalies
Detect cost anomalies.

**Response:**
```json
{
  "anomalies": [
    {
      "date": "2025-01-14",
      "resource": "Storage Account",
      "expectedCost": 120.00,
      "actualCost": 450.00,
      "deviation": 275.0,
      "severity": "high",
      "possibleCauses": [
        "Unusual data egress",
        "Snapshot retention increased"
      ]
    }
  ]
}
```

---

### Marketplace

#### POST /api/marketplace/subscription
Handle marketplace subscription (webhook).

**Request Body** (from Azure Marketplace):
```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "publisherId": "contoso",
  "offerId": "smartcost",
  "planId": "premium",
  "quantity": 1,
  "subscription": {
    "id": "sub-marketplace-123",
    "name": "Contoso SmartCost"
  },
  "purchaser": {
    "emailId": "buyer@contoso.com",
    "objectId": "87654321-4321-4321-4321-876543210987",
    "tenantId": "12345678-1234-1234-1234-123456789012"
  },
  "action": "Subscribe"
}
```

**Response:** `200 OK`

#### GET /api/marketplace/subscription/{id}
Get marketplace subscription status.

---

### Cache

#### GET /api/cache/stats
Get cache statistics.

**Authorization**: Required

**Response:**
```json
{
  "totalKeys": 1247,
  "usedMemoryBytes": 52428800,
  "hitCount": 15678,
  "missCount": 1234,
  "hitRate": 92.7,
  "isConnected": true,
  "version": "7.2.4"
}
```

#### DELETE /api/cache/invalidate
Invalidate cache by pattern.

**Authorization**: Admin

**Query Parameters:**
- `pattern` (required): Redis key pattern (e.g., `tenant:123:*`)

**Response:** `200 OK`
```json
{
  "deletedKeys": 45
}
```

#### DELETE /api/cache/flush
Flush all cache.

**Authorization**: Admin

**Query Parameters:**
- `confirm` (required): Must be `true`

**Response:** `200 OK`

---

### Health

#### GET /api/health
API health check.

**Response:**
```json
{
  "status": "healthy",
  "version": "2.0.0",
  "timestamp": "2025-01-15T10:00:00Z",
  "services": {
    "database": {
      "status": "healthy",
      "responseTime": 12
    },
    "cache": {
      "status": "healthy",
      "responseTime": 5
    },
    "storage": {
      "status": "healthy",
      "responseTime": 8
    }
  }
}
```

---

## Error Handling

### Standard Error Response

```json
{
  "error": {
    "code": "RESOURCE_NOT_FOUND",
    "message": "Tenant with ID 'tenant-999' not found",
    "timestamp": "2025-01-15T10:00:00Z",
    "traceId": "abc123def456",
    "details": {
      "tenantId": "tenant-999"
    }
  }
}
```

### HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created |
| 204 | No Content |
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 409 | Conflict |
| 429 | Too Many Requests |
| 500 | Internal Server Error |
| 503 | Service Unavailable |

### Error Codes

| Code | Description |
|------|-------------|
| `INVALID_REQUEST` | Request validation failed |
| `UNAUTHORIZED` | Authentication required |
| `FORBIDDEN` | Insufficient permissions |
| `RESOURCE_NOT_FOUND` | Resource doesn't exist |
| `DUPLICATE_RESOURCE` | Resource already exists |
| `RATE_LIMIT_EXCEEDED` | Too many requests |
| `SERVICE_UNAVAILABLE` | Service temporarily unavailable |
| `INTERNAL_ERROR` | Unexpected server error |

---

## Rate Limiting

**Limits:**
- **Free Tier**: 100 requests/hour
- **Basic Tier**: 1,000 requests/hour
- **Premium Tier**: 10,000 requests/hour

**Headers:**
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 847
X-RateLimit-Reset: 1642248000
```

**429 Response:**
```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded. Try again in 3600 seconds.",
    "retryAfter": 3600
  }
}
```

---

## Webhooks

### Stripe Webhooks

**Endpoint**: `POST /api/stripe/webhook`

**Events:**
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.payment_succeeded`
- `invoice.payment_failed`

**Signature Verification:**
```csharp
var signature = Request.Headers["Stripe-Signature"];
var webhookSecret = Configuration["Stripe:WebhookSecret"];
var stripeEvent = EventUtility.ConstructEvent(
    requestBody, 
    signature, 
    webhookSecret
);
```

### Custom Webhooks

Configure custom webhooks for alerts:

```http
POST /api/webhooks
```

**Request Body:**
```json
{
  "url": "https://hooks.slack.com/services/...",
  "events": ["alert.created", "budget.exceeded"],
  "secret": "your-webhook-secret"
}
```

**Payload Example:**
```json
{
  "event": "alert.created",
  "timestamp": "2025-01-15T10:00:00Z",
  "data": {
    "alertId": "alert-789",
    "severity": "critical",
    "message": "Budget threshold exceeded"
  },
  "signature": "sha256=abcdef123456..."
}
```

---

## SDK Examples

### C# (.NET)
```csharp
using Azure.Identity;
using Microsoft.Rest;

var credential = new DefaultAzureCredential();
var token = await credential.GetTokenAsync(
    new TokenRequestContext(new[] { "https://smartcost.azurewebsites.net/.default" })
);

var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token.Token);

var response = await client.GetAsync("https://api.smartcost.com/api/tenants");
var tenants = await response.Content.ReadAsAsync<TenantsResponse>();
```

### JavaScript (Node.js)
```javascript
const axios = require('axios');

const client = axios.create({
  baseURL: 'https://api.smartcost.com',
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json'
  }
});

const tenants = await client.get('/api/tenants');
console.log(tenants.data);
```

### Python
```python
import requests

headers = {
    'Authorization': f'Bearer {access_token}',
    'Content-Type': 'application/json'
}

response = requests.get(
    'https://api.smartcost.com/api/tenants',
    headers=headers
)

tenants = response.json()
```

---

**Last Updated**: January 2025  
**API Version**: 2.0  
**Contact**: api-support@smartcost.com
