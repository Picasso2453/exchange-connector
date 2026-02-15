using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Google.Protobuf;
using xws.Core.Output;
using xws.Core.Shared.Logging;

namespace xws.Exchanges.Mexc;

public sealed class MexcSpotTradeSubscriber
{
    private const int MaxSubscriptions = 30;
    private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(20);

    public async Task<int> RunAsync(
        Uri wsUri,
        string[] symbols,
        int? maxMessages,
        TimeSpan? timeout,
        CancellationToken cancellationToken,
        ChannelWriter<string> output)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (symbols.Length == 0)
        {
            throw new InvalidOperationException("at least one symbol is required");
        }

        if (symbols.Length > MaxSubscriptions)
        {
            throw new InvalidOperationException($"mexc spot supports max {MaxSubscriptions} subscriptions per connection");
        }

        using var timeoutCts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : null;
        using var linkedCts = timeoutCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;
        var runToken = linkedCts?.Token ?? cancellationToken;

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(wsUri, runToken);

        var channels = symbols.Select(symbol => $"spot@public.aggre.deals.v3.api.pb@100ms@{symbol}").ToArray();
        var payload = new
        {
            method = "SUBSCRIPTION",
            @params = channels
        };

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, runToken);

        var writer = new EnvelopeWriter(
            "mexc",
            "trades",
            "spot",
            symbols,
            line => output.TryWrite(line));
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        var messageCount = 0;

        using var pingCts = CancellationTokenSource.CreateLinkedTokenSource(runToken);
        var pingTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(PingInterval);
            while (await timer.WaitForNextTickAsync(pingCts.Token))
            {
                var pingBytes = Encoding.UTF8.GetBytes("{\"method\":\"PING\"}");
                await socket.SendAsync(pingBytes, WebSocketMessageType.Text, true, pingCts.Token);
            }
        }, pingCts.Token);

        try
        {
            while (!runToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var segment = new ArraySegment<byte>(buffer);
                using var messageBuffer = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(segment, runToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return 0;
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
                        writer.WriteRawJson(jsonElement.ToString());
                        messageCount++;
                    }
                    else
                    {
                        Logger.Error("mexc spot protobuf decode failed");
                    }

                    if (maxMessages.HasValue && messageCount >= maxMessages.Value)
                    {
                        pingCts.Cancel();
                        await pingTask;
                        return 0;
                    }
                }
            }

            if (timeoutCts?.IsCancellationRequested == true)
            {
                Logger.Error("mexc spot timeout reached");
                return 1;
            }

            return 0;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            pingCts.Cancel();
            await pingTask;
        }
    }
}

