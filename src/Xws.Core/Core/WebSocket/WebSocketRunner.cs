using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using Xws.Abstractions;
using Xws.Data.Output;
using Xws.Data.Shared.Logging;
using Xws.Data.Subscriptions;

namespace Xws.Data.WebSocket;

public sealed class WebSocketRunner
{
    private const int MaxReconnectAttempts = 3;
    private static readonly TimeSpan BaseBackoff = TimeSpan.FromSeconds(1);
    private readonly IJsonlWriter _writer;
    private readonly SubscriptionRegistry _registry;

    public WebSocketRunner(IJsonlWriter writer, SubscriptionRegistry registry)
    {
        _writer = writer;
        _registry = registry;
    }

    public async Task<int> RunAsync(
        Uri uri,
        WebSocketRunnerOptions options,
        CancellationToken cancellationToken,
        Func<string, Task>? frameHandler = null)
    {
        var reconnectAttempts = 0;
        var state = new RunState();
        using var timeoutCts = options.Timeout.HasValue
            ? new CancellationTokenSource(options.Timeout.Value)
            : null;
        using var linkedCts = timeoutCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;
        var runToken = linkedCts?.Token ?? cancellationToken;

        while (!runToken.IsCancellationRequested)
        {
            try
            {
                using var socket = new ClientWebSocket();
                Logger.Info($"connecting: {uri}");
                await socket.ConnectAsync(uri, runToken);
                Logger.Info("connected");

                await SendAllSubscriptionsAsync(socket, runToken);

                var outcome = await ReceiveLoopAsync(socket, runToken, options, state, frameHandler);
                if (outcome.MaxMessagesReached)
                {
                    Logger.Info($"max messages reached: {state.MessageCount}");
                    return 0;
                }

                if (outcome.ReceivedAny)
                {
                    reconnectAttempts = 0;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
            {
                Logger.Error($"timeout reached before max messages: {state.MessageCount}");
                return 1;
            }
            catch (Exception ex)
            {
                Logger.Error($"connection error: {ex.Message}");
            }

            if (runToken.IsCancellationRequested)
            {
                break;
            }

            reconnectAttempts++;
            if (reconnectAttempts > MaxReconnectAttempts)
            {
                Logger.Error($"reconnect failed after {MaxReconnectAttempts} attempts");
                return 1;
            }

            var delay = TimeSpan.FromSeconds(BaseBackoff.TotalSeconds * Math.Pow(2, reconnectAttempts - 1));
            Logger.Info($"reconnect attempt {reconnectAttempts}/{MaxReconnectAttempts} in {delay.TotalSeconds:0}s");

            try
            {
                await Task.Delay(delay, runToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
            {
                Logger.Error($"timeout reached before max messages: {state.MessageCount}");
                return 1;
            }
        }

        return 0;
    }

    private async Task<ReceiveOutcome> ReceiveLoopAsync(
        ClientWebSocket socket,
        CancellationToken cancellationToken,
        WebSocketRunnerOptions options,
        RunState state,
        Func<string, Task>? frameHandler)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        var outcome = new ReceiveOutcome();
        var staleTimeout = options.StaleTimeout;
        using var pingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pingTask = StartPingLoopAsync(socket, options, pingCts.Token);

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var segment = new ArraySegment<byte>(buffer);
                using var messageBuffer = new MemoryStream();

                WebSocketReceiveResult result;
                using var staleCts = staleTimeout.HasValue
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    : null;
                if (staleCts is not null)
                {
                    staleCts.CancelAfter(staleTimeout.GetValueOrDefault());
                }

                try
                {
                    do
                    {
                        var token = staleCts?.Token ?? cancellationToken;
                        result = await socket.ReceiveAsync(segment, token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Logger.Info("remote closed connection");
                            return outcome;
                        }

                        messageBuffer.Write(segment.Array!, segment.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);
                }
                catch (OperationCanceledException) when (staleCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
                {
                    Logger.Error("connection appears stale, reconnecting");
                    return outcome;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text = messageBuffer.TryGetBuffer(out var bufferSegment)
                        ? Encoding.UTF8.GetString(bufferSegment.Array!, bufferSegment.Offset, (int)messageBuffer.Length)
                        : Encoding.UTF8.GetString(messageBuffer.ToArray());
                    try
                    {
                        if (frameHandler is not null)
                        {
                            await frameHandler(text);
                        }
                        else
                        {
                            _writer.WriteLine(text);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"frame handler error: {ex.Message}");
                    }
                    outcome.ReceivedAny = true;
                    state.MessageCount++;

                    if (options.MaxMessages.HasValue && state.MessageCount >= options.MaxMessages.Value)
                    {
                        outcome.MaxMessagesReached = true;
                        return outcome;
                    }
                }
            }

            return outcome;
        }
        finally
        {
            pingCts.Cancel();
            await pingTask;
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task SendAllSubscriptionsAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        foreach (var subscription in _registry.GetAll())
        {
            await SendTextAsync(socket, subscription.Json, cancellationToken);
            Logger.Info("subscribed");
        }
    }

    private static Task SendTextAsync(ClientWebSocket socket, string message, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(bytes);
        return socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
    }

    private static Task StartPingLoopAsync(ClientWebSocket socket, WebSocketRunnerOptions options, CancellationToken cancellationToken)
    {
        if (!options.PingInterval.HasValue || string.IsNullOrWhiteSpace(options.PingPayload))
        {
            return Task.CompletedTask;
        }

        var payload = Encoding.UTF8.GetBytes(options.PingPayload);
        return Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(options.PingInterval.Value);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
            }
        }, cancellationToken);
    }

    private sealed class ReceiveOutcome
    {
        public bool ReceivedAny { get; set; }
        public bool MaxMessagesReached { get; set; }
    }

    private sealed class RunState
    {
        public int MessageCount { get; set; }
    }
}

