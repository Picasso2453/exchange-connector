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
                    Logger.Error("--count must be greater than 0");
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
                    Logger.Error("dev emit timeout reached");
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
                Logger.Error($"dev emit failed: {ex.Message}");
                Environment.ExitCode = 2;
            }
        }, devCountOption, devTimeoutSecondsOption);

        devCommand.AddCommand(devEmitCommand);
        return devCommand;
    }
}
