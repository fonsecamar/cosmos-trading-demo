using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Bogus;
using Bogus.Distributions.Gaussian;
using trading_model;

namespace order_executor.DataGenerators
{
    public class customer_id
    {
        public string id { get; set; }
    }
    public class OrderGenerator
    {
        static OrderGenerator()
        {
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", Environment.GetEnvironmentVariable("CreateOrderApiKey"));
            cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), new CosmosClientOptions() { AllowBulkExecution = true });
            container = cosmosClient.GetContainer("trading", "stockPriceSummary");
            portfolios_container = cosmosClient.GetContainer("trading", "customerPortfolio");
            CreateOrderApiUrl = Environment.GetEnvironmentVariable("CreateOrderApiUrl");

            GetCurrentCustomers();
        }
        private static HttpClient _httpClient = new HttpClient();
        static string CreateOrderApiUrl;

        static CosmosClient cosmosClient;
        static Container container;
        static Container portfolios_container;
        static List<customer_id> CustomerIds = new List<customer_id>();
        StockPriceSummary current_price;

        [FunctionName("OrderGenerator")]
        public async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            if (CustomerIds.Count == 0)
            {
                var fake_customers = new Faker<customer_id>()
                    .RuleFor(c => c.id, f => f.Random.Words(3).ToLower().Replace(" ", "-"));
                CustomerIds = fake_customers.Generate(40);
            }

            current_price = await GetCurrentPrices();

            List<Order> GeneratedOrders = new List<Order>();

            foreach (KeyValuePair<string, AvgPrices> stock in current_price.summary)
            {
                var fake_price = new Faker<Order>()
                    .RuleFor(o => o.orderId, f => Guid.NewGuid().ToString())
                    .RuleFor(o => o.customerId, f => f.PickRandom(CustomerIds).id)
                    .RuleFor(o => o.quantity, f => f.Random.Number(1, 2500))
                    .RuleFor(o => o.symbol, stock.Key)
                    .RuleFor(o => o.action, f => f.PickRandom(new string[] { "buy", "sell" }))
                    .RuleFor(o => o.price, (f, o) =>
                        o.action == "buy" ?
                        f.Random.GaussianDecimal((double)stock.Value.avgBidPrice, (double)(stock.Value.avgBidPrice * 0.01m)) :
                        f.Random.GaussianDecimal((double)stock.Value.avgAskPrice, (double)(stock.Value.avgAskPrice * 0.01m)))
                    .RuleFor(o => o.status, f => "created")
                    .RuleFor(o => o.createdAt, f => DateTime.UtcNow);

                GeneratedOrders.AddRange(fake_price.GenerateBetween(0, 50));
            }

            await Parallel.ForEachAsync(GeneratedOrders, async (GeneratedOrder, token) =>
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync<Order>(CreateOrderApiUrl, GeneratedOrder);
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message, ex);
                }

            });

            log.LogInformation($"Generated {GeneratedOrders.Count} Orders at: {DateTime.Now}");
        }

        public async Task<StockPriceSummary> GetCurrentPrices()
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

            return stocks;
        }

        public static void GetCurrentCustomers()
        {
            using FeedIterator<CustomerPortfolio> feed = portfolios_container.GetItemQueryIterator<CustomerPortfolio>(
                queryText: "SELECT * FROM customerPortfolio"
            );

            while (feed.HasMoreResults)
            {
                foreach (var item in feed.ReadNextAsync().Result)
                {
                    CustomerIds.Add(new customer_id { id = item.customerId });
                }
            }
        }
    }
}
