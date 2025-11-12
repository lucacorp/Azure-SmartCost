targetScope = 'subscription'

@description('The principal ID of the Function App managed identity')
param functionAppPrincipalId string

// Role Assignment - Cost Management Reader for Function App
resource costManagementReaderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, functionAppPrincipalId, 'Cost Management Reader')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '72fafb9e-0641-4937-9268-a91bfd8191a3') // Cost Management Reader
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output roleAssignmentId string = costManagementReaderRole.id