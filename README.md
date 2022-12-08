# Cosmos DB NoSQL API - Trading Demo

## Introduction

This repository provides a code sample in .NET on how to use some Azure Cosmos DB features integrated with Azure Funcions.

## Requirements

> It's recommended to create all the resources within the same region.

* <a href="https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-app-portal#create-a-function-app" target="_blank">Create a Function App.</a> Choose the Runtime stack accordingly (sample code provided in **.NET 6**).

* <a href="https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/quickstart-dotnet?tabs=azure-portal%2Cwindows#create-account" target="_blank">Create a Cosmos DB NoSQL API (formerly Core API) account.</a>

* <a href="https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-create#create-an-event-hubs-namespace" target="_blank">Create an Event Hub namespace.</a>

* <a href="https://github.com/fonsecamar/cosmos-trading-demo.git" target="_blank">Clone this repository.</a>

## Configuration

* <a href="https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-create#create-an-event-hub" target="_blank">Create Event Hubs</a>
    * Create `marketdata` Event Hub
    * Create `ems-orderstoexecute` Event Hub
    * Create `ems-ordersexecuted` Event Hub
    * Create `ems-executions` Event Hub

* <a href="https://docs.microsoft.com/en-us/azure/cosmos-db/mongodb/how-to-create-container-mongodb#portal-mongodb" target="_blank">Create a database and containers</a>
    * Create `trading` database
    * Create `orders` container: provide `/orderId` as the **Partition key**, select `Autoscale` and provide `1000` as **Collection Max RU/s**.
    * Create `orderExecutions` container: provide `/orderId` as the **Partition key**, select `Autoscale` and provide `1000` as **Collection Max RU/s**.
    * Create `marketdata` container: provide `/symbol` as the **Partition key**, select `Autoscale` and provide `1000` as **Collection Max RU/s**.
    * Create `customerPortfolio` container using the SDK (<a href="https://learn.microsoft.com/en-us/azure/cosmos-db/hierarchical-partition-keys?tabs=net-v3%2Cbicep#create-new-container-with-hierarchical-partition-keys" target="_blank">Create new container with hierarchical partition keys</a>): provide `/customerId` and `/assetClass` as the **Partition keys**, select `Autoscale` and provide `1000` as **Collection Max RU/s**.

* <a href="https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings?tabs=portal" target="_blank">Configure application settings</a>
    * CosmosDBConnection: `<your Cosmos DB connection string>`
    * ordersHubConnection: `<your Event Hub namespace connection string>`

* Deploy Function application to Azure (<a href="https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-vs-code" target="_blank">Deploy using VS Code</a>).

## Running the sample

You can call Function APIs from Azure Portal or your favorite tool.

1. Call CreateOrder function

    ```
    curl --request POST 'https://<function app name>.azurewebsites.net/api/orders/create?code=<function code>' \
    --header 'Content-Type: application/json' \
    --data-raw '{
        "customerId": 99999999,
        "quantity": 1000,
        "symbol": "MSFT",
        "price": 300,
        "action": "buy"
    }'
    ```

1. Call GetOrder function (use orderId from the previous response)

    ```
    -- Returns Order by orderId
    curl --request GET 'https://<function app name>.azurewebsites.net/api/orders/{orderId}?code=<function code>'
    ```

1. Call GetExecutions function (use the same orderId)

    ```
    -- Returns Order Executions by orderId
    curl --request GET 'https://<function app name>.azurewebsites.net/api/orders/execution/{orderId}?code=<function code>'
    ```

1. Call GetCustomerPortfolio function (use customerId provided on step 1)

    ```
    -- Returns Customer Portfolio by customerId
    curl --request GET 'https://<function app name>.azurewebsites.net/api/customerPortfolio/{customerId}?code=<function code>'
    ```

<br/>

# How to Contribute

If you find any errors or have suggestions for changes, please be part of this project!

1. Create your branch: `git checkout -b my-new-feature`
2. Add your changes: `git add .`
3. Commit your changes: `git commit -m '<message>'`
4. Push your branch to Github: `git push origin my-new-feature`
5. Create a new Pull Request 😄