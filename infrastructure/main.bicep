@description('Cosmos DB account name, max length 44 characters, lowercase')
param cosmosAccountName string = 'cosmosdemo${suffix}'

@description('Event Hub namespace name, max length 44 characters, lowercase')
param eventHubNamespace string = 'eventhubdemo${suffix}'

@description('Stream Analytics Job name')
param streamAnalyticsJobName string = 'asademo${suffix}'

@description('Function App name')
param functionAppName string = 'functiondemo${suffix}'

@description('Storage account name, max length 44 characters, lowercase')
param storageAccountName string = 'blobdemo${suffix}'

@description('Location for resource deployment')
param location string = resourceGroup().location

@description('Suffix for resource deployment')
param suffix string = uniqueString(resourceGroup().id)

module cosmosdb 'cosmos.bicep' = {
  scope: resourceGroup()
  name: 'cosmosDeploy'
  params: {
    accountName: cosmosAccountName
    location: location
  }
}

module eventhub 'eventhub.bicep' = {
  name: 'eventHubDeploy'
  params: {
    eventHubNamespace: eventHubNamespace
    location: location
  }
}

module streamanalytics 'streamanalytics.bicep' = {
  name: 'streamAnalyticsDeploy'
  params: {
    streamAnalyticsJobName: streamAnalyticsJobName
    location: location
    cosmosOutputAccountName: cosmosdb.outputs.cosmosAccountName
    cosmosOutputDatabaseName: cosmosdb.outputs.cosmosDatabaseName
    cosmosOutputContainerName: cosmosdb.outputs.cosmosMarketDataContainerName
    marketdataInputEventHubNamespaceName: eventHubNamespace
    marketdataInputEventHubName: eventhub.outputs.marketdataHubName
    marketdataInputEventHubConsumerGroupName: eventhub.outputs.asaConsumerGroup
  }
}

module blob 'blob.bicep' = {
  name: 'blobDeploy'
  params: {
    storageAccountName: storageAccountName
    location: location
  }
}

module function 'functions.bicep' = {
  name: 'functionDeploy'
  params: {
    cosmosAccountName: cosmosAccountName
    eventHubNamespaceName: eventHubNamespace
    functionAppName: functionAppName
    storageAccountName: storageAccountName
    location: location
  }
  dependsOn: [
    cosmosdb
    eventhub
    blob
  ]
}
