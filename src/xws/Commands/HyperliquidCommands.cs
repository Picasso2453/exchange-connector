using System.CommandLine;

using Xws.Data.Env;
using Xws.Data.Output;
using xws.Runner;
using Xws.Data.Shared.Logging;
using Xws.Data.Subscriptions;
using Xws.Data.WebSocket;
using Xws.Exchanges.Hyperliquid;
using Xws.Exchanges.Hyperliquid.Rest;

namespace xws.Commands;

public static class HyperliquidCommands
{
    public static Command Build()
    {
        var hlCommand = new Command("hl", "Hyperliquid adapter");
        hlCommand.AddCommand(BuildSymbolsCommand());
        hlCommand.AddCommand(BuildSubscribeCommand());
        return hlCommand;
    }

    private static Command BuildSymbolsCommand()
    {
        var hlSymbolsCommand = new Command("symbols", "List available symbols/instruments");
        var symbolsFilterOption = new Option<string?>("--filter", "Filter by substring");
        hlSymbolsCommand.AddOption(symbolsFilterOption);
        hlSymbolsCommand.SetHandler(async (string? filter) =>
        {
            var ok = true;
            try
            {
                try
                {
                    var config = HyperliquidConfig.Load();
                    Logger.Info($"symbols: posting to {config.HttpUri}");

                    var meta = await HLRestClient.PostInfoAsync(config.HttpUri, "meta", CancellationToken.None);
                    if (Console.IsOutputRedirected && string.IsNullOrWhiteSpace(filter))
                    {
                        if (ShouldEmit(meta, filter))
                        {
                            Console.Out.WriteLine(meta);
                        }

                        Environment.Exit(0);
                    }

                    var output = new OutputChannel();
                    var writerTask = CommandHelpers.WriteOutputAsync(output.Reader, CancellationToken.None);
                    var writer = new JsonlWriter(line => output.Writer.TryWrite(line));
                    try
                    {
                        if (ShouldEmit(meta, filter))
                        {
                            writer.WriteLine(meta);
                        }

                        var spotMeta = await HLRestClient.PostInfoAsync(config.HttpUri, "spotMeta", CancellationToken.None);
                        if (ShouldEmit(spotMeta, filter))
                        {
                            writer.WriteLine(spotMeta);
                        }
                    }
                    finally
                    {
                        output.Complete();
                        await writerTask;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"HL symbols failed. {ex.Message}. Retry or check connectivity.");
                    Environment.ExitCode = 2;
                    ok = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"HL symbols failed. {ex.Message}. Retry or check connectivity.");
                Environment.ExitCode = 2;
            }

            if (ok && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 0;
            }
        }, symbolsFilterOption);
        return hlSymbolsCommand;
    }

    private static Command BuildSubscribeCommand()
    {
        var hlSubscribeCommand = new Command("subscribe", "Subscribe to Hyperliquid streams");
        var tradesSymbolOption = new Option<string>("--symbol", "Native coin symbol")
        {
            IsRequired = true
        };
        var candleIntervalOption = new Option<string>("--interval", "Candle interval (e.g., 1m, 5m, 15m, 1h, 4h, 1d)")
        {
            IsRequired = true
        };
        var maxMessagesOption = new Option<int?>("--max-messages", "Stop after N JSONL messages (exit 0)");
        var timeoutSecondsOption = new Option<int?>("--timeout-seconds", "Fail if max messages not reached within T seconds");
        var formatOption = new Option<string>("--format", () => "envelope", "Output format: envelope|raw");

        hlSubscribeCommand.AddCommand(BuildStreamCommand(
            "trades",
            "Subscribe to trades stream",
            tradesSymbolOption,
            maxMessagesOption,
            timeoutSecondsOption,
            formatOption,
            (runner, options, format, symbol, token) => runner.RunHlTradesAsync(symbol, options, format, token)));

        hlSubscribeCommand.AddCommand(BuildStreamCommand(
            "l2",
            "Subscribe to L2 orderbook stream",
            tradesSymbolOption,
            maxMessagesOption,
            timeoutSecondsOption,
            formatOption,
            (runner, options, format, symbol, token) => runner.RunHlL2Async(symbol, options, format, token)));

        hlSubscribeCommand.AddCommand(BuildCandleCommand(
            tradesSymbolOption,
            candleIntervalOption,
            maxMessagesOption,
            timeoutSecondsOption,
            formatOption));

        hlSubscribeCommand.AddCommand(BuildPositionsCommand(
            maxMessagesOption,
            timeoutSecondsOption,
            formatOption));

        return hlSubscribeCommand;
    }

    private static Command BuildStreamCommand(
        string name,
        string description,
        Option<string> tradesSymbolOption,
        Option<int?> maxMessagesOption,
        Option<int?> timeoutSecondsOption,
        Option<string> formatOption,
        Func<XwsRunner, WebSocketRunnerOptions, string, string, CancellationToken, Task<int>> run)
    {
        var command = new Command(name, description);
        command.AddOption(tradesSymbolOption);
        command.AddOption(maxMessagesOption);
        command.AddOption(timeoutSecondsOption);
        command.AddOption(formatOption);
        command.SetHandler(async (string symbol, int? maxMessages, int? timeoutSeconds, string format) =>
        {
            try
            {
                using var cts = CommandHelpers.CreateShutdownCts();

                if (!CommandHelpers.ValidateMaxMessagesTimeout(maxMessages, timeoutSeconds))
                {
                    Environment.ExitCode = 1;
                    return;
                }

                if (!CommandHelpers.ValidateFormat(format))
                {
                    Environment.ExitCode = 1;
                    return;
                }

                if (!SymbolValidation.IsValidSymbol("hl", null, symbol, out var symbolError))
                {
                    Logger.Error($"Invalid --symbol. {symbolError}. Provide an exchange-native symbol.");
                    Environment.ExitCode = 1;
                    return;
                }

                var runner = new XwsRunner();
                var writerTask = CommandHelpers.WriteOutputAsync(runner.Output.Reader, cts.Token);
                try
                {
                    var options = new WebSocketRunnerOptions
                    {
                        MaxMessages = maxMessages,
                        Timeout = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null
                    };

                    var exitCode = await run(runner, options, format, symbol, cts.Token);
                    Environment.ExitCode = exitCode;
                }
                catch (Exception ex)
                {
                    Logger.Error($"HL subscribe {name} failed. {ex.Message}. Check connectivity and retry.");
                    Environment.ExitCode = 2;
                }
                finally
                {
                    await writerTask;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"HL subscribe {name} failed. {ex.Message}. Check connectivity and retry.");
                Environment.ExitCode = 2;
            }
        }, tradesSymbolOption, maxMessagesOption, timeoutSecondsOption, formatOption);

        return command;
    }

    private static Command BuildCandleCommand(
        Option<string> tradesSymbolOption,
        Option<string> candleIntervalOption,
        Option<int?> maxMessagesOption,
        Option<int?> timeoutSecondsOption,
        Option<string> formatOption)
    {
        var command = new Command("candle", "Subscribe to candle (OHLCV) stream");
        command.AddOption(tradesSymbolOption);
        command.AddOption(candleIntervalOption);
        command.AddOption(maxMessagesOption);
        command.AddOption(timeoutSecondsOption);
        command.AddOption(formatOption);
        command.SetHandler(async (string symbol, string interval, int? maxMessages, int? timeoutSeconds, string format) =>
        {
            try
            {
                using var cts = CommandHelpers.CreateShutdownCts();

                if (!CommandHelpers.ValidateMaxMessagesTimeout(maxMessages, timeoutSeconds))
                {
                    Environment.ExitCode = 1;
                    return;
                }

                if (!CommandHelpers.ValidateFormat(format))
                {
                    Environment.ExitCode = 1;
                    return;
                }

                if (!SymbolValidation.IsValidSymbol("hl", null, symbol, out var symbolError))
                {
                    Logger.Error($"Invalid --symbol. {symbolError}. Provide an exchange-native symbol.");
                    Environment.ExitCode = 1;
                    return;
                }

                var runner = new XwsRunner();
                var writerTask = CommandHelpers.WriteOutputAsync(runner.Output.Reader, cts.Token);
                try
                {
                    var options = new WebSocketRunnerOptions
                    {
                        MaxMessages = maxMessages,
                        Timeout = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null
                    };

                    var exitCode = await runner.RunHlCandleAsync(symbol, interval, options, format, cts.Token);
                    Environment.ExitCode = exitCode;
                }
                catch (Exception ex)
                {
                    Logger.Error($"HL subscribe candle failed. {ex.Message}. Check connectivity and retry.");
                    Environment.ExitCode = 2;
                }
                finally
                {
                    await writerTask;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"HL subscribe candle failed. {ex.Message}. Check connectivity and retry.");
                Environment.ExitCode = 2;
            }
        }, tradesSymbolOption, candleIntervalOption, maxMessagesOption, timeoutSecondsOption, formatOption);

        return command;
    }

    private static Command BuildPositionsCommand(
        Option<int?> maxMessagesOption,
        Option<int?> timeoutSecondsOption,
        Option<string> formatOption)
    {
        var command = new Command("positions", "Subscribe to positions/account stream");
        command.AddOption(maxMessagesOption);
        command.AddOption(timeoutSecondsOption);
        command.AddOption(formatOption);
        command.SetHandler(async (int? maxMessages, int? timeoutSeconds, string format) =>
        {
            try
            {
                using var cts = CommandHelpers.CreateShutdownCts();

                if (!CommandHelpers.ValidateMaxMessagesTimeout(maxMessages, timeoutSeconds))
                {
                    Environment.ExitCode = 1;
                    return;
                }

                if (!CommandHelpers.ValidateFormat(format))
                {
                    Environment.ExitCode = 1;
                    return;
                }

                var user = EnvReader.GetOptional("XWS_HL_USER");
                if (string.IsNullOrWhiteSpace(user))
                {
                    Logger.Error("Missing required env var: XWS_HL_USER. Positions stream requires a wallet address. Set XWS_HL_USER in .env or environment.");
                    Environment.ExitCode = 1;
                    return;
                }

                var runner = new XwsRunner();
                var writerTask = CommandHelpers.WriteOutputAsync(runner.Output.Reader, cts.Token);
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
                    Logger.Error($"HL subscribe positions failed. {ex.Message}. Check connectivity and retry.");
                    Environment.ExitCode = 2;
                }
                finally
                {
                    await writerTask;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"HL subscribe positions failed. {ex.Message}. Check connectivity and retry.");
                Environment.ExitCode = 2;
            }
        }, maxMessagesOption, timeoutSecondsOption, formatOption);

        return command;
    }

    private static bool ShouldEmit(string json, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return json.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }
}
