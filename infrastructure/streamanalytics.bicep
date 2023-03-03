@description('Stream Analytics Job name')
param streamAnalyticsJobName string = 'marketdata'

@description('Resource location')
param location string = resourceGroup().location

@description('Market data event hub namespace name')
param marketdataInputEventHubNamespaceName string

@description('Market data event hub name')
param marketdataInputEventHubName string

@description('Market data event hub consumer group name')
param marketdataInputEventHubConsumerGroupName string

@description('Cosmos db account name output')
param cosmosOutputAccountName string

@description('Cosmos db database name output')
param cosmosOutputDatabaseName string

@description('Cosmos db container name output')
param cosmosOutputContainerName string

resource asa 'Microsoft.StreamAnalytics/streamingjobs@2021-10-01-preview' = {
  name: streamAnalyticsJobName
  location: location
  sku: {
    capacity: 1
    name: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    jobType: 'Cloud'
    compatibilityLevel: '1.2'
    sku: {
       capacity: 1
       name: 'Standard'
    }
    inputs: [
      {
        name: 'marketdata'
        properties: {
          type: 'Stream'
          serialization: {
            type: 'Json'
            properties: {
              encoding: 'UTF8'
            }
          }
          compression: {
            type: 'None'
          }
          datasource: {
            type: 'Microsoft.EventHub/EventHub'
            properties: {
              authenticationMode: 'Msi'
              consumerGroupName: marketdataInputEventHubConsumerGroupName
              eventHubName: marketdataInputEventHubName
              serviceBusNamespace: marketdataInputEventHubNamespaceName
            }
          }
        }
      }
    ]
    transformation: {
      name: 'query'
      properties: {
        query: '''
        SELECT record.symbol AS id,
            record.symbol, 
            record.timestamp, 
            record.avgAskPrice AS lastAskPrice, 
            record.avgBidPrice AS lastBidPrice,
            avgAskPrice, avgBidPrice,
            minAskPrice, minBidPrice,
            maxAskPrice,maxBidPrice
        INTO
            [cosmosMarketdata]
        FROM (
            SELECT TopOne() OVER (ORDER BY CAST(timestamp AS datetime) DESC) AS record,
            ROUND(AVG(avgAskPrice),2) AS avgAskPrice,
            ROUND(AVG(avgBidPrice),2) AS avgBidPrice,
            MIN(avgAskPrice) AS minAskPrice,
            MIN(avgBidPrice) AS minBidPrice,
            MAX(avgAskPrice) AS maxAskPrice,
            MAX(avgBidPrice) AS maxBidPrice
            FROM
                [marketdata]
            GROUP BY symbol,
            TumblingWindow(Duration(second, 15), Offset(millisecond, -1))
        ) x
        '''
        streamingUnits: 1
      }
    }
    outputs: [
      {
        name: 'cosmosMarketdata'
        properties: {
          datasource: {
            type: 'Microsoft.Storage/DocumentDB'
            properties: {
              accountId: cosmosOutputAccountName
              authenticationMode: 'Msi'
              database: cosmosOutputDatabaseName
              collectionNamePattern: cosmosOutputContainerName
              documentId: 'id'
            }
          }
        }
      }
    ]
  }
}

resource eh 'Microsoft.EventHub/namespaces@2022-10-01-preview' existing = {
  name: marketdataInputEventHubNamespaceName
}

resource roleAssignmentEh 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(eh.id, 'ASAReader')
  scope: eh
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde') //Azure Event Hubs Data Receiver
    principalId: asa.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' existing = {
  name: cosmosOutputAccountName
}

resource roleAssignmentCosmos 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2022-08-15' = {
  name: guid(cosmos.id, 'ASAWriter')
  parent: cosmos
  properties: {
    scope: cosmos.id
    roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions', cosmos.name, '00000000-0000-0000-0000-000000000002') //Cosmos DB Built-in Data Contributor
    principalId: asa.identity.principalId
  }
}
