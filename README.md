# Cosmos DB NoSQL API - Trading Demo

## Introduction

This repository provides a code sample in .NET on how to use some Azure Cosmos DB features integrated with Azure Funcions.

## Requirements to deploy
> Setup shell was tested on WSL2 (Ubuntu 22.04.2 LTS)

* <a href="https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-linux?pivots=apt#option-1-install-with-one-command" target="_blank">Install Azure CLI</a>

* <a href="https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=v4%2Clinux%2Ccsharp%2Cportal%2Cbash#install-the-azure-functions-core-tools" target="_blank">Install Azure Functions Core Tools</a>

* <a href="https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#install-the-sdk" target="_blank">Install .NET SDK 7.0</a>

* <a href="https://git-scm.com/download/linux" target="_blank">Install Git</a>

## Setup environment

> The setup will provision and configure all the resources required.

* Sign in with Azure CLI

    ```bash
    az login
    ```

* Clone the repo
    ```bash
    git clone https://github.com/fonsecamar/cosmos-trading-demo.git
    cd cosmos-trading-demo/deploy/
    ```

* Run setup.sh with the appropriete parameters. Keep the API's URIs prompted when completed.

    ```bash
    #SAMPLE
    #./setup.sh 00000000-0000-0000-0000-000000000000 rg-my-demo SouthCentralUS myrandomsuffix

    ./setup.sh <subscription id> <resource grouop> <location> <resources suffix>
    ```
> Setup has some pause stages. Hit enter to continue when prompted. 
> 
> It takes around 3min to provision and configure resoures.
>
> Resources created:
> - Eesource groups
> - Azure Blob Storage (ADLS Gen2)
> - Azure Cosmos DB account (1 database with 1000 RUs autoscale shared with 5 collections) with Analytical Store enabled
> - Azure Event Hub standard
> - Azure Steam Analytics job
> - Azure Functions Consumption Plan
> - Azure Application Insights

## Running the sample

You can call Function APIs from Azure Portal or your favorite tool.

1. Start Azure Stream Analytics job

1. Run markerdata generator

    ```bash
    cd ../src/marketdata-generator
    dotnet run
    ```

1. Check Cosmos DB marketdata container (updated every 15 second by Azure Stream Analytics job).

4. Call GetStockPrice function

    ```bash
    #Setting variables
    SUFFIX=<your suffix>

    # Returns Stock Price by symbol
    curl --request GET "https://functiondemo$SUFFIX.azurewebsites.net/api/stock/MSFT"
    ```

1. Call CreateOrder function

    ```bash
    # Creates an Order
    curl --request POST "https://functiondemo$SUFFIX.azurewebsites.net/api/orders/create" \
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

    ```bash
    # Returns Order by orderId
    curl --request GET "https://functiondemo$SUFFIX.azurewebsites.net/api/orders/{orderId}"
    ```

1. Call GetExecutions function (use the same orderId)

    ```bash
    -- Returns Order Executions by orderId
    curl --request GET "https://functiondemo$SUFFIX.azurewebsites.net/api/orders/execution/{orderId}"
    ```

1. Call GetCustomerPortfolio function (use customerId provided on step 1)

    ```bash
    -- Returns Customer Portfolio by customerId
    curl --request GET "https://functiondemo$SUFFIX.azurewebsites.net/api/customerPortfolio/{customerId}"
    ```
<br/>

# How to Contribute

If you find any errors or have suggestions for changes, please be part of this project!

1. Create your branch: `git checkout -b my-new-feature`
2. Add your changes: `git add .`
3. Commit your changes: `git commit -m '<message>'`
4. Push your branch to Github: `git push origin my-new-feature`
5. Create a new Pull Request 😄