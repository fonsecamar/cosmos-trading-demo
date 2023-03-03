using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using trading_model;

namespace order_executor.APIs
{
    /// <summary>
    /// Create order Function API
    /// </summary>
    public static class OrderCreate
    {
        [FunctionName("OrderCreate")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/create")] HttpRequest req,
            [CosmosDB(
                databaseName: "trading",
                containerName: "orders",
                Connection = "CosmosDBConnection")] IAsyncCollector<Order> orderCollector,
            ILogger log)
        {
            try
            {
                //Read request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var order = JsonConvert.DeserializeObject<Order>(requestBody);

                //Add entity values
                order.orderId = Guid.NewGuid().ToString();
                order.status = "created";
                order.createdAt = DateTime.UtcNow;

                //Post order to Cosmos DB using output binding
                await orderCollector.AddAsync(order);

                //Return order to caller
                return new OkObjectResult(order);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);

                return new BadRequestResult();
            }
        }
    }
}
