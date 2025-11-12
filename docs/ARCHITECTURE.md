# Azure SmartCost - Architecture Overview

## 📋 Table of Contents
- [System Overview](#system-overview)
- [Architecture Diagram](#architecture-diagram)
- [Components](#components)
- [Data Flow](#data-flow)
- [Security Architecture](#security-architecture)
- [Scalability & Performance](#scalability--performance)
- [Disaster Recovery](#disaster-recovery)
- [Technology Stack](#technology-stack)

---

## System Overview

Azure SmartCost is a cloud-native FinOps platform built on Microsoft Azure, designed to help organizations monitor, optimize, and manage their Azure cloud spending.

### Key Characteristics
- **Multi-tenant**: Supports multiple organizations with data isolation
- **Serverless**: Leverages Azure Functions and consumption-based services
- **Real-time**: Live cost monitoring and instant alerts
- **Scalable**: Auto-scales based on demand
- **Secure**: Azure AD integration, Key Vault, and encryption at rest/transit

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                           CLIENT LAYER                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │   Web App    │  │  Mobile PWA  │  │   Power BI   │              │
│  │  (React SPA) │  │ (Offline 1st)│  │  Dashboards  │              │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘              │
│         │                  │                  │                       │
└─────────┼──────────────────┼──────────────────┼───────────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      API GATEWAY / CDN                               │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Azure Front Door / Application Gateway                      │   │
│  │  - SSL Termination                                           │   │
│  │  - WAF (Web Application Firewall)                            │   │
│  │  - Rate Limiting                                              │   │
│  │  - Geographic routing                                         │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────┼───────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │              Azure App Service (API)                        │    │
│  │  ┌──────────────────┐  ┌──────────────────┐                │    │
│  │  │ TenantService    │  │ CostService      │                │    │
│  │  │ - Multi-tenancy  │  │ - Cost analysis  │                │    │
│  │  │ - User mgmt      │  │ - Forecasting    │                │    │
│  │  └──────────────────┘  └──────────────────┘                │    │
│  │  ┌──────────────────┐  ┌──────────────────┐                │    │
│  │  │ BudgetService    │  │ AlertService     │                │    │
│  │  │ - Budget tracking│  │ - Notifications  │                │    │
│  │  │ - Thresholds     │  │ - Anomaly detect │                │    │
│  │  └──────────────────┘  └──────────────────┘                │    │
│  │  ┌──────────────────┐  ┌──────────────────┐                │    │
│  │  │ AnalyticsService │  │ ReportingService │                │    │
│  │  │ - Trends         │  │ - Power BI embed │                │    │
│  │  │ - Predictions    │  │ - PDF generation │                │    │
│  │  └──────────────────┘  └──────────────────┘                │    │
│  └────────────────────────────────────────────────────────────┘    │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │              Azure Functions (Serverless)                   │    │
│  │  ┌─────────────────────────────────────────────────────┐   │    │
│  │  │  CollectCostData (Timer: 0 */6 * * *)               │   │    │
│  │  │  - Fetch Azure Cost Management API                   │   │    │
│  │  │  - Store in Cosmos DB                                │   │    │
│  │  │  - Update cache                                      │   │    │
│  │  └─────────────────────────────────────────────────────┘   │    │
│  │  ┌─────────────────────────────────────────────────────┐   │    │
│  │  │  ProcessAlerts (Timer: */15 * * * *)                │   │    │
│  │  │  - Evaluate budget thresholds                        │   │    │
│  │  │  - Detect anomalies                                  │   │    │
│  │  │  - Send notifications                                │   │    │
│  │  └─────────────────────────────────────────────────────┘   │    │
│  │  ┌─────────────────────────────────────────────────────┐   │    │
│  │  │  GenerateReports (Queue trigger)                     │   │    │
│  │  │  - Export data to CSV/PDF                            │   │    │
│  │  │  - Upload to Blob Storage                            │   │    │
│  │  └─────────────────────────────────────────────────────┘   │    │
│  └────────────────────────────────────────────────────────────┘    │
│                                                                       │
└───────────┬──────────────────┬────────────────────┬──────────────────┘
            │                  │                    │
            ▼                  ▼                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        DATA LAYER                                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────┐   │
│  │  Cosmos DB       │  │  Redis Cache     │  │  Blob Storage   │   │
│  │  (NoSQL)         │  │  (Distributed)   │  │  (Files)        │   │
│  │                  │  │                  │  │                 │   │
│  │  Containers:     │  │  Cache:          │  │  Containers:    │   │
│  │  - Tenants       │  │  - Tenant data   │  │  - Reports      │   │
│  │  - Users         │  │  - Cost data     │  │  - Exports      │   │
│  │  - Costs         │  │  - Analytics     │  │  - Logs         │   │
│  │  - Budgets       │  │  - Sessions      │  │  - Backups      │   │
│  │  - Alerts        │  │                  │  │                 │   │
│  │  - Subscriptions │  │  TTL: 15-60min   │  │  Lifecycle:     │   │
│  │                  │  │  Eviction: LRU   │  │  - Hot (30d)    │   │
│  │  Partition: /id  │  │                  │  │  - Cool (90d)   │   │
│  │  Consistency:    │  │  Replication:    │  │  - Archive (1y) │   │
│  │  Session         │  │  Active-Active   │  │                 │   │
│  └──────────────────┘  └──────────────────┘  └─────────────────┘   │
│                                                                       │
└─────────┬─────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     INTEGRATION LAYER                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────┐   │
│  │  Azure Cost      │  │  Azure AD        │  │  Stripe         │   │
│  │  Management API  │  │  (Authentication)│  │  (Payments)     │   │
│  └──────────────────┘  └──────────────────┘  └─────────────────┘   │
│                                                                       │
│  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────┐   │
│  │  Azure           │  │  SendGrid        │  │  Power BI       │   │
│  │  Marketplace     │  │  (Email)         │  │  Embedded       │   │
│  └──────────────────┘  └──────────────────┘  └─────────────────┘   │
│                                                                       │
└───────────────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    SECURITY & MONITORING                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────┐   │
│  │  Key Vault       │  │  Application     │  │  Log Analytics  │   │
│  │  (Secrets)       │  │  Insights        │  │  (Logs)         │   │
│  │                  │  │  (Monitoring)    │  │                 │   │
│  │  Secrets:        │  │                  │  │  Workspaces:    │   │
│  │  - API keys      │  │  Metrics:        │  │  - API logs     │   │
│  │  - Conn strings  │  │  - Response time │  │  - Function logs│   │
│  │  - Certificates  │  │  - Error rate    │  │  - Audit logs   │   │
│  │                  │  │  - Throughput    │  │  - Security logs│   │
│  │  Rotation: 90d   │  │  - Availability  │  │                 │   │
│  └──────────────────┘  └──────────────────┘  └─────────────────┘   │
│                                                                       │
│  ┌──────────────────┐  ┌──────────────────┐                         │
│  │  Azure Monitor   │  │  Azure Sentinel  │                         │
│  │  (Alerts)        │  │  (SIEM)          │                         │
│  └──────────────────┘  └──────────────────┘                         │
│                                                                       │
└───────────────────────────────────────────────────────────────────────┘
```

---

## Components

### Frontend Layer

#### React Web Application
- **Technology**: React 18 + TypeScript
- **State Management**: React Context API
- **Routing**: React Router v6
- **UI Framework**: Material-UI / Chakra UI
- **Charts**: Recharts / Chart.js
- **Build Tool**: Create React App / Vite
- **Deployment**: Azure Storage (Static Website) + CDN

**Features**:
- Server-side rendering ready
- Progressive Web App (PWA) capabilities
- Offline support via Service Worker
- Push notifications
- Responsive design (mobile-first)

#### Mobile PWA
- **Install Prompt**: Custom component for iOS/Android
- **Offline Strategy**: NetworkFirst for API, CacheFirst for assets
- **Background Sync**: Queue offline actions
- **Push Notifications**: Web Push API with VAPID

### Backend Layer

#### API (Azure App Service)
- **Framework**: ASP.NET Core 8.0
- **Architecture**: Clean Architecture / Onion Architecture
- **Layers**:
  - **Controllers**: REST endpoints
  - **Services**: Business logic
  - **Repositories**: Data access
  - **Models**: DTOs and domain entities

**Design Patterns**:
- Repository Pattern (data access)
- Dependency Injection (IoC)
- Factory Pattern (service creation)
- Strategy Pattern (cost calculation algorithms)
- Observer Pattern (event notifications)

**Middleware**:
- Authentication (JWT Bearer)
- Authorization (Role-based)
- Exception Handling
- Request Logging
- Rate Limiting
- CORS
- Compression

#### Functions (Azure Functions)
- **Runtime**: .NET 8 Isolated
- **Triggers**:
  - Timer (cost collection every 6 hours)
  - Queue (report generation)
  - HTTP (webhooks)

**Functions**:
1. **CollectCostData**: Fetch Azure Cost Management API → Store in Cosmos DB
2. **ProcessAlerts**: Evaluate budgets → Send notifications
3. **GenerateReports**: Export data → Upload to Blob Storage
4. **SyncAzureAdGroups**: Sync Azure AD group memberships
5. **CleanupExpiredData**: Purge old data (retention policy)

### Data Layer

#### Cosmos DB (Primary Database)
- **API**: Core (SQL)
- **Consistency**: Session (balance of performance and consistency)
- **Partition Strategy**: 
  - Tenants: `/id`
  - Costs: `/tenantId`
  - Users: `/tenantId`
- **Indexing**: Automatic with custom policies
- **TTL**: Enabled for temporary data (sessions, cache)
- **Backup**: Continuous (4 hours interval, 30 days retention)

**Containers**:
```
SmartCostDB
├── Tenants (partition: /id)
├── Users (partition: /tenantId)
├── Costs (partition: /tenantId, TTL: 2 years)
├── Budgets (partition: /tenantId)
├── Alerts (partition: /tenantId)
├── Subscriptions (partition: /tenantId)
├── MarketplaceSubscriptions (partition: /id)
└── AuditLogs (partition: /tenantId, TTL: 90 days)
```

#### Redis Cache
- **SKU**: Standard C1 (1GB) for production
- **Eviction Policy**: LRU (Least Recently Used)
- **Replication**: Active-Active (geo-replication for premium)
- **Persistence**: AOF (Append-only file) + RDB snapshots

**Cache Keys**:
```
tenant:{tenantId}                          TTL: 15min
tenant:{tenantId}:users                    TTL: 30min
tenant:{tenantId}:costs:{subscriptionId}   TTL: 60min
tenant:{tenantId}:analytics:{period}       TTL: 60min
tenant:{tenantId}:budgets                  TTL: 15min
session:{sessionId}                        TTL: 30min
```

#### Blob Storage
- **Tiers**:
  - Hot: Active reports (<30 days)
  - Cool: Archived reports (30-90 days)
  - Archive: Long-term storage (>90 days)
- **Lifecycle Management**: Auto-transition between tiers
- **Containers**:
  - `$web`: Static website hosting (frontend)
  - `reports`: Generated PDF/CSV reports
  - `exports`: Data exports
  - `backups`: Database backups

---

## Data Flow

### Cost Collection Flow
```
1. Timer Trigger (every 6 hours)
   ↓
2. Azure Function: CollectCostData
   ↓
3. Call Azure Cost Management API
   ├─ Query: UsageDetails
   ├─ Filter: Last 24 hours
   └─ GroupBy: Resource, Service, Location
   ↓
4. Transform & Aggregate Data
   ├─ Calculate totals
   ├─ Detect anomalies
   └─ Update trends
   ↓
5. Store in Cosmos DB (Costs container)
   ↓
6. Invalidate Redis Cache
   ├─ tenant:{id}:costs:*
   └─ tenant:{id}:analytics:*
   ↓
7. Trigger Alert Processing
   ↓
8. Send Notifications (if needed)
   ├─ Email (SendGrid)
   ├─ Push (Web Push API)
   └─ Webhook (Slack, Teams)
```

### User Request Flow
```
1. User Request (Frontend)
   ├─ GET /api/costs?tenantId=123&startDate=2025-01-01
   ↓
2. Azure Front Door (CDN)
   ├─ SSL Termination
   ├─ WAF Rules
   └─ Rate Limiting
   ↓
3. App Service (API)
   ├─ Authentication (JWT)
   ├─ Authorization (Role check)
   └─ Route to Controller
   ↓
4. CostController → CostService
   ↓
5. Check Redis Cache
   ├─ HIT → Return cached data (5ms)
   └─ MISS → Query Cosmos DB (50ms)
        ↓
        Cache result (15min TTL)
   ↓
6. Apply Business Logic
   ├─ Calculate aggregates
   ├─ Apply filters
   └─ Format response
   ↓
7. Return JSON Response
   ↓
8. Frontend Renders Data
```

### Authentication Flow (Azure AD SSO)
```
1. User clicks "Login with Microsoft"
   ↓
2. Redirect to Azure AD
   ├─ Authorize endpoint
   ├─ Scope: openid, profile, email
   └─ Response type: code
   ↓
3. User authenticates (username/password + MFA)
   ↓
4. Azure AD redirects back with auth code
   ↓
5. Frontend exchanges code for tokens
   ├─ Access token (API calls)
   ├─ ID token (user info)
   └─ Refresh token (renew access)
   ↓
6. Store tokens in localStorage
   ↓
7. All API calls include: Authorization: Bearer {token}
   ↓
8. API validates token
   ├─ Signature verification
   ├─ Expiration check
   └─ Claims extraction (tenantId, roles)
   ↓
9. API processes request with user context
```

---

## Security Architecture

### Authentication & Authorization

#### Azure AD Integration
- **Protocol**: OAuth 2.0 / OpenID Connect
- **Token Type**: JWT (JSON Web Tokens)
- **Token Lifetime**: 60 minutes (access), 90 days (refresh)
- **MFA**: Enforced for admin roles
- **Conditional Access**: Based on location, device compliance

#### Role-Based Access Control (RBAC)
```
Roles:
├── GlobalAdmin (platform administrators)
│   ├─ Manage all tenants
│   ├─ System configuration
│   └─ User management
├── TenantAdmin (organization administrators)
│   ├─ Manage tenant settings
│   ├─ Invite users
│   └─ Configure budgets/alerts
├── TenantManager (cost managers)
│   ├─ View all cost data
│   ├─ Create reports
│   └─ Manage budgets
└── TenantUser (viewers)
    ├─ View costs (read-only)
    └─ Personal dashboards
```

### Data Security

#### Encryption
- **At Rest**: 
  - Cosmos DB: AES-256 (Microsoft-managed keys)
  - Blob Storage: AES-256
  - Redis: AES-256
- **In Transit**: 
  - TLS 1.2+ for all connections
  - HTTPS enforced (HTTP redirects)

#### Key Management
- **Azure Key Vault**: Centralized secret storage
- **Managed Identities**: Passwordless authentication
- **Rotation**: Automatic key rotation (90 days)

#### Network Security
- **Private Endpoints**: Database/cache access (production)
- **VNet Integration**: Isolate backend services
- **NSG**: Network Security Groups for traffic filtering
- **DDoS Protection**: Azure DDoS Standard

### Compliance
- **GDPR**: Data residency, right to be forgotten
- **SOC 2**: Security controls audit
- **ISO 27001**: Information security management
- **HIPAA**: Healthcare compliance (optional)

---

## Scalability & Performance

### Auto-Scaling

#### App Service
```bicep
resource autoScaleSettings 'Microsoft.Insights/autoscalesettings@2022-10-01' = {
  properties: {
    profiles: [{
      capacity: {
        minimum: '2'
        maximum: '10'
        default: '2'
      }
      rules: [
        {
          metricTrigger: {
            metricName: 'CpuPercentage'
            operator: 'GreaterThan'
            threshold: 70
            timeAggregation: 'Average'
            timeWindow: 'PT5M'
          }
          scaleAction: {
            direction: 'Increase'
            type: 'ChangeCount'
            value: '1'
            cooldown: 'PT5M'
          }
        }
      ]
    }]
  }
}
```

#### Cosmos DB
- **Autoscale**: 400-40,000 RU/s per container
- **Partition Strategy**: Logical partitions by tenantId
- **Hot Partition Mitigation**: Synthetic partition keys

### Caching Strategy

**3-Tier Caching**:
1. **Browser Cache**: Static assets (1 hour)
2. **CDN Cache**: API responses (5 minutes)
3. **Redis Cache**: Database queries (15-60 minutes)

**Cache Invalidation**:
- Time-based (TTL)
- Event-based (on updates)
- Manual (admin API)

### Performance Targets
| Metric | Target | Current |
|--------|--------|---------|
| API Response Time (p95) | <200ms | 120ms |
| Page Load Time | <3s | 1.8s |
| Database Query Time | <50ms | 35ms |
| Cache Hit Rate | >90% | 92.7% |
| Uptime | 99.9% | 99.95% |

---

## Disaster Recovery

### Backup Strategy

#### Databases
- **Cosmos DB**: 
  - Continuous backup (every 4 hours)
  - Retention: 30 days
  - Point-in-time restore
- **Redis**: 
  - RDB snapshots (daily)
  - AOF persistence

#### Application Code
- **Git**: Version control (GitHub)
- **Container Registry**: Docker images
- **ARM Templates**: Infrastructure as Code

### High Availability

#### Multi-Region Deployment (Premium)
```
Primary Region: East US
  ├── App Service (Active)
  ├── Cosmos DB (Write region)
  └── Redis (Primary)

Secondary Region: West US
  ├── App Service (Standby)
  ├── Cosmos DB (Read region)
  └── Redis (Replica)

Failover:
  ├── Traffic Manager (DNS failover)
  ├── Cosmos DB (automatic failover)
  └── Redis (manual failover)
```

#### RTO/RPO Targets
- **Recovery Time Objective (RTO)**: 1 hour
- **Recovery Point Objective (RPO)**: 15 minutes

---

## Technology Stack

### Backend
- **.NET 8**: Modern C# framework
- **ASP.NET Core**: Web API
- **Azure Functions**: Serverless compute
- **Entity Framework Core**: ORM (if needed)

### Frontend
- **React 18**: UI library
- **TypeScript**: Type safety
- **React Router**: Navigation
- **Axios**: HTTP client
- **Recharts**: Data visualization

### Data
- **Azure Cosmos DB**: NoSQL database
- **Redis**: In-memory cache
- **Azure Blob Storage**: File storage

### DevOps
- **GitHub Actions**: CI/CD
- **Bicep**: Infrastructure as Code
- **Docker**: Containerization
- **Azure CLI**: Automation

### Monitoring
- **Application Insights**: APM
- **Log Analytics**: Log aggregation
- **Azure Monitor**: Alerts

### Security
- **Azure AD**: Authentication
- **Key Vault**: Secret management
- **Azure Sentinel**: SIEM

---

**Last Updated**: January 2025  
**Version**: 2.0  
**Architecture Team**: Azure SmartCost
