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
    public static class StockPriceSummaryMView
    {
        static StockPriceSummaryMView()
        {
            //Instance CosmosClient
            cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), new CosmosClientOptions() { AllowBulkExecution = true });
            container = cosmosClient.GetContainer("trading", "stockPriceSummary");
        }

        static CosmosClient cosmosClient;
        static Container container;

        [FunctionName("StockPriceSummaryMView")]
        public static async Task RunAsync([CosmosDBTrigger(
                databaseName: "trading",
                containerName: "marketdata",
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                LeaseContainerPrefix = "stockprice-summary-",
                FeedPollDelay = 5000,
                MaxItemsPerInvocation = 100,
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<StockPrice> input,
            ILogger log)
        {
            //Process received orders
            await Parallel.ForEachAsync(input, async (stock_price, token) =>
            {
                
                try
                {
                    StockPriceSummary stocks = null;

                    //Build hierarchical partition key (currently preview feature)
                    PartitionKey partitionKey = new PartitionKeyBuilder()
                         .Add("stockprice_summary")
                         .Build();

                    //Lookup (point read) customer portfolio
                    using (var response = await container.ReadItemStreamAsync("stockprice_summary", partitionKey))
                    {
                        if (response.StatusCode != HttpStatusCode.NotFound)
                        {
                            //Deserialize portfolio object if already exists for that customer/symbol
                            JsonSerializer serializer = new JsonSerializer();
                            using (StreamReader streamReader = new StreamReader(response.Content))
                            using (var reader = new JsonTextReader(streamReader))
                            {
                                stocks = serializer.Deserialize<StockPriceSummary>(reader);
                            }
                        }
                    }

                    if (stocks == null)
                    {
                        //Create new portfolio if not exists
                        stocks = new StockPriceSummary();
                        
                        stocks.summary.Add(stock_price.symbol, new AvgPrices { avgAskPrice = stock_price.avgAskPrice, avgBidPrice = stock_price.avgBidPrice });

                        //Store portfolio on Cosmos DB
                        await container.CreateItemAsync(stocks, partitionKey);
                    }
                    else
                    {
                        //If portfolio exists, create patch operations to update entity
                        var operations = new List<PatchOperation>()
                        {
                            PatchOperation.Add($"/summary/{stock_price.symbol}", new AvgPrices { avgAskPrice = stock_price.avgAskPrice, avgBidPrice = stock_price.avgBidPrice })
                        };

                        //Call Cosmos Patch API to update stock prices MView
                        await container.PatchItemAsync<StockPriceSummary>("stockprice_summary", partitionKey, operations);
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