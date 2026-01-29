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

    public async Task<int> RunAsync(Uri uri, CancellationToken cancellationToken)
    {
        var reconnectAttempts = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var socket = new ClientWebSocket();
                Logger.Info($"connecting: {uri}");
                await socket.ConnectAsync(uri, cancellationToken);
                Logger.Info("connected");

                await SendAllSubscriptionsAsync(socket, cancellationToken);

                var outcome = await ReceiveLoopAsync(socket, cancellationToken);
                if (outcome)
                {
                    reconnectAttempts = 0;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"connection error: {ex.Message}");
            }

            if (cancellationToken.IsCancellationRequested)
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
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        return 0;
    }

    private async Task<bool> ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var receivedAny = false;

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
                    return receivedAny;
                }

                messageBuffer.Write(segment.Array!, segment.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var text = Encoding.UTF8.GetString(messageBuffer.ToArray());
                _writer.WriteLine(text);
                receivedAny = true;
            }
        }

        return receivedAny;
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

}
