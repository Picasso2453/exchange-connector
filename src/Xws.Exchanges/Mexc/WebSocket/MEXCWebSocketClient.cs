using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace Xws.Exchanges.Mexc.WebSocket;

public static class MEXCWebSocketClient
{
    public static async Task RunAsync(
        Uri wsUri,
        IEnumerable<string> subscribePayloads,
        TimeSpan pingInterval,
        Func<string, CancellationToken, Task> onTextFrame,
        CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(wsUri, cancellationToken);

        foreach (var payload in subscribePayloads)
        {
            var bytes = Encoding.UTF8.GetBytes(payload);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }

        using var pingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pingTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(pingInterval);
            while (await timer.WaitForNextTickAsync(pingCts.Token))
            {
                var pingPayload = Encoding.UTF8.GetBytes("{\"method\":\"ping\"}");
                await socket.SendAsync(pingPayload, WebSocketMessageType.Text, true, pingCts.Token);
            }
        }, pingCts.Token);

        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
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
                        return;
                    }

                    messageBuffer.Write(segment.Array!, segment.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    continue;
                }

                var text = messageBuffer.TryGetBuffer(out var bufferSegment)
                    ? Encoding.UTF8.GetString(bufferSegment.Array!, bufferSegment.Offset, (int)messageBuffer.Length)
                    : Encoding.UTF8.GetString(messageBuffer.ToArray());
                if (string.Equals(text, "pong", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                await onTextFrame(text, cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            pingCts.Cancel();
            await pingTask;
        }
    }
}
