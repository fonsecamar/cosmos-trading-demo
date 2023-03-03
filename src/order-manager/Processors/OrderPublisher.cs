using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.Processors
{
    /// <summary>
    /// Cosmos DB triggered function (Change Feed) to publish created orders to event hub
    /// </summary>
    public static class OrderPublisher
    {
        [FunctionName("OrderPublisher")]
        public static async Task RunAsync([CosmosDBTrigger(
                databaseName: "trading",
                containerName: "orders",
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                LeaseContainerPrefix = "order-publisher-",
                FeedPollDelay = 5000,
                MaxItemsPerInvocation = 100,
                CreateLeaseContainerIfNotExists = false)]IReadOnlyList<Order> input,
            [EventHub("ems-orderstoexecute", Connection = "ordersHubConnection")] IAsyncCollector<Order> outputOrdersToExecute,
            ILogger log)
        {
            //Process received orders
            await Parallel.ForEachAsync(input, async (order, token) =>
            {
                try
                {
                    //Publish only created orders to event hub for execution
                    if (order.status == "created")
                        await outputOrdersToExecute.AddAsync(order);
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message, ex);
                }
            });
        }
    }
}