using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using xws.Core.Shared.Logging;

namespace xws.Exchanges.Okx.WebSocket;

public static class OKXWebSocketClient
{
    public const string PublicUrl = "wss://ws.okx.com:8443/ws/v5/public";
    private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(30);
    private const string PingPayload = "ping";

    public static async Task RunAsync(
        Uri wsUri,
        string payload,
        Func<string, CancellationToken, Task> onFrame,
        CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(wsUri, cancellationToken);

        var bytes = Encoding.UTF8.GetBytes(payload);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);

        using var pingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pingTask = StartPingLoopAsync(socket, pingCts.Token);
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

                try
                {
                    await onFrame(text, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.Error($"okx frame handler error: {ex.Message}");
                }
            }
        }
        finally
        {
            pingCts.Cancel();
            await pingTask;
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static Task StartPingLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(PingPayload);
        return Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(PingInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
            }
        }, cancellationToken);
    }
}
