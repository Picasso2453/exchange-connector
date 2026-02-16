using System.CommandLine;

using xws.Core.Mux;
using xws.Core.Runner;
using xws.Core.Shared.Logging;

namespace xws.Commands;

public static class MuxCommands
{
    public static Command Build()
    {
        var muxCommand = new Command("subscribe", "Subscribe to multiple exchanges");
        var muxSubOption = new Option<string[]>(
            "--sub",
            "Subscription spec: <exchange>[.<market>]=SYM1,SYM2 (repeatable)")
        {
            Arity = ArgumentArity.OneOrMore
        };
        var muxMaxMessagesOption = new Option<int?>("--max-messages", "Stop after N JSONL messages (exit 0)");
        var muxTimeoutSecondsOption = new Option<int?>("--timeout-seconds", "Fail if max messages not reached within T seconds");
        var muxFormatOption = new Option<string>("--format", () => "envelope", "Output format: envelope|raw");

        muxCommand.AddCommand(BuildMuxStreamCommand(
            "trades",
            "Mux trades subscriptions",
            muxSubOption,
            muxMaxMessagesOption,
            muxTimeoutSecondsOption,
            muxFormatOption,
            (runner, subs, options, token) => runner.RunMuxTradesAsync(subs, options, token)));

        muxCommand.AddCommand(BuildMuxStreamCommand(
            "l2",
            "Mux l2 orderbook subscriptions",
            muxSubOption,
            muxMaxMessagesOption,
            muxTimeoutSecondsOption,
            muxFormatOption,
            (runner, subs, options, token) => runner.RunMuxL2Async(subs, options, token)));

        muxCommand.AddCommand(BuildMuxStreamCommand(
            "funding",
            "Mux funding rate subscriptions",
            muxSubOption,
            muxMaxMessagesOption,
            muxTimeoutSecondsOption,
            muxFormatOption,
            (runner, subs, options, token) => runner.RunMuxFundingAsync(subs, options, token)));

        muxCommand.AddCommand(BuildMuxStreamCommand(
            "liquidations",
            "Mux liquidations subscriptions",
            muxSubOption,
            muxMaxMessagesOption,
            muxTimeoutSecondsOption,
            muxFormatOption,
            (runner, subs, options, token) => runner.RunMuxLiquidationsAsync(subs, options, token)));

        muxCommand.AddCommand(BuildMuxStreamCommand(
            "markprice",
            "Mux mark price subscriptions",
            muxSubOption,
            muxMaxMessagesOption,
            muxTimeoutSecondsOption,
            muxFormatOption,
            (runner, subs, options, token) => runner.RunMuxMarkPriceAsync(subs, options, token)));

        muxCommand.AddCommand(BuildMuxStreamCommand(
            "fills",
            "Mux user fills subscriptions",
            muxSubOption,
            muxMaxMessagesOption,
            muxTimeoutSecondsOption,
            muxFormatOption,
            (runner, subs, options, token) => runner.RunMuxFillsAsync(subs, options, token)));

        return muxCommand;
    }

    private static Command BuildMuxStreamCommand(
        string name,
        string description,
        Option<string[]> muxSubOption,
        Option<int?> muxMaxMessagesOption,
        Option<int?> muxTimeoutSecondsOption,
        Option<string> muxFormatOption,
        Func<XwsRunner, List<MuxSubscription>, MuxRunnerOptions, CancellationToken, Task<int>> run)
    {
        var command = new Command(name, description);
        command.AddOption(muxSubOption);
        command.AddOption(muxMaxMessagesOption);
        command.AddOption(muxTimeoutSecondsOption);
        command.AddOption(muxFormatOption);
        command.SetHandler(async (string[] subs, int? maxMessages, int? timeoutSeconds, string format) =>
        {
            try
            {
                using var cts = CommandHelpers.CreateShutdownCts();

                if (!CommandHelpers.ValidateFormat(format))
                {
                    Environment.ExitCode = 1;
                    return;
                }

                if (format.Equals("raw", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Error("Unsupported --format for mux. Mux outputs envelope JSONL only. Remove --format or set --format envelope.");
                    Environment.ExitCode = 1;
                    return;
                }

                if (!CommandHelpers.ValidateMaxMessagesTimeout(maxMessages, timeoutSeconds))
                {
                    Environment.ExitCode = 1;
                    return;
                }

                var parsed = new List<ParsedSub>();
                foreach (var sub in subs)
                {
                    if (!CommandHelpers.TryParseSub(sub, out var parsedSub, out var parseError))
                    {
                        Logger.Error($"Invalid --sub. {parseError}. Example: hl=SOL or okx.fut=BTC-USDT-SWAP.");
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
                var writerTask = CommandHelpers.WriteOutputAsync(runner.Output.Reader, cts.Token);
                try
                {
                    var muxSubs = parsed.Select(p => new MuxSubscription(p.Exchange, p.Market, p.Symbols)).ToList();
                    var exitCode = await run(runner, muxSubs, options, cts.Token);
                    Environment.ExitCode = exitCode;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Mux subscribe {name} failed. {ex.Message}. Check connectivity and retry.");
                    Environment.ExitCode = 2;
                }
                finally
                {
                    await writerTask;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Mux subscribe {name} failed. {ex.Message}. Check connectivity and retry.");
                Environment.ExitCode = 2;
            }
        }, muxSubOption, muxMaxMessagesOption, muxTimeoutSecondsOption, muxFormatOption);

        return command;
    }
}
