using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using xws.Core.Output;

namespace xws.Exchanges.Okx;

public static class OkxMuxSource
{
    public static Task RunTradesAsync(
        string[] symbols,
        string? market,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
        => RunAsync(symbols, market, "trades", "trades", writer, cancellationToken);

    public static Task RunL2Async(
        string[] symbols,
        string? market,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
        => RunAsync(symbols, market, "books5", "l2", writer, cancellationToken);

    public static Task RunFundingAsync(
        string[] symbols,
        string? market,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
        => RunAsync(symbols, market, "funding-rate", "funding", writer, cancellationToken);

    public static Task RunLiquidationsAsync(
        string[] symbols,
        string? market,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
        => RunAsync(symbols, market, "liquidation-orders", "liquidations", writer, cancellationToken);

    public static Task RunMarkPriceAsync(
        string[] symbols,
        string? market,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
        => RunAsync(symbols, market, "mark-price", "markprice", writer, cancellationToken);

    private static async Task RunAsync(
        string[] symbols,
        string? market,
        string channel,
        string stream,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        if (symbols.Length == 0)
        {
            throw new InvalidOperationException("at least one symbol is required");
        }

        var config = OkxConfig.Load();
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(config.WsUri, cancellationToken);

        var args = symbols.Select(symbol => new { channel, instId = symbol }).ToArray();
        var payload = JsonSerializer.Serialize(new { op = "subscribe", args });
        var bytes = Encoding.UTF8.GetBytes(payload);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);

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
                    return;
                }

                messageBuffer.Write(segment.Array!, segment.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            var text = Encoding.UTF8.GetString(messageBuffer.ToArray());
            EnvelopeV1 envelope;
            try
            {
                var payloadJson = JsonSerializer.Deserialize<JsonElement>(text);
                envelope = new EnvelopeV1(
                    "xws.envelope.v1",
                    "okx",
                    market,
                    stream,
                    symbols,
                    DateTimeOffset.UtcNow.ToString("O"),
                    payloadJson,
                    "json");
            }
            catch
            {
                envelope = new EnvelopeV1(
                    "xws.envelope.v1",
                    "okx",
                    market,
                    stream,
                    symbols,
                    DateTimeOffset.UtcNow.ToString("O"),
                    text,
                    "text");
            }

            await writer.WriteAsync(envelope, cancellationToken);
        }
    }
}
