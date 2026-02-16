using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Xws.Data.Output;
using Xws.Data.Shared.Logging;

namespace Xws.Exchanges.Mexc;

public static class MexcSpotMuxSource
{
    private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(20);

    public static async Task RunTradesAsync(
        string[] symbols,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        var config = MexcConfig.Load();
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(config.SpotWsUri, cancellationToken);

        var channels = symbols.Select(symbol => $"spot@public.aggre.deals.v3.api.pb@100ms@{symbol}").ToArray();
        var payload = new
        {
            method = "SUBSCRIPTION",
            @params = channels
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);

        using var pingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pingTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(PingInterval);
            while (await timer.WaitForNextTickAsync(pingCts.Token))
            {
                var pingBytes = Encoding.UTF8.GetBytes("{\"method\":\"PING\"}");
                await socket.SendAsync(pingBytes, WebSocketMessageType.Text, true, pingCts.Token);
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

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text = messageBuffer.TryGetBuffer(out var bufferSegment)
                        ? Encoding.UTF8.GetString(bufferSegment.Array!, bufferSegment.Offset, (int)messageBuffer.Length)
                        : Encoding.UTF8.GetString(messageBuffer.ToArray());
                    if (!string.Equals(text, "PONG", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Info($"mexc spot text frame: {text}");
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var payloadBytes = messageBuffer.ToArray();
                    if (MexcSpotProtoDecoder.TryDecodeToJsonElement(payloadBytes, out var jsonElement))
                    {
                        var envelope = new EnvelopeV1(
                            "xws.envelope.v1",
                            "mexc",
                            "spot",
                            "trades",
                            symbols,
                            DateTimeOffset.UtcNow.ToString("O"),
                            jsonElement,
                            "json");
                        await writer.WriteAsync(envelope, cancellationToken);
                    }
                    else
                    {
                        Logger.Error("mexc spot protobuf decode failed");
                    }
                }
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

