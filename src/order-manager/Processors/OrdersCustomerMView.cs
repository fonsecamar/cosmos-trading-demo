using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.Processors
{
    /// <summary>
    /// Cosmos DB triggered function (Change Feed) to generate customer portfolio view based on executed orders
    /// </summary>
    public static class OrdersCustomerMView
    {
        static Container container;

        [FunctionName("OrdersCustomerMView")]
        public static async Task RunAsync([CosmosDBTrigger(
                databaseName: "trading",
                containerName: "orders",
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                LeaseContainerPrefix = "customer-portfolio-",
                FeedPollDelay = 5000,
                MaxItemsPerInvocation = 100,
                CreateLeaseContainerIfNotExists = false)]IReadOnlyList<Order> input,
            [CosmosDB(
                databaseName: "trading",
                containerName: "customerPortfolio",
                Connection = "CosmosDBConnection")] CosmosClient cosmosClient,
            ILogger log)
        {
            if(container == null)
                container = cosmosClient.GetContainer("trading", "customerPortfolio");

            //Process received orders
            await Parallel.ForEachAsync(input, async (order, token) =>
            {
                //Only executed orders affect customer portfolio
                if (order.status != "executed")
                    return;

                try
                {
                    CustomerPortfolio portfolio = null;

                    //Build hierarchical partition key (currently preview feature)
                    PartitionKey partitionKey = new PartitionKeyBuilder()
                         .Add(order.customerId)
                         .Add(order.assetClass)
                         .Build();

                    //Lookup (point read) customer portfolio
                    using (var response = await container.ReadItemStreamAsync($"{order.customerId}_{order.symbol}", partitionKey))
                    {
                        if (response.StatusCode != HttpStatusCode.NotFound)
                        {
                            //Deserialize portfolio object if already exists for that customer/symbol
                            JsonSerializer serializer = new JsonSerializer();
                            using (StreamReader streamReader = new StreamReader(response.Content))
                            using (var reader = new JsonTextReader(streamReader))
                            {
                                portfolio = serializer.Deserialize<CustomerPortfolio>(reader);
                            }
                        }
                    }

                    if (portfolio == null)
                    {
                        //Create new portfolio if not exists
                        portfolio = new CustomerPortfolio()
                        {
                            symbol = order.symbol,
                            customerId = order.customerId,
                            assetClass = order.assetClass,
                            createdAt = order.createdAt,
                            quantity = order.action == "sell" ? -order.quantity : order.quantity,
                            price = order.price
                        };

                        //Store portfolio on Cosmos DB
                        await container.CreateItemAsync(portfolio, partitionKey);
                    }
                    else
                    {
                        //If portfolio exists, create patch operations to update entity
                        var operations = new List<PatchOperation>()
                        {
                            PatchOperation.Increment("/quantity", order.action == "sell" ? -order.quantity : order.quantity),
                            PatchOperation.Add("/lastModifiedAt", order.lastModifiedAt.Value)
                        };

                        //If buying new lot, calculate avg price
                        //If selling all shares, add ttl to expire entry after 24hs
                        if (order.action == "buy")
                            operations.Add(PatchOperation.Set("/price", Math.Round((portfolio.position + order.price * order.quantity) / (portfolio.quantity + order.quantity), 2)));
                        else if (portfolio.quantity == order.quantity)
                            operations.Add(PatchOperation.Add("/ttl", 86400));

                        //Call Cosmos Patch API to update portfolio
                        await container.PatchItemAsync<CustomerPortfolio>(portfolio.id, partitionKey, operations);
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message, ex);
                }
            });
        }
    }
}