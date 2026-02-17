using System.Reflection;
using System.Text.Json;
using Connector.Core;
using Connector.Core.Abstractions;
using Connector.Core.Contracts;
using Connector.Core.Exchanges;
using Connector.Core.Exchanges.Hyperliquid;
using Connector.Core.Managers;
using Connector.Core.Transport;
using Microsoft.Extensions.Logging;

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp(version);
    return 0;
}

if (args.Contains("--version") || args.Contains("-v"))
{
    Console.WriteLine(version);
    return 0;
}

// Parse args
var config = ParseArgs(args);
if (config is null) return 1;

// Set up logging (stderr only)
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole(opts => opts.LogToStandardErrorThreshold = LogLevel.Trace);
    builder.SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("Connector.Cli");

// Set up exchange
var hlConfig = HyperliquidConfig.FromEnvironment();
var registry = new ExchangeRegistry();
registry.Register(new HyperliquidAdapter(hlConfig, loggerFactory, config.IncludeRaw));

IExchangeAdapter adapter;
try
{
    adapter = registry.Get(config.Exchange);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

// Set up auth
IAuthProvider authProvider = config.NoAuth
    ? new NoAuthProvider()
    : new NoAuthProvider(); // TODO: real auth provider in S10

// Set up transport
var wsTransport = new WsTransport(loggerFactory.CreateLogger<WsTransport>());
var wsTranslator = adapter.CreateWsTranslator();
var rateLimiter = new TokenBucketRateLimiter(10, TimeSpan.FromSeconds(1));

await using var wsManager = new WebSocketManager(
    wsTransport, wsTranslator, authProvider,
    loggerFactory.CreateLogger<WebSocketManager>(),
    rateLimiter);

// Handle Ctrl+C
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.Error.WriteLine("Shutting down...");
};

try
{
    // Connect
    var wsUri = new Uri(hlConfig.WsUrl);
    logger.LogInformation("Starting connector for {Exchange} ({Network})", config.Exchange, hlConfig.Network);
    await wsManager.StartAsync(wsUri, cts.Token);

    // Subscribe
    foreach (var channel in config.Channels)
    {
        var subReq = new UnifiedWsSubscribeRequest
        {
            CorrelationId = Guid.NewGuid().ToString("N"),
            Exchange = config.Exchange,
            Channel = channel,
            Symbols = config.Symbols
        };
        await wsManager.SubscribeAsync(subReq, cts.Token);
        logger.LogInformation("Subscribed to {Channel} for {Symbols}", channel, string.Join(",", config.Symbols));
    }

    // Read events → JSONL to stdout
    var reader = wsManager.GetEventReader();
    while (await reader.WaitToReadAsync(cts.Token))
    {
        while (reader.TryRead(out var evt))
        {
            var json = JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions.Default);
            Console.Out.WriteLine(json);
        }
    }
}
catch (OperationCanceledException)
{
    // Clean shutdown
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error");
    return 2;
}

logger.LogInformation("Connector stopped");
return 0;

// --- Arg parsing ---

static ConnectorConfig? ParseArgs(string[] args)
{
    string? exchange = null, symbols = null, channels = null, configPath = null;
    bool noAuth = false, raw = false;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--exchange" when i + 1 < args.Length: exchange = args[++i]; break;
            case "--symbols" when i + 1 < args.Length: symbols = args[++i]; break;
            case "--channels" when i + 1 < args.Length: channels = args[++i]; break;
            case "--config" when i + 1 < args.Length: configPath = args[++i]; break;
            case "--no-auth": noAuth = true; break;
            case "--raw": raw = true; break;
            default:
                Console.Error.WriteLine($"Unknown argument: {args[i]}");
                return null;
        }
    }

    if (exchange is null) { Console.Error.WriteLine("Error: --exchange required"); return null; }
    if (symbols is null) { Console.Error.WriteLine("Error: --symbols required"); return null; }
    if (channels is null) { Console.Error.WriteLine("Error: --channels required"); return null; }

    if (!TryParseExchange(exchange, out var exchangeEnum))
    {
        Console.Error.WriteLine($"Error: Unknown exchange '{exchange}'. Supported: hyperliquid");
        return null;
    }

    var channelList = channels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var parsedChannels = new List<UnifiedWsChannel>();
    foreach (var ch in channelList)
    {
        if (!TryParseChannel(ch, out var wsChannel))
        {
            Console.Error.WriteLine($"Error: Unknown channel '{ch}'. Supported: trades, l2, candles, userOrders, fills");
            return null;
        }
        parsedChannels.Add(wsChannel);
    }

    return new ConnectorConfig
    {
        Exchange = exchangeEnum,
        Symbols = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        Channels = parsedChannels.ToArray(),
        NoAuth = noAuth,
        IncludeRaw = raw,
        ConfigFilePath = configPath
    };
}

static bool TryParseExchange(string name, out UnifiedExchange result)
{
    result = name.ToLowerInvariant() switch
    {
        "hyperliquid" or "hl" => UnifiedExchange.Hyperliquid,
        "bybit" => UnifiedExchange.Bybit,
        "mexc" => UnifiedExchange.Mexc,
        _ => (UnifiedExchange)(-1)
    };
    return (int)result >= 0;
}

static bool TryParseChannel(string name, out UnifiedWsChannel result)
{
    result = name.ToLowerInvariant() switch
    {
        "trades" => UnifiedWsChannel.Trades,
        "l1" or "orderbookl1" => UnifiedWsChannel.OrderBookL1,
        "l2" or "orderbookl2" => UnifiedWsChannel.OrderBookL2,
        "candles" => UnifiedWsChannel.Candles,
        "userorders" or "orders" => UnifiedWsChannel.UserOrders,
        "fills" => UnifiedWsChannel.Fills,
        "positions" => UnifiedWsChannel.Positions,
        "balances" => UnifiedWsChannel.Balances,
        _ => (UnifiedWsChannel)(-1)
    };
    return (int)result >= 0;
}

static void PrintHelp(string version)
{
    Console.Error.WriteLine($"""
        Connector.Cli v{version}
        Exchange WebSocket/REST connector daemon.

        Usage:
          connector [options]

        Options:
          --exchange <name>    Exchange to connect to (hyperliquid, bybit, mexc)
          --symbols <list>     Comma-separated symbol list (e.g. BTC,ETH,SOL)
          --channels <list>    Comma-separated channels (trades,l2,candles,userOrders,fills)
          --config <path>      Path to JSON config file (optional)
          --no-auth            Run without authentication (public data only)
          --raw                Include raw exchange payloads in events
          --help, -h           Show this help
          --version, -v        Show version

        Examples:
          connector --exchange hyperliquid --symbols BTC,ETH --channels trades --no-auth
          connector --exchange hl --symbols SOL --channels trades,candles --raw
        """);
}
