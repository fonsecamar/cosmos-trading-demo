using Azure.Messaging.EventHubs;
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
    /// Eventhub triggered function to simulare orders execution
    /// </summary>
    public static class OrderExecutor
    {
        [FunctionName("OrderExecutor")]
        public static async Task Run([EventHubTrigger("ems-orderstoexecute", Connection = "ordersHubConnection")] EventData[] events,
            [EventHub("ems-executions", Connection = "ordersHubConnection")] IAsyncCollector<OrderExecution> outputExecutions,
            [EventHub("ems-ordersexecuted", Connection = "ordersHubConnection")] IAsyncCollector<Order> outputOrders,
            ILogger log)
        {
            //Process received orders
            await Parallel.ForEachAsync(events, async (eventData, token) =>
            {
                try
                {
                    //Deserialize event to business entity
                    var order = JsonConvert.DeserializeObject<Order>(Encoding.UTF8.GetString(eventData.EventBody));
                    int _quantity = order.quantity;

                    //Breakdown order into random quantity executions
                    while (_quantity > 0)
                    {
                        //Get random quantity for execution
                        var partialQuantity = _quantity <= 100 ? _quantity : Random.Shared.Next(100, _quantity);
                        _quantity -= partialQuantity;

                        //Instance execution object
                        var execution = new OrderExecution()
                        {
                            id = Guid.NewGuid().ToString(),
                            orderId = order.orderId,
                            quantity = partialQuantity,
                            customerId = order.customerId,
                            action = order.action,
                            price = order.price,
                            executedAt = DateTime.UtcNow,
                            symbol = order.symbol
                        };

                        //Publish execution to event hub
                        await outputExecutions.AddAsync(execution);
                    }

                    //After order has been fully "executed" change status and add modified timestamp
                    order.status = "executed";
                    order.lastModifiedAt = DateTime.UtcNow;

                    //Publish executed order to event hub
                    await outputOrders.AddAsync(order);
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message, ex);
                }
            });
        }
    }
}