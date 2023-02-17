using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using System.Linq;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using trading_model;

using Bogus;
using Bogus.Distributions.Gaussian;
using System.Diagnostics.SymbolStore;
using Newtonsoft.Json;
using System.IO;
using System.Net;

namespace order_executor.DataGenerators
{

    public class sym
    {
        // Just a holder so we can more easily generate symbols
        public string symbol { get; set; }
    }
    public class MarketDataGenerator
    {
        static MarketDataGenerator()
        {
            //Instance CosmosClient
            cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), new CosmosClientOptions() { AllowBulkExecution = true });
            container = cosmosClient.GetContainer("trading", "stockPriceSummary");
        }

        static CosmosClient cosmosClient;
        static Container container;
        StockPriceSummary current_price;

        [FunctionName("MarketDataGenerator")]
        public async Task Run([TimerTrigger("0/5 * * * * *")]TimerInfo myTimer,
            [EventHub("marketdata", Connection = "ordersHubConnection")] IAsyncCollector<MarketDataFeed> outputMarketData, 
            ILogger log)
        { 
            current_price = await GetCurrentPrices();

            if (current_price == null || current_price.summary.Count == 0) 
            {
                List<sym> symbols = new List<sym>();
                symbols.Add(new sym { symbol =  "MSFT" });
                var fake_symbols = new Faker<sym>()
                    .RuleFor(o => o.symbol, f => f.Random.String(7, 'A', 'Z').Substring(0, f.Random.Int(2, 6)));

                symbols.AddRange(fake_symbols.Generate(19));

                foreach(sym symbol_hold in symbols)
                {
                    string symbol = symbol_hold.symbol;
                    var fake_price = new Faker<MarketDataFeed>()
                        .RuleFor(o => o.timestamp, DateTime.UtcNow)
                        .RuleFor(o => o.symbol, symbol)
                        .RuleFor(o => o.avgBidPrice, f => f.Finance.Amount(5, 5000, 2))
                        .RuleFor(o => o.avgAskPrice, (f, o) => o.avgBidPrice + (o.avgBidPrice * f.Random.Decimal(0.005m, 0.15m)));
                    await outputMarketData.AddAsync(fake_price.Generate());
                }
            } else
            {
                foreach(KeyValuePair<string, AvgPrices> stock in current_price.summary)
                {
                    var fake_price = new Faker<MarketDataFeed>()
                        .RuleFor(o => o.timestamp, DateTime.UtcNow)
                        .RuleFor(o => o.symbol, stock.Key)
                        .RuleFor(o => o.avgBidPrice, f => f.Random.GaussianDecimal((double)stock.Value.avgBidPrice, (double)(stock.Value.avgBidPrice * 0.05m)))
                        .RuleFor(o => o.avgAskPrice, (f, o) => o.avgBidPrice + (o.avgBidPrice * f.Random.Decimal(0.005m, 0.15m)));

                    List<MarketDataFeed> generated = fake_price.GenerateBetween(1, 20);
                    await Parallel.ForEachAsync(generated, async (marketdata, token) =>
                    {
                        try
                        {
                            await outputMarketData.AddAsync(marketdata);
                        }
                        catch (Exception ex)
                        {
                            log.LogError(ex.Message, ex);
                        }
                    });
                }
            }


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
    }
}
