using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Bogus;
using Microsoft.Extensions.Configuration;
using trading_model;

public partial class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("To STOP press CTRL+C...");

        var configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile($"settings.json");

        var config = configuration.Build();

        string? connection = config.GetRequiredSection("eventHubConnection").Value;
        string? topic = config.GetRequiredSection("eventHubTopic").Value;

        if(string.IsNullOrEmpty(connection) || string.IsNullOrEmpty(topic) )
        {
            Console.WriteLine("Missing connection string or topic settings. Aborting...");
            return;
        }

        Console.CancelKeyPress += Console_CancelKeyPress1;

        CancellationToken cancellation = tokenSource.Token;
        await SendAsync(connection, topic, 1000, cancellation);

        Console.WriteLine("Stopped!");
    }

    static CancellationTokenSource tokenSource = new CancellationTokenSource();

    static Dictionary<string, (decimal, decimal)> symbols = new Dictionary<string, (decimal, decimal)>()
    {
        { "MSFT", (250, 300) },
        { "ABCD", (100, 130) },
        { "XYZ1", (1200, 1250) },
        { "ABC1", (80, 120) },
        { "DCBA", (500, 600) }
    };

    static void Console_CancelKeyPress1(object? sender, ConsoleCancelEventArgs e)
    {
        Console.WriteLine("Stopping...");
        e.Cancel = true;
        tokenSource.Cancel();
    }

    static async Task SendAsync(string connection, string topic, int interval, CancellationToken cancellation)
    {
        var hubClient = new EventHubProducerClient(connection, topic);

        var tasks = new List<Task>();

        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                using (var batch = await hubClient.CreateBatchAsync())
                {
                    for (int i = 0; i < 5000; i++)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        var data = new Faker<MarketDataFeed>()
                            .RuleFor(u => u.symbol, (f, u) => f.PickRandom<string>(symbols.Keys))
                            .RuleFor(u => u.timestamp, (f, u) => DateTime.UtcNow)
                            .RuleFor(u => u.avgBidPrice, (f, u) => f.Finance.Amount(symbols[u.symbol].Item1, symbols[u.symbol].Item2, 2))
                            .RuleFor(u => u.avgAskPrice, (f, u) => f.Finance.Amount(symbols[u.symbol].Item1, symbols[u.symbol].Item2, 2));

                        if (!batch.TryAdd(new EventData(new BinaryData(data.Generate()))))
                        {
                            Console.WriteLine($"Batch size exceeded: {i}");
                            break;
                        }
                    }

                    tasks.Add(hubClient.SendAsync(batch));

                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }                

                await Task.Delay(interval);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sending message failed: {ex.Message}");
        }
        finally
        {
            await hubClient.DisposeAsync();
        }
    }
}