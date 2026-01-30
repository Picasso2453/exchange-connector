using System.CommandLine;
using System.Threading.Channels;
using xws.Core.Dev;
using xws.Core.Env;
using xws.Core.Mux;
using xws.Core.Output;
using xws.Core.Runner;
using xws.Core.Subscriptions;
using xws.Core.WebSocket;
using xws.Exchanges.Hyperliquid;
using xws.Exchanges.Mexc;

Logger.Configure(Console.Error.WriteLine, Console.Error.WriteLine);

var root = new RootCommand("xws CLI");

var muxMaxMessagesOption = new Option<int?>("--max-messages", "Stop after N JSONL messages (exit 0)");
var muxTimeoutSecondsOption = new Option<int?>("--timeout-seconds", "Fail if max messages not reached within T seconds");

var hlCommand = new Command("hl", "Hyperliquid adapter");
var devCommand = new Command("dev", "Developer utilities");
var devEmitCommand = new Command("emit", "Emit deterministic JSONL lines (offline)");
var devCountOption = new Option<int>("--count", "Number of lines to emit")
{
    IsRequired = true
};
var devTimeoutSecondsOption = new Option<int?>("--timeout-seconds", "Fail if not finished within T seconds");
devEmitCommand.AddOption(devCountOption);
devEmitCommand.AddOption(devTimeoutSecondsOption);
devEmitCommand.SetHandler(async (int count, int? timeoutSeconds) =>
{
    if (count <= 0)
    {
        Logger.Error("--count must be greater than 0");
        Environment.ExitCode = 1;
        return;
    }

    using var cts = timeoutSeconds.HasValue
        ? new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds.Value))
        : new CancellationTokenSource();

    var output = new OutputChannel();
    var writerTask = WriteOutputAsync(output.Reader, cts.Token);
    try
    {
        foreach (var line in DevEmitter.BuildLines(count))
        {
            cts.Token.ThrowIfCancellationRequested();
            output.Writer.TryWrite(line);
        }
    }
    catch (OperationCanceledException)
    {
        Logger.Error("dev emit timeout reached");
        Environment.ExitCode = 1;
    }
    finally
    {
        output.Complete();
        await writerTask;
    }
}, devCountOption, devTimeoutSecondsOption);
devCommand.AddCommand(devEmitCommand);
var mexcCommand = new Command("mexc", "MEXC adapter");
var mexcSpotCommand = new Command("spot", "MEXC spot");
var mexcSubscribeCommand = new Command("subscribe", "Subscribe to MEXC streams");
var mexcTradesCommand = new Command("trades", "Subscribe to MEXC spot trades");
var mexcSymbolOption = new Option<string>("--symbol", "Symbol list (comma-separated)")
{
    IsRequired = true
};
mexcTradesCommand.AddOption(mexcSymbolOption);
mexcTradesCommand.AddOption(muxMaxMessagesOption);
mexcTradesCommand.AddOption(muxTimeoutSecondsOption);
mexcTradesCommand.SetHandler(async (string symbol, int? maxMessages, int? timeoutSeconds) =>
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

    var symbols = symbol
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(s => s.ToUpperInvariant())
        .ToArray();

    if (symbols.Length == 0)
    {
        Logger.Error("--symbol requires at least one value");
        Environment.ExitCode = 1;
        return;
    }

    if (symbols.Length > 30)
    {
        Logger.Error("mexc spot supports max 30 subscriptions per connection");
        Environment.ExitCode = 1;
        return;
    }

    var runner = new XwsRunner();
    var writerTask = WriteOutputAsync(runner.Output.Reader, cts.Token);
    try
    {
        var exitCode = await runner.RunMexcSpotTradesAsync(
            symbols,
            maxMessages,
            timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null,
            cts.Token);
        Environment.ExitCode = exitCode;
    }
    catch (Exception ex)
    {
        Logger.Error($"mexc spot subscribe trades failed: {ex.Message}");
        Environment.ExitCode = 1;
    }
    finally
    {
        await writerTask;
    }
}, mexcSymbolOption, muxMaxMessagesOption, muxTimeoutSecondsOption);

var muxCommand = new Command("subscribe", "Subscribe to multiple exchanges");
var muxTradesCommand = new Command("trades", "Mux trades subscriptions");
var muxL2Command = new Command("l2", "Mux l2 orderbook subscriptions");
var muxSubOption = new Option<string[]>(
    "--sub",
    "Subscription spec: <exchange>[.<market>]=SYM1,SYM2 (repeatable)")
{
    Arity = ArgumentArity.OneOrMore
};
var muxFormatOption = new Option<string>("--format", () => "envelope", "Output format: envelope|raw");

muxTradesCommand.AddOption(muxSubOption);
muxTradesCommand.AddOption(muxMaxMessagesOption);
muxTradesCommand.AddOption(muxTimeoutSecondsOption);
muxTradesCommand.AddOption(muxFormatOption);
muxTradesCommand.SetHandler(async (string[] subs, int? maxMessages, int? timeoutSeconds, string format) =>
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

    if (!IsValidFormat(format))
    {
        Logger.Error("--format must be envelope or raw");
        Environment.ExitCode = 1;
        return;
    }

    if (format.Equals("raw", StringComparison.OrdinalIgnoreCase))
    {
        Logger.Error("mux only supports --format envelope");
        Environment.ExitCode = 1;
        return;
    }

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

    var parsed = new List<ParsedSub>();
    foreach (var sub in subs)
    {
        if (!TryParseSub(sub, out var parsedSub))
        {
            Logger.Error($"invalid --sub format: {sub}");
            Environment.ExitCode = 1;
            return;
        }

        parsed.Add(parsedSub);
    }

    var options = new MuxRunnerOptions
    {
        MaxMessages = maxMessages,
        Timeout = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null
    };

    var runner = new XwsRunner();
    var writerTask = WriteOutputAsync(runner.Output.Reader, cts.Token);
    try
    {
        var muxSubs = parsed.Select(p => new MuxSubscription(p.Exchange, p.Market, p.Symbols)).ToList();
        var exitCode = await runner.RunMuxTradesAsync(muxSubs, options, cts.Token);
        Environment.ExitCode = exitCode;
    }
    catch (Exception ex)
    {
        Logger.Error($"mux subscribe trades failed: {ex.Message}");
        Environment.ExitCode = 1;
    }
    finally
    {
        await writerTask;
    }
}, muxSubOption, muxMaxMessagesOption, muxTimeoutSecondsOption, muxFormatOption);

muxL2Command.AddOption(muxSubOption);
muxL2Command.AddOption(muxMaxMessagesOption);
muxL2Command.AddOption(muxTimeoutSecondsOption);
muxL2Command.AddOption(muxFormatOption);
muxL2Command.SetHandler(async (string[] subs, int? maxMessages, int? timeoutSeconds, string format) =>
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

    if (!IsValidFormat(format))
    {
        Logger.Error("--format must be envelope or raw");
        Environment.ExitCode = 1;
        return;
    }

    if (format.Equals("raw", StringComparison.OrdinalIgnoreCase))
    {
        Logger.Error("mux only supports --format envelope");
        Environment.ExitCode = 1;
        return;
    }

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

    var parsed = new List<ParsedSub>();
    foreach (var sub in subs)
    {
        if (!TryParseSub(sub, out var parsedSub))
        {
            Logger.Error($"invalid --sub format: {sub}");
            Environment.ExitCode = 1;
            return;
        }

        parsed.Add(parsedSub);
    }

    var options = new MuxRunnerOptions
    {
        MaxMessages = maxMessages,
        Timeout = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null
    };

    var runner = new XwsRunner();
    var writerTask = WriteOutputAsync(runner.Output.Reader, cts.Token);
    try
    {
        var muxSubs = parsed.Select(p => new MuxSubscription(p.Exchange, p.Market, p.Symbols)).ToList();
        var exitCode = await runner.RunMuxL2Async(muxSubs, options, cts.Token);
        Environment.ExitCode = exitCode;
    }
    catch (Exception ex)
    {
        Logger.Error($"mux subscribe l2 failed: {ex.Message}");
        Environment.ExitCode = 1;
    }
    finally
    {
        await writerTask;
    }
}, muxSubOption, muxMaxMessagesOption, muxTimeoutSecondsOption, muxFormatOption);

muxCommand.AddCommand(muxTradesCommand);
muxCommand.AddCommand(muxL2Command);

var hlSymbolsCommand = new Command("symbols", "List available symbols/instruments");
var symbolsFilterOption = new Option<string?>("--filter", "Filter by substring");
hlSymbolsCommand.AddOption(symbolsFilterOption);
hlSymbolsCommand.SetHandler(async (string? filter) =>
{
    var output = new OutputChannel();
    var writerTask = WriteOutputAsync(output.Reader, CancellationToken.None);
    var writer = new JsonlWriter(line => output.Writer.TryWrite(line));

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
    finally
    {
        output.Complete();
        await writerTask;
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

    var runner = new XwsRunner();
    var writerTask = WriteOutputAsync(runner.Output.Reader, cts.Token);
    try
    {
        var options = new WebSocketRunnerOptions
        {
            MaxMessages = maxMessages,
            Timeout = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null
        };

        var exitCode = await runner.RunHlTradesAsync(symbol, options, format, cts.Token);
        Environment.ExitCode = exitCode;
    }
    catch (Exception ex)
    {
        Logger.Error($"hl subscribe trades failed: {ex.Message}");
        Environment.ExitCode = 1;
    }
    finally
    {
        await writerTask;
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

    var runner = new XwsRunner();
    var writerTask = WriteOutputAsync(runner.Output.Reader, cts.Token);
    try
    {
        var options = new WebSocketRunnerOptions
        {
            MaxMessages = maxMessages,
            Timeout = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null
        };

        var exitCode = await runner.RunHlPositionsAsync(user, options, format, cts.Token);
        Environment.ExitCode = exitCode;
    }
    catch (Exception ex)
    {
        Logger.Error($"hl subscribe positions failed: {ex.Message}");
        Environment.ExitCode = 1;
    }
    finally
    {
        await writerTask;
    }
}, maxMessagesOption, timeoutSecondsOption, formatOption);

hlSubscribeCommand.AddCommand(tradesCommand);
hlSubscribeCommand.AddCommand(positionsCommand);

hlCommand.AddCommand(hlSymbolsCommand);
hlCommand.AddCommand(hlSubscribeCommand);

mexcSubscribeCommand.AddCommand(mexcTradesCommand);
mexcSpotCommand.AddCommand(mexcSubscribeCommand);
mexcCommand.AddCommand(mexcSpotCommand);

root.AddCommand(hlCommand);
root.AddCommand(devCommand);
root.AddCommand(mexcCommand);
root.AddCommand(muxCommand);

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

static async Task WriteOutputAsync(ChannelReader<string> reader, CancellationToken cancellationToken)
{
    try
    {
        await foreach (var line in reader.ReadAllAsync(cancellationToken))
        {
            Console.Out.WriteLine(line);
        }
    }
    catch (OperationCanceledException)
    {
    }
}

sealed record ParsedSub(string Exchange, string? Market, string[] Symbols);
