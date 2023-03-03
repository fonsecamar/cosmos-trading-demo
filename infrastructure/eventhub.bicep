@description('Event Hub namespace name, max length 44 characters, lowercase')
param eventHubNamespace string

@description('Resource location')
param location string = resourceGroup().location

var eventHubs = ['marketdata', 'ems-orderstoexecute', 'ems-ordersexecuted', 'ems-executions']

resource namespace 'Microsoft.EventHub/namespaces@2022-10-01-preview' = {
  name: eventHubNamespace
  location: location
  sku: {
    name: 'Standard'
    capacity: 1
    tier: 'Standard'
  }
}

resource hubs 'Microsoft.EventHub/namespaces/eventhubs@2022-10-01-preview' = [for eh in eventHubs: {
  name: eh
  parent: namespace
  properties: {
    partitionCount: 32
    messageRetentionInDays: 1
    status: 'Active'
  }
}]

resource consumergroup 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2022-10-01-preview' = {
  name: 'asa'
  parent: hubs[indexOf(eventHubs,'marketdata')]
}

output marketdataHubName string = eventHubs[0]
output asaConsumerGroup string = consumergroup.name
