using System.Threading.Channels;
using Xws.Data.Output;
using Xws.Exchanges.Bybit.WebSocket;

namespace Xws.Exchanges.Bybit;

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

        var payload = BybitSubscriptionBuilder.BuildSubscribePayload(channelPrefix, symbols);
        await BybitWebSocketClient.RunAsync(
            wsUri,
            payload,
            async (text, token) =>
            {
                var envelope = BybitMessageParser.BuildEnvelope(market, stream, symbols, text);
                await writer.WriteAsync(envelope, token);
            },
            cancellationToken);
    }
}
