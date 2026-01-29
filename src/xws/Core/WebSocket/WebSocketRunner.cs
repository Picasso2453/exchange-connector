using System.Net.WebSockets;
using System.Text;
using xws.Core.Output;

namespace xws.Core.WebSocket;

public sealed class WebSocketRunner
{
    private readonly IJsonlWriter _writer;

    public WebSocketRunner(IJsonlWriter writer)
    {
        _writer = writer;
    }

    public async Task<int> RunAsync(Uri uri, IReadOnlyList<string> subscriptions, CancellationToken cancellationToken)
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

            await ReceiveLoopAsync(socket, cancellationToken);
            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Error($"connection error: {ex.Message}");
            return 1;
        }
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
