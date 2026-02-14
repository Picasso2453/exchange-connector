using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using xws.Core.Output;

namespace xws.Exchanges.Bybit;

public static class BybitMuxSource
{
    public static Task RunTradesAsync(
        string[] symbols,
        string? market,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
        => RunAsync(symbols, market, "publicTrade", "trades", writer, cancellationToken);

    public static Task RunL2Async(
        string[] symbols,
        string? market,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
        => RunAsync(symbols, market, "orderbook.50", "l2", writer, cancellationToken);

    public static Task RunFundingAsync(
        string[] symbols,
        string? market,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
        => RunAsync(symbols, market, "fundingRate", "funding", writer, cancellationToken);

    public static Task RunLiquidationsAsync(
        string[] symbols,
        string? market,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
        => RunAsync(symbols, market, "liquidation", "liquidations", writer, cancellationToken);

    public static Task RunMarkPriceAsync(
        string[] symbols,
        string? market,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
        => RunAsync(symbols, market, "markPrice", "markprice", writer, cancellationToken);

    private static async Task RunAsync(
        string[] symbols,
        string? market,
        string channelPrefix,
        string stream,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        if (symbols.Length == 0)
        {
            throw new InvalidOperationException("at least one symbol is required");
        }

        var config = BybitConfig.Load();
        var wsUri = string.Equals(market, "spot", StringComparison.OrdinalIgnoreCase)
            ? config.SpotWsUri
            : config.FuturesWsUri;

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(wsUri, cancellationToken);

        var args = symbols.Select(symbol => $"{channelPrefix}.{symbol}").ToArray();
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
                    "bybit",
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
                    "bybit",
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
