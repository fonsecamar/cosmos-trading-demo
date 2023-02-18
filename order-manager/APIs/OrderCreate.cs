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
using Microsoft.Azure.Cosmos;
using System.Net;

namespace order_executor.APIs
{
    /// <summary>
    /// Create order Function API
    /// </summary>
    public static class OrderCreate
    {
        static OrderCreate()
        {
            //Instance CosmosClient
            cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), new CosmosClientOptions() { AllowBulkExecution = true });
            container = cosmosClient.GetContainer("trading", "customerPortfolio");
        }

        static CosmosClient cosmosClient;
        static Container container;

        [FunctionName("OrderCreate")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders/create")] HttpRequest req,
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

                double owned_quantity = portfolio == null ? 0 : portfolio.quantity;

                if ((order.action == "sell" && owned_quantity - order.quantity >= 0) || order.action == "buy")
                {
                    //Add entity values
                    order.orderId = Guid.NewGuid().ToString();
                    order.status = "created";
                    order.createdAt = DateTime.UtcNow;

                    //Post order to Cosmos DB using output binding
                    await orderCollector.AddAsync(order);

                    //Return order to caller
                    return new OkObjectResult(order);
                } else
                {
                    if(owned_quantity - order.quantity < 0)
                    {
                        return new BadRequestObjectResult(new { Error = "Invalid Quantity: Not enough stock owned to sell" });
                    } else
                    {
                        return new BadRequestObjectResult(new { Error = "Invalid Request" });
                    }
                    
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);

                return new BadRequestResult();
            }
        }
    }
}
