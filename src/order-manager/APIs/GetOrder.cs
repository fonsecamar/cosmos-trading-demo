using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.APIs
{
    /// <summary>
    /// Get order Function API
    /// orderId value is provided directly to Cosmos input binding
    /// </summary>
    public static class GetOrder
    {
        [FunctionName("GetOrder")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderId}")] HttpRequest req,
            [CosmosDB(
                databaseName: "trading",
                containerName: "orders",
                PartitionKey = "{orderId}",
                Id = "{orderId}",
                Connection = "CosmosDBConnection")] Order order,
            ILogger log)
        {
            if (order == null)
                return new NotFoundResult();

            //Order returned to caller
            return new OkObjectResult(order);
        }
    }
}
