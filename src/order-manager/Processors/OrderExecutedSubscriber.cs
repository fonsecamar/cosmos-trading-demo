using Azure.Messaging.EventHubs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.Processors
{
    /// <summary>
    /// Eventhub triggered function to update executed orders
    /// </summary>
    public static class OrderExecutedSubscriber
    {
        static Container container;

        [FunctionName("OrderExecutedSubscriber")]
        public static async Task RunAsync(
            [EventHubTrigger(
                "ems-ordersexecuted", 
                Connection = "ordersHubConnection")] EventData[] events,
            [CosmosDB(
                databaseName: "trading",
                containerName: "orders",
                Connection = "CosmosDBConnection")] CosmosClient cosmosClient,
            ILogger log)
        {
            if(container == null)
                container = cosmosClient.GetContainer("trading", "orders");

            //Process received events
            await Parallel.ForEachAsync(events, async (eventData, token) =>
            {
                try
                {
                    //Deserialize event to business entity
                    var order = JsonConvert.DeserializeObject<Order>(Encoding.UTF8.GetString(eventData.EventBody));

                    //Call Cosmos Patch API to replace status and add last modified timestamp
                    await container.PatchItemStreamAsync(order.id,
                        new PartitionKey(order.orderId),
                        new List<PatchOperation>()
                        {
                            PatchOperation.Replace("/status", order.status),
                            PatchOperation.Set("/lastModifiedAt", order.lastModifiedAt.Value)
                        }
                    );
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message, ex);
                }
            });
        }
    }
}
