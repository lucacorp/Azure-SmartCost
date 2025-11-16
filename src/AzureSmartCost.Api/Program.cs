using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.Text;
using AzureSmartCost.Shared.Services;
using AzureSmartCost.Shared.Services.Implementation;
using AzureSmartCost.Shared.Models;
using AzureSmartCost.Api.Middleware;
using AzureSmartCost.Shared.Interfaces;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault if enabled
var keyVaultUrl = builder.Configuration["KeyVault:VaultUrl"];
var useKeyVault = builder.Configuration.GetValue<bool>("KeyVault:UseKeyVault");

if (useKeyVault && !string.IsNullOrEmpty(keyVaultUrl))
{
    var credential = new DefaultAzureCredential();
    var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
    
    builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
    
    builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);
    Console.WriteLine($"✅ Azure Key Vault configured: {keyVaultUrl}");
}
else
{
    Console.WriteLine("⚠️ Azure Key Vault disabled - using appsettings.json for secrets (development only)");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Azure SmartCost FinOps API", 
        Version = "v1",
        Description = "API REST para monitoramento e análise de custos Azure em tempo real"
    });
});

// Configure CORS for frontend integration
var corsOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>() ?? new[]
{
    "http://localhost:3000",
    "https://localhost:3000",
    "https://victorious-ground-003efd50f.3.azurestaticapps.net",
    "https://victorious-ground-003efd50f-preview.eastus2.3.azurestaticapps.net"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSmartCostFrontend", policy =>
        policy.WithOrigins(corsOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

// Configure JWT and Azure AD Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SmartCost-Default-Secret-Key-Change-In-Production-2024";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AzureSmartCost";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AzureSmartCost-API";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    })
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), "AzureAd")
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddMicrosoftGraph(builder.Configuration.GetSection("Graph"))
        .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    // Define authorization policies
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole(Roles.Admin));
    
    options.AddPolicy("FinanceTeam", policy => 
        policy.RequireRole(Roles.Admin, Roles.FinanceManager, Roles.CostAnalyst));
    
    options.AddPolicy("CanReadCosts", policy => 
        policy.RequireClaim("permission", Permissions.ReadCosts));
    
    options.AddPolicy("CanWriteCosts", policy => 
        policy.RequireClaim("permission", Permissions.WriteCosts));
    
    options.AddPolicy("CanManageAlerts", policy => 
        policy.RequireClaim("permission", Permissions.ManageThresholds));
});

// Configure Cosmos DB
builder.Services.Configure<CosmosDbSettings>(
    builder.Configuration.GetSection("CosmosDb"));

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("CosmosDb")
        ?? throw new InvalidOperationException("CosmosDb connection string not found");
    
    return new CosmosClient(connectionString, new CosmosClientOptions
    {
        ApplicationName = "AzureSmartCost.Api",
        MaxRetryAttemptsOnRateLimitedRequests = 3,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(10)
    });
});

// Register services
// Cosmos DB Service
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
builder.Services.AddSingleton<IAlertService, AlertService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Add Advanced Analytics & ML Services
builder.Services.AddScoped<ICostAnalyticsService, CostAnalyticsService>();

// Add Monitoring & Alerting Services
builder.Services.AddScoped<IMonitoringService, MonitoringService>();

// Add Budget Management Services
builder.Services.AddScoped<IBudgetManagementService, BudgetManagementService>();

// Add Multi-Tenancy Services
builder.Services.AddScoped<AzureSmartCost.Shared.Interfaces.ITenantContext, AzureSmartCost.Shared.Interfaces.TenantContext>();
builder.Services.AddScoped<AzureSmartCost.Shared.Interfaces.ITenantService, TenantService>();

// Add Stripe Billing Service
builder.Services.AddScoped<AzureSmartCost.Shared.Interfaces.IStripeService, AzureSmartCost.Shared.Services.Implementation.StripeService>();

// Add Key Vault Service
builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();

// Add Marketplace Service
builder.Services.AddHttpClient<IMarketplaceService, MarketplaceService>();
builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();

// Add Azure AD Service
builder.Services.AddScoped<IAzureAdService, AzureAdService>();

// Configure Redis Cache
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var enableRedis = builder.Configuration.GetValue<bool>("Redis:Enabled", false);

if (enableRedis && !string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        // Add IConnectionMultiplexer as singleton
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = ConfigurationOptions.Parse(redisConnectionString);
            config.AbortOnConnectFail = false; // Don't crash if Redis is unavailable
            config.ConnectTimeout = 5000;
            config.SyncTimeout = 5000;
            
            var logger = sp.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Connecting to Redis: {Endpoint}", config.EndPoints.FirstOrDefault());
            
            return ConnectionMultiplexer.Connect(config);
        });

        // Add ICacheService
        builder.Services.AddScoped<ICacheService, RedisCacheService>();
        
        // Add distributed cache (for session state if needed)
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "SmartCost:";
        });
        
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        
        Console.WriteLine($"✅ Redis Cache enabled: {redisConnectionString.Split(',').First()}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Redis connection failed: {ex.Message}. Running without cache.");
        // Register null cache service as fallback
        builder.Services.AddScoped<ICacheService>(sp => null!);
    }
}
else
{
    Console.WriteLine("⚠️ Redis Cache disabled - running without distributed cache");
    // Register null cache service
    builder.Services.AddScoped<ICacheService>(sp => null!);
}

// Add Application Insights Telemetry (optional - will be null if not configured)
builder.Services.AddSingleton<Microsoft.ApplicationInsights.TelemetryClient>(sp =>
{
    var config = new Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration();
    // Get instrumentation key from configuration (optional)
    var instrumentationKey = builder.Configuration["ApplicationInsights:InstrumentationKey"];
    if (!string.IsNullOrEmpty(instrumentationKey))
    {
        config.ConnectionString = $"InstrumentationKey={instrumentationKey}";
    }
    return new Microsoft.ApplicationInsights.TelemetryClient(config);
});

// Register HTTP client for Power BI service
builder.Services.AddHttpClient<IPowerBiService, PowerBiService>();

// Choose between real Power BI API or mock
var useRealPowerBi = builder.Configuration.GetValue<bool>("USE_REAL_POWERBI_API");
if (useRealPowerBi)
{
    builder.Services.AddScoped<IPowerBiService, PowerBiService>();
}
else
{
    builder.Services.AddScoped<IPowerBiService, MockPowerBiService>();
}

// Choose between real API or mock
var useRealCostApi = builder.Configuration.GetValue<bool>("USE_REAL_COST_API");
if (useRealCostApi)
{
    builder.Services.AddSingleton<ICostManagementService, CostManagementServiceReal>();
}
else
{
    builder.Services.AddSingleton<ICostManagementService, CostManagementService>();
}

var app = builder.Build();

// Initialize Cosmos DB containers on startup
using (var scope = app.Services.CreateScope())
{
    var cosmosDbService = scope.ServiceProvider.GetRequiredService<ICosmosDbService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("🗄️ Initializing Cosmos DB containers...");
        await cosmosDbService.InitializeAsync();
        logger.LogInformation("✅ Cosmos DB initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Failed to initialize Cosmos DB. Application will continue but may have issues.");
        // Continue startup even if Cosmos DB fails - allows development with mock services
    }
    
    // Test Redis connection if enabled
    if (enableRedis)
    {
        try
        {
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();
            if (cacheService != null)
            {
                var isHealthy = await cacheService.IsHealthyAsync();
                if (isHealthy)
                {
                    logger.LogInformation("✅ Redis Cache connection verified");
                }
                else
                {
                    logger.LogWarning("⚠️ Redis Cache connection unhealthy");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Redis Cache health check failed");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure SmartCost FinOps API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowSmartCostFrontend");

// Enable session if Redis is configured
if (enableRedis)
{
    app.UseSession();
}

// Enable authentication and authorization
app.UseAuthentication();

// Add Tenant Context Middleware (must be after Authentication)
app.UseTenantMiddleware();

app.UseAuthorization();

app.MapControllers();

// Welcome endpoint
app.MapGet("/", () => new { 
    Message = "🚀 Azure SmartCost FinOps API is running!",
    Version = "1.0.0",
    Documentation = "/swagger",
    Endpoints = new {
        Costs = "/api/costs",
        Alerts = "/api/alerts",
        Dashboards = "/api/dashboard",
        Health = "/api/health"
    }
});

app.Run();
