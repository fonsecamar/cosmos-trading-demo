@description('Function app name')
param functionAppName string

@description('Storage account name')
param storageAccountName string

@description('Cosmos DB account name')
param cosmosAccountName string

@description('Event Hub namespace name')
param eventHubNamespaceName string

@description('Resource location')
param location string = resourceGroup().location

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: functionAppName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource plan 'Microsoft.Web/serverfarms@2020-12-01' = {
  name: '${functionAppName}Plan'
  location: location
  kind: 'functionapp'
  sku: {
    name: 'Y1'
  }
}

resource blob 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: storageAccountName
}

resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    siteConfig: {
      use32BitWorkerProcess: false
      
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${blob.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(blob.id, blob.apiVersion).keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${blob.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(blob.id, blob.apiVersion).keys[0].value}'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appInsights.properties.InstrumentationKey}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'CosmosDBConnection__accountEndpoint'
          value: 'https://${cosmosAccountName}.documents.azure.com:443/'
        }
        {
          name: 'ordersHubConnection__fullyQualifiedNamespace'
          value: '${eventHubNamespaceName}.servicebus.windows.net'
        }        
      ]
    }
    httpsOnly: true
  }
}

resource eh 'Microsoft.EventHub/namespaces@2022-10-01-preview' existing = {
  name: eventHubNamespaceName
}

resource roleAssignmentEh 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(eh.id, 'FunctionOwner')
  scope: eh
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec') //Azure Event Hubs Data Owner
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' existing = {
  name: cosmosAccountName
}

resource roleAssignmentCosmos 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2022-08-15' = {
  name: guid(cosmos.id, 'CosmosContributor')
  parent: cosmos
  properties: {
    scope: cosmos.id
    roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions', cosmos.name, '00000000-0000-0000-0000-000000000002') //Cosmos DB Built-in Data Contributor
    principalId: functionApp.identity.principalId
  }
}
