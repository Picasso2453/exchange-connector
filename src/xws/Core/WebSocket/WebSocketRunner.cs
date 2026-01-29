using System.Net.WebSockets;
using System.Text;
using xws.Core.Output;

namespace xws.Core.WebSocket;

public sealed class WebSocketRunner
{
    private const int MaxReconnectAttempts = 3;
    private static readonly TimeSpan BaseBackoff = TimeSpan.FromSeconds(1);
    private readonly IJsonlWriter _writer;

    public WebSocketRunner(IJsonlWriter writer)
    {
        _writer = writer;
    }

    public async Task<int> RunAsync(Uri uri, IReadOnlyList<string> subscriptions, CancellationToken cancellationToken)
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

                foreach (var subscription in subscriptions)
                {
                    await SendTextAsync(socket, subscription, cancellationToken);
                    Logger.Info("subscribed");
                }

                reconnectAttempts = 0;

                await ReceiveLoopAsync(socket, cancellationToken);
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

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

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
                    return;
                }

                messageBuffer.Write(segment.Array!, segment.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var text = Encoding.UTF8.GetString(messageBuffer.ToArray());
                _writer.WriteLine(text);
            }
        }
    }

    private static Task SendTextAsync(ClientWebSocket socket, string message, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(bytes);
        return socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
    }
}
