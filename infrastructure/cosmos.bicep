@description('Cosmos DB account name, max length 44 characters, lowercase')
param accountName string

@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

var databaseName = 'trading'

var containers = [
  {
    name: 'orders'
    partitionKeys: ['/orderId']
    enableIndex: true
    enableAnalyticalStore: true
  }
  {
    name: 'orderExecutions'
    partitionKeys: ['/orderId']
    enableIndex: true
    enableAnalyticalStore: true
  }
  {
    name: 'marketdata'
    partitionKeys: ['/symbol']
    enableIndex: false
    enableAnalyticalStore: true
  }
  {
    name: 'customerPortfolio'
    partitionKeys: ['/customerId', '/assetClass']
    enableIndex: true
    enableAnalyticalStore: true
  }
  {
    name: 'leases'
    partitionKeys: ['/id' ]
    enableIndex: true
    enableAnalyticalStore: false
  }
]

var locations = [
  {
    locationName: location
    failoverPriority: 0
    isZoneRedundant: false
  }
]

@description('Maximum autoscale throughput for the container')
@minValue(1000)
@maxValue(1000000)
param autoscaleMaxThroughput int = 1000

resource account 'Microsoft.DocumentDB/databaseAccounts@2022-05-15' = {
  name: toLower(accountName)
  kind: 'GlobalDocumentDB'
  location: location
  properties: {
    locations: locations
    databaseAccountOfferType: 'Standard'
    enableAnalyticalStorage: true
    analyticalStorageConfiguration: {
      schemaType: 'FullFidelity'
    }
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-05-15' = {
  parent: account
  name: databaseName
  properties: {
    options: {
      autoscaleSettings: {
        maxThroughput: autoscaleMaxThroughput
      }
    }
    resource: {
      id: databaseName
    }
  }
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = [for (config, i) in containers: {
  parent: database
  name: config.name
  properties: {
    resource: {
      id: config.name
      analyticalStorageTtl: -1
      partitionKey: {
        paths: [for pk in config.partitionKeys: pk]
        kind: length(config.partitionKeys) == 1 ? 'Hash' : 'MultiHash'
        version: length(config.partitionKeys) == 1 ? 1 : 2
      }
      indexingPolicy: {
        automatic: config.enableIndex
        indexingMode: config.enableIndex ? 'consistent' : 'none'
      }
    }
  }
}]

output cosmosAccountName string = account.name
output cosmosDatabaseName string = database.name
output cosmosMarketDataContainerName string = container[2].name
