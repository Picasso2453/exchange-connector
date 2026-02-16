using System.CommandLine;

using Xws.Data.Output;
using xws.Runner;
using Xws.Data.Shared.Logging;
using Xws.Exchanges.Mexc;

namespace xws.Commands;

public static class MexcCommands
{
    public static Command Build()
    {
        var mexcCommand = new Command("mexc", "MEXC adapter");
        var mexcSpotCommand = new Command("spot", "MEXC spot");
        var mexcSubscribeCommand = new Command("subscribe", "Subscribe to MEXC streams");
        var mexcTradesCommand = new Command("trades", "Subscribe to MEXC spot trades");
        var mexcSymbolOption = new Option<string>("--symbol", "Symbol list (comma-separated)")
        {
            IsRequired = true
        };
        var muxMaxMessagesOption = new Option<int?>("--max-messages", "Stop after N JSONL messages (exit 0)");
        var muxTimeoutSecondsOption = new Option<int?>("--timeout-seconds", "Fail if max messages not reached within T seconds");

        mexcTradesCommand.AddOption(mexcSymbolOption);
        mexcTradesCommand.AddOption(muxMaxMessagesOption);
        mexcTradesCommand.AddOption(muxTimeoutSecondsOption);
        mexcTradesCommand.SetHandler(async (string symbol, int? maxMessages, int? timeoutSeconds) =>
        {
            try
            {
                using var cts = CommandHelpers.CreateShutdownCts();

                if (!CommandHelpers.ValidateMaxMessagesTimeout(maxMessages, timeoutSeconds))
                {
                    Environment.ExitCode = 1;
                    return;
                }

                var symbols = symbol
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToUpperInvariant())
                    .ToArray();

                if (symbols.Length == 0)
                {
                    Logger.Error("Invalid --symbol. At least one symbol is required. Provide a comma-separated list.");
                    Environment.ExitCode = 1;
                    return;
                }

                if (symbols.Length > 30)
                {
                    Logger.Error("Too many symbols. MEXC spot supports max 30 subscriptions per connection. Reduce --symbol values or use multiple runs.");
                    Environment.ExitCode = 1;
                    return;
                }

                if (!SymbolValidation.ValidateSymbols("mexc", "spot", symbols, out var symbolError))
                {
                    Logger.Error($"Invalid --symbol. {symbolError}. Provide exchange-native symbols.");
                    Environment.ExitCode = 1;
                    return;
                }

                var runner = new XwsRunner();
                var writerTask = CommandHelpers.WriteOutputAsync(runner.Output.Reader, cts.Token);
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
                    Logger.Error($"MEXC spot subscribe trades failed. {ex.Message}. Check connectivity and retry.");
                    Environment.ExitCode = 2;
                }
                finally
                {
                    await writerTask;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"MEXC spot subscribe trades failed. {ex.Message}. Check connectivity and retry.");
                Environment.ExitCode = 2;
            }
        }, mexcSymbolOption, muxMaxMessagesOption, muxTimeoutSecondsOption);

        mexcSubscribeCommand.AddCommand(mexcTradesCommand);
        mexcSpotCommand.AddCommand(mexcSubscribeCommand);
        mexcCommand.AddCommand(mexcSpotCommand);
        return mexcCommand;
    }
}
