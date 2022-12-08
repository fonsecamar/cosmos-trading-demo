using Azure.Messaging.EventHubs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.Processors
{
    /// <summary>
    /// Eventhub triggered function to store orders executions
    /// </summary>
    public static class OrderExecutionLogger
    {
        static OrderExecutionLogger()
        {
            //Instance CosmosClient
            cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), new CosmosClientOptions() { AllowBulkExecution = true });
            container = cosmosClient.GetContainer("trading", "orderExecutions");
        }

        static CosmosClient cosmosClient;
        static Container container;

        [FunctionName("OrderExecutedLogger")]
        public static async Task Run([EventHubTrigger("ems-executions", Connection = "ordersHubConnection")] EventData[] events, ILogger log)
        {
            //Process received events
            await Parallel.ForEachAsync(events, async (eventData, token) =>
            {
                try
                {
                    //Deserialize event to business entity
                    var execution = JsonConvert.DeserializeObject<OrderExecution>(Encoding.UTF8.GetString(eventData.EventBody));

                    //Call Cosmos create item to store object
                    await container.CreateItemAsync(execution, new PartitionKey(execution.orderId));
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message, ex);
                }
            });
        }
    }
}
