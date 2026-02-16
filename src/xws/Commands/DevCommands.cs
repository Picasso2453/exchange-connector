using System.CommandLine;

using xws.Core.Dev;
using xws.Core.Output;
using xws.Core.Shared.Logging;

namespace xws.Commands;

public static class DevCommands
{
    public static Command Build()
    {
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
            try
            {
                if (count <= 0)
                {
                    Logger.Error("Invalid --count. Value must be greater than 0. Provide a positive integer.");
                    Environment.ExitCode = 1;
                    return;
                }

                if (timeoutSeconds.HasValue && timeoutSeconds.Value <= 0)
                {
                    Logger.Error("Invalid --timeout-seconds. Value must be greater than 0. Provide a positive integer.");
                    Environment.ExitCode = 1;
                    return;
                }

                using var cts = timeoutSeconds.HasValue
                    ? new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds.Value))
                    : new CancellationTokenSource();

                var output = new OutputChannel();
                var writerTask = CommandHelpers.WriteOutputAsync(output.Reader, cts.Token);
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
                    Logger.Error("Dev emit timed out. The timeout expired before all lines were emitted. Increase --timeout-seconds or reduce --count.");
                    Environment.ExitCode = 1;
                }
                finally
                {
                    output.Complete();
                    await writerTask;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Dev emit failed. {ex.Message}. Retry or check input parameters.");
                Environment.ExitCode = 2;
            }
        }, devCountOption, devTimeoutSecondsOption);

        devCommand.AddCommand(devEmitCommand);
        return devCommand;
    }
}
