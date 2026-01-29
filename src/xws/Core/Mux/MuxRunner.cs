using System.Text.Json;
using System.Threading.Channels;
using xws.Core.Output;

namespace xws.Core.Mux;

public sealed class MuxRunner
{
    private readonly IJsonlWriter _writer;

    public MuxRunner(IJsonlWriter writer)
    {
        _writer = writer;
    }

    public async Task<int> RunAsync(
        IEnumerable<Func<ChannelWriter<EnvelopeV1>, CancellationToken, Task>> producers,
        MuxRunnerOptions options,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = options.Timeout.HasValue
            ? new CancellationTokenSource(options.Timeout.Value)
            : null;
        using var linkedCts = timeoutCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;
        var runToken = linkedCts?.Token ?? cancellationToken;

        var channel = Channel.CreateUnbounded<EnvelopeV1>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var producerTasks = producers
            .Select(p => Task.Run(() => p(channel.Writer, runToken), runToken))
            .ToArray();

        _ = Task.WhenAll(producerTasks).ContinueWith(_ => channel.Writer.TryComplete(), TaskScheduler.Default);

        var emitted = 0;
        var timedOut = false;

        try
        {
            await foreach (var envelope in channel.Reader.ReadAllAsync(runToken))
            {
                var line = JsonSerializer.Serialize(envelope);
                _writer.WriteLine(line);
                emitted++;

                if (options.MaxMessages.HasValue && emitted >= options.MaxMessages.Value)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
        {
            timedOut = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return 0;
        }
        finally
        {
            try
            {
                if (!runToken.IsCancellationRequested)
                {
                    linkedCts?.Cancel();
                }
            }
            catch
            {
            }
        }

        try
        {
            await Task.WhenAll(producerTasks);
        }
        catch
        {
        }

        if (producerTasks.Any(t => t.IsFaulted))
        {
            Logger.Error("mux producer failed");
            return 1;
        }

        if (timedOut)
        {
            Logger.Error("mux timeout reached");
        }

        return 0;
    }
}
