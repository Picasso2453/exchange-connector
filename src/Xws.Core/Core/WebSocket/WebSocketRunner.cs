using System.Net.WebSockets;
using System.Text;
using xws.Core.Output;
using xws.Core.Subscriptions;

namespace xws.Core.WebSocket;

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
        var buffer = new byte[8192];
        var outcome = new ReceiveOutcome();

        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            var segment = new ArraySegment<byte>(buffer);
            using var messageBuffer = new MemoryStream();

            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(segment, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Logger.Info("remote closed connection");
                    return outcome;
                }

                messageBuffer.Write(segment.Array!, segment.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var text = Encoding.UTF8.GetString(messageBuffer.ToArray());
                if (frameHandler is not null)
                {
                    await frameHandler(text);
                }
                else
                {
                    _writer.WriteLine(text);
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
