@description('The location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environmentName string = 'dev'

@description('The name prefix for all resources')
param projectName string = 'smartcost'

@description('The name of the Function App')
param functionAppName string = '${projectName}-func-${environmentName}-${uniqueString(resourceGroup().id)}'

@description('The name of the API App Service')
param apiAppName string = '${projectName}-api-${environmentName}-${uniqueString(resourceGroup().id)}'

@description('The name of the Static Web App')
param staticWebAppName string = '${projectName}-web-${environmentName}-${uniqueString(resourceGroup().id)}'

@description('The name of the Storage Account')
param storageAccountName string = '${projectName}stg${uniqueString(resourceGroup().id)}'

@description('The name of the Cosmos DB Account')
param cosmosAccountName string = '${projectName}-cosmos-${environmentName}-${uniqueString(resourceGroup().id)}'

@description('The name of the Key Vault')
param keyVaultName string = '${projectName}-kv-${environmentName}-${uniqueString(resourceGroup().id)}'

@description('The Azure subscription ID to monitor costs for')
param subscriptionId string = subscription().subscriptionId

@description('SKU for App Service Plan')
param appServiceSku string = 'B1'

@description('JWT Secret for authentication')
@secure()
param jwtSecret string

@description('Azure AD Client ID')
param azureAdClientId string

@description('Azure AD Client Secret')
@secure()
param azureAdClientSecret string

@description('Azure AD Tenant ID')
param azureAdTenantId string = tenant().tenantId

@description('Azure Marketplace App Registration Tenant ID')
param marketplaceTenantId string = tenant().tenantId

@description('Azure Marketplace App Registration Client ID')
param marketplaceClientId string

@description('Azure Marketplace App Registration Client Secret')
@secure()
param marketplaceClientSecret string

@description('Azure Marketplace Offer ID')
param marketplaceOfferId string = 'azure-smartcost'

@description('Azure Marketplace Publisher ID')
param marketplacePublisherId string

// Application Insights (linked to Log Analytics)
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${functionAppName}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    IngestionMode: 'ApplicationInsights'
    WorkspaceResourceId: logAnalytics.id
  }
}

// Log Analytics Workspace (required for Application Insights)
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${functionAppName}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Storage Account for Azure Functions
resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

// Azure Cache for Redis
resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: '${projectName}-redis-${environmentName}-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    sku: {
      name: environmentName == 'prod' ? 'Standard' : 'Basic'
      family: environmentName == 'prod' ? 'C' : 'C'
      capacity: environmentName == 'prod' ? 1 : 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
      'maxmemory-reserved': '50'
    }
  }
}

// Cosmos DB Account
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

// Cosmos Database
resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: cosmosAccount
  name: 'SmartCostDB'
  properties: {
    resource: {
      id: 'SmartCostDB'
    }
  }
}

// Cost Records Container
resource costRecordsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: 'CostRecords'
  properties: {
    resource: {
      id: 'CostRecords'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
    }
  }
}

// Events Container
resource eventsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: 'Events'
  properties: {
    resource: {
      id: 'Events'
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
      }
    }
  }
}

// Marketplace Subscriptions Container
resource marketplaceSubscriptionsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: 'MarketplaceSubscriptions'
  properties: {
    resource: {
      id: 'MarketplaceSubscriptions'
      partitionKey: {
        paths: ['/marketplaceSubscriptionId']
        kind: 'Hash'
      }
    }
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    accessPolicies: []
    enableRbacAuthorization: true
  }
}

// App Service Plan for API
resource apiPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${apiAppName}-plan'
  location: location
  sku: {
    name: appServiceSku
    capacity: 1
  }
  properties: {
    reserved: false
  }
}

// API App Service
resource apiApp 'Microsoft.Web/sites@2022-09-01' = {
  name: apiAppName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: apiPlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : 'Development'
        }
        {
          name: 'Jwt__Secret'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=jwt-secret)'
        }
        {
          name: 'Jwt__Issuer'
          value: 'AzureSmartCost'
        }
        {
          name: 'Jwt__Audience'
          value: 'AzureSmartCost-API'
        }
        {
          name: 'Jwt__ExpiryMinutes'
          value: '60'
        }
        {
          name: 'AzureAd__TenantId'
          value: azureAdTenantId
        }
        {
          name: 'AzureAd__ClientId'
          value: azureAdClientId
        }
        {
          name: 'AzureAd__ClientSecret'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=azure-ad-client-secret)'
        }
        {
          name: 'Stripe__ApiKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=stripe-api-key)'
        }
        {
          name: 'Stripe__PublishableKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=stripe-publishable-key)'
        }
        {
          name: 'Stripe__WebhookSecret'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=stripe-webhook-secret)'
        }
        {
          name: 'Marketplace__TenantId'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=marketplace-tenant-id)'
        }
        {
          name: 'Marketplace__ClientId'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=marketplace-client-id)'
        }
        {
          name: 'Marketplace__ClientSecret'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=marketplace-client-secret)'
        }
        {
          name: 'Marketplace__OfferId'
          value: marketplaceOfferId
        }
        {
          name: 'Marketplace__PublisherId'
          value: marketplacePublisherId
        }
        {
          name: 'Marketplace__LandingPageUrl'
          value: 'https://${apiAppName}.azurewebsites.net/api/marketplace/landing'
        }
        {
          name: 'Marketplace__WebhookUrl'
          value: 'https://${apiAppName}.azurewebsites.net/api/marketplace/webhook'
        }
        {
          name: 'KeyVault__VaultUrl'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'KeyVault__UseKeyVault'
          value: 'true'
        }
        {
          name: 'Redis__Enabled'
          value: 'true'
        }
        {
          name: 'Redis__InstanceName'
          value: 'SmartCost:'
        }
        {
          name: 'Redis__DefaultExpirationMinutes'
          value: '60'
        }
        {
          name: 'CosmosDb__DatabaseName'
          value: 'SmartCostDB'
        }
        {
          name: 'CosmosDb__ContainerName'
          value: 'CostRecords'
        }
      ]
      connectionStrings: [
        {
          name: 'CosmosDb'
          connectionString: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=cosmos-connection-string)'
          type: 'Custom'
        }
        {
          name: 'Redis'
          connectionString: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=redis-connection-string)'
          type: 'Custom'
        }
      ]
    }
  }
}

// Static Web App for Frontend
resource staticWebApp 'Microsoft.Web/staticSites@2022-09-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    repositoryUrl: ''
    branch: 'main'
    buildProperties: {
      appLocation: '/smartcost-dashboard'
      apiLocation: ''
      outputLocation: 'build'
    }
  }
}

// App Service Plan for Functions
resource plan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: false
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AZURE_SUBSCRIPTION_ID'
          value: subscriptionId
        }
        {
          name: 'CosmosDb__DatabaseName'
          value: 'SmartCostDB'
        }
        {
          name: 'CosmosDb__CostRecordsContainer'
          value: 'CostRecords'
        }
        {
          name: 'CosmosDb__EventsContainer'
          value: 'Events'
        }
      ]
      connectionStrings: [
        {
          name: 'CosmosDb'
          connectionString: cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
          type: 'Custom'
        }
      ]
    }
  }
}

// Role Assignment - Cost Management Reader for Function App
// Note: This needs to be deployed at subscription scope separately
// resource costManagementReaderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
//   name: guid(subscription().id, functionApp.id, 'Cost Management Reader')
//   scope: subscription()
//   properties: {
//     roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '72fafb9e-0641-4937-9268-a91bfd8191a3') // Cost Management Reader
//     principalId: functionApp.identity.principalId
//     principalType: 'ServicePrincipal'
//   }
// }

// Role Assignment - Key Vault access for API App
resource apiAppKeyVaultRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiApp.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Role Assignment - Cosmos DB Built-in Data Contributor for API App
resource apiAppCosmosRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(cosmosAccount.id, apiApp.id, 'Cosmos DB Built-in Data Contributor')
  scope: cosmosAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '00000000-0000-0000-0000-000000000002') // Cosmos DB Built-in Data Contributor
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Role Assignment - Cosmos DB Built-in Data Contributor for Function App
resource cosmosContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(cosmosAccount.id, functionApp.id, 'Cosmos DB Built-in Data Contributor')
  scope: cosmosAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '00000000-0000-0000-0000-000000000002') // Cosmos DB Built-in Data Contributor
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Key Vault Secret - Store sensitive configuration
resource jwtSecretKv 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-secret'
  properties: {
    value: jwtSecret
  }
}

resource azureAdClientSecretKv 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'azure-ad-client-secret'
  properties: {
    value: azureAdClientSecret
  }
}

resource storageConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'storage-connection-string'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
  }
}

resource cosmosConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'cosmos-connection-string'
  properties: {
    value: cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
  }
}

resource redisConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'redis-connection-string'
  properties: {
    value: '${redisCache.properties.hostName}:${redisCache.properties.sslPort},password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
  }
}

resource marketplaceTenantIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'marketplace-tenant-id'
  properties: {
    value: marketplaceTenantId
  }
}

resource marketplaceClientIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'marketplace-client-id'
  properties: {
    value: marketplaceClientId
  }
}

resource marketplaceClientSecretKv 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'marketplace-client-secret'
  properties: {
    value: marketplaceClientSecret
  }
}

// Outputs
output resourceGroupName string = resourceGroup().name
output functionAppName string = functionApp.name
output functionAppPrincipalId string = functionApp.identity.principalId
output apiAppName string = apiApp.name
output apiAppUrl string = 'https://${apiApp.properties.defaultHostName}'
output apiAppPrincipalId string = apiApp.identity.principalId
output staticWebAppName string = staticWebApp.name
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output cosmosAccountName string = cosmosAccount.name
output keyVaultName string = keyVault.name
output storageAccountName string = storage.name
output appInsightsName string = appInsights.name
output logAnalyticsWorkspaceName string = logAnalytics.name
