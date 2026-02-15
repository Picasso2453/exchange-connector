using System.CommandLine;
using xws.Core.Output;
using xws.Core.Runner;
using xws.Core.Shared.Logging;
using xws.Exchanges.Mexc;

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
                    Logger.Error($"mexc spot subscribe trades failed: {ex.Message}");
                    Environment.ExitCode = 2;
                }
                finally
                {
                    await writerTask;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"mexc spot subscribe trades failed: {ex.Message}");
                Environment.ExitCode = 2;
            }
        }, mexcSymbolOption, muxMaxMessagesOption, muxTimeoutSecondsOption);

        mexcSubscribeCommand.AddCommand(mexcTradesCommand);
        mexcSpotCommand.AddCommand(mexcSubscribeCommand);
        mexcCommand.AddCommand(mexcSpotCommand);
        return mexcCommand;
    }
}
