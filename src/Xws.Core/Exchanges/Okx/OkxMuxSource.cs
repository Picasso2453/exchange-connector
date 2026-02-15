using System.Threading.Channels;
using xws.Core.Output;
using xws.Exchanges.Okx.WebSocket;

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
        var payload = OKXSubscriptionBuilder.BuildSubscribePayload(channel, symbols);
        await OKXWebSocketClient.RunAsync(
            config.WsUri,
            payload,
            async (text, token) =>
            {
                var envelope = OKXMessageParser.BuildEnvelope(market, stream, symbols, text);
                await writer.WriteAsync(envelope, token);
            },
            cancellationToken);
    }
}
