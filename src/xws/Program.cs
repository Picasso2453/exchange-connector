using System.CommandLine;
using xws.Core.Env;
using xws.Core.Output;
using xws.Core.Subscriptions;
using xws.Core.WebSocket;
using xws.Exchanges.Hyperliquid;

var root = new RootCommand("xws CLI");

var hlCommand = new Command("hl", "Hyperliquid adapter");

var hlSymbolsCommand = new Command("symbols", "List available symbols/instruments");
var symbolsFilterOption = new Option<string?>("--filter", "Filter by substring");
hlSymbolsCommand.AddOption(symbolsFilterOption);
hlSymbolsCommand.SetHandler(async (string? filter) =>
{
    var writer = new JsonlWriter();

    try
    {
        var config = HyperliquidConfig.Load();
        Logger.Info($"symbols: posting to {config.HttpUri}");

        var meta = await HyperliquidHttp.PostInfoAsync(config.HttpUri, "meta", CancellationToken.None);
        if (ShouldEmit(meta, filter))
        {
            writer.WriteLine(meta);
        }

        var spotMeta = await HyperliquidHttp.PostInfoAsync(config.HttpUri, "spotMeta", CancellationToken.None);
        if (ShouldEmit(spotMeta, filter))
        {
            writer.WriteLine(spotMeta);
        }
    }
    catch (Exception ex)
    {
        Logger.Error($"hl symbols failed: {ex.Message}");
        Environment.ExitCode = 1;
    }
}, symbolsFilterOption);

var hlSubscribeCommand = new Command("subscribe", "Subscribe to Hyperliquid streams");

var tradesCommand = new Command("trades", "Subscribe to trades stream");
var tradesSymbolOption = new Option<string>("--symbol", "Native coin symbol")
{
    IsRequired = true
};
var maxMessagesOption = new Option<int?>("--max-messages", "Stop after N JSONL messages (exit 0)");
var timeoutSecondsOption = new Option<int?>("--timeout-seconds", "Fail if max messages not reached within T seconds");
var formatOption = new Option<string>("--format", () => "envelope", "Output format: envelope|raw");
tradesCommand.AddOption(tradesSymbolOption);
tradesCommand.AddOption(maxMessagesOption);
tradesCommand.AddOption(timeoutSecondsOption);
tradesCommand.AddOption(formatOption);
tradesCommand.SetHandler(async (string symbol, int? maxMessages, int? timeoutSeconds, string format) =>
{
    using var cts = new CancellationTokenSource();
    var cancelLogged = 0;
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        if (Interlocked.Exchange(ref cancelLogged, 1) == 0)
        {
            Logger.Info("shutdown requested");
        }
        cts.Cancel();
    };

    if (maxMessages.HasValue && maxMessages.Value <= 0)
    {
        Logger.Error("--max-messages must be greater than 0");
        Environment.ExitCode = 1;
        return;
    }

    if (timeoutSeconds.HasValue && timeoutSeconds.Value <= 0)
    {
        Logger.Error("--timeout-seconds must be greater than 0");
        Environment.ExitCode = 1;
        return;
    }

    if (timeoutSeconds.HasValue && !maxMessages.HasValue)
    {
        Logger.Error("--timeout-seconds requires --max-messages");
        Environment.ExitCode = 1;
        return;
    }

    if (!IsValidFormat(format))
    {
        Logger.Error("--format must be envelope or raw");
        Environment.ExitCode = 1;
        return;
    }

    try
    {
        var config = HyperliquidConfig.Load();
        var subscription = HyperliquidWs.BuildTradesSubscription(symbol);
        IJsonlWriter writer = format == "raw"
            ? new JsonlWriter()
            : new EnvelopeWriter("hl", "trades", null, new[] { symbol });
        var registry = new SubscriptionRegistry();
        registry.Add(subscription);
        var runner = new WebSocketRunner(writer, registry);
        var options = new WebSocketRunnerOptions
        {
            MaxMessages = maxMessages,
            Timeout = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null
        };

        var exitCode = await runner.RunAsync(config.WsUri, options, cts.Token);
        Environment.ExitCode = exitCode;
    }
    catch (Exception ex)
    {
        Logger.Error($"hl subscribe trades failed: {ex.Message}");
        Environment.ExitCode = 1;
    }
}, tradesSymbolOption, maxMessagesOption, timeoutSecondsOption, formatOption);

var positionsCommand = new Command("positions", "Subscribe to positions/account stream");
positionsCommand.AddOption(maxMessagesOption);
positionsCommand.AddOption(timeoutSecondsOption);
positionsCommand.AddOption(formatOption);
positionsCommand.SetHandler(async (int? maxMessages, int? timeoutSeconds, string format) =>
{
    using var cts = new CancellationTokenSource();
    var cancelLogged = 0;
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        if (Interlocked.Exchange(ref cancelLogged, 1) == 0)
        {
            Logger.Info("shutdown requested");
        }
        cts.Cancel();
    };

    if (maxMessages.HasValue && maxMessages.Value <= 0)
    {
        Logger.Error("--max-messages must be greater than 0");
        Environment.ExitCode = 1;
        return;
    }

    if (timeoutSeconds.HasValue && timeoutSeconds.Value <= 0)
    {
        Logger.Error("--timeout-seconds must be greater than 0");
        Environment.ExitCode = 1;
        return;
    }

    if (timeoutSeconds.HasValue && !maxMessages.HasValue)
    {
        Logger.Error("--timeout-seconds requires --max-messages");
        Environment.ExitCode = 1;
        return;
    }

    if (!IsValidFormat(format))
    {
        Logger.Error("--format must be envelope or raw");
        Environment.ExitCode = 1;
        return;
    }

    var user = EnvReader.GetOptional("XWS_HL_USER");
    if (string.IsNullOrWhiteSpace(user))
    {
        Logger.Error("missing required env var: XWS_HL_USER");
        Environment.ExitCode = 1;
        return;
    }

    try
    {
        var config = HyperliquidConfig.Load();
        var subscription = HyperliquidWs.BuildClearinghouseStateSubscription(user);
        var writer = new JsonlWriter();
        var registry = new SubscriptionRegistry();
        registry.Add(subscription);
        var runner = new WebSocketRunner(writer, registry);
        var options = new WebSocketRunnerOptions
        {
            MaxMessages = maxMessages,
            Timeout = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null
        };

        var exitCode = await runner.RunAsync(config.WsUri, options, cts.Token);
        Environment.ExitCode = exitCode;
    }
    catch (Exception ex)
    {
        Logger.Error($"hl subscribe positions failed: {ex.Message}");
        Environment.ExitCode = 1;
    }
}, maxMessagesOption, timeoutSecondsOption, formatOption);

hlSubscribeCommand.AddCommand(tradesCommand);
hlSubscribeCommand.AddCommand(positionsCommand);

hlCommand.AddCommand(hlSymbolsCommand);
hlCommand.AddCommand(hlSubscribeCommand);

root.AddCommand(hlCommand);

return await root.InvokeAsync(args);

static bool ShouldEmit(string json, string? filter)
{
    if (string.IsNullOrWhiteSpace(filter))
    {
        return true;
    }

    return json.Contains(filter, StringComparison.OrdinalIgnoreCase);
}

static bool IsValidFormat(string? format)
{
    return string.Equals(format, "envelope", StringComparison.OrdinalIgnoreCase)
        || string.Equals(format, "raw", StringComparison.OrdinalIgnoreCase);
}

static bool TryParseSub(string input, out ParsedSub parsed)
{
    parsed = new ParsedSub(string.Empty, null, Array.Empty<string>());
    if (string.IsNullOrWhiteSpace(input))
    {
        return false;
    }

    var parts = input.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 2)
    {
        return false;
    }

    var exchangePart = parts[0].Trim();
    var symbolsPart = parts[1].Trim();
    if (string.IsNullOrWhiteSpace(exchangePart) || string.IsNullOrWhiteSpace(symbolsPart))
    {
        return false;
    }

    var exchangePieces = exchangePart.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
    var exchange = exchangePieces[0].Trim();
    var market = exchangePieces.Length > 1 ? exchangePieces[1].Trim() : null;
    if (string.IsNullOrWhiteSpace(exchange))
    {
        return false;
    }

    var symbols = symbolsPart
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .ToArray();
    if (symbols.Length == 0)
    {
        return false;
    }

    parsed = new ParsedSub(exchange, market, symbols);
    return true;
}

sealed record ParsedSub(string Exchange, string? Market, string[] Symbols);
