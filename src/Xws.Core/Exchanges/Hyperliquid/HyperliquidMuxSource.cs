using System.Threading.Channels;
using xws.Core.Output;
using xws.Core.Subscriptions;
using xws.Core.WebSocket;
using xws.Exchanges.Hyperliquid.WebSocket;

namespace xws.Exchanges.Hyperliquid;

public static class HyperliquidMuxSource
{
    public static async Task RunFundingAsync(
        string[] symbols,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        await RunStreamAsync(
            symbols,
            writer,
            cancellationToken,
            "funding",
            HLSubscriptionBuilder.BuildFundingSubscription);
    }

    public static async Task RunLiquidationsAsync(
        string[] symbols,
        string user,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        var config = HyperliquidConfig.Load();
        var registry = new SubscriptionRegistry();
        registry.Add(HLSubscriptionBuilder.BuildLiquidationsSubscription(user));

        var runner = new WebSocketRunner(new JsonlWriter(_ => { }), registry);
        var options = new WebSocketRunnerOptions
        {
            MaxMessages = null,
            Timeout = null
        };

        await runner.RunAsync(
            config.WsUri,
            options,
            cancellationToken,
            async frame =>
            {
                EnvelopeV1 envelope;
                try
                {
                    envelope = HLMessageParser.BuildEnvelope("liquidations", symbols, frame);
                }
                catch
                {
                    envelope = HLMessageParser.BuildEnvelope("liquidations", symbols, frame);
                }

                await writer.WriteAsync(envelope, cancellationToken);
            });
    }

    public static async Task RunMarkPriceAsync(
        string[] symbols,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        await RunStreamAsync(
            symbols,
            writer,
            cancellationToken,
            "markprice",
            HLSubscriptionBuilder.BuildMarkPriceSubscription);
    }

    public static async Task RunTradesAsync(
        string[] symbols,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        await RunStreamAsync(
            symbols,
            writer,
            cancellationToken,
            "trades",
            HLSubscriptionBuilder.BuildTradesSubscription);
    }

    public static async Task RunL2Async(
        string[] symbols,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        await RunStreamAsync(
            symbols,
            writer,
            cancellationToken,
            "l2",
            HLSubscriptionBuilder.BuildL2BookSubscription);
    }

    public static async Task RunFillsAsync(
        string[] symbols,
        string user,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        var config = HyperliquidConfig.Load();
        var registry = new SubscriptionRegistry();
        registry.Add(HLSubscriptionBuilder.BuildUserFillsSubscription(user));

        var runner = new WebSocketRunner(new JsonlWriter(_ => { }), registry);
        var options = new WebSocketRunnerOptions
        {
            MaxMessages = null,
            Timeout = null
        };

        await runner.RunAsync(
            config.WsUri,
            options,
            cancellationToken,
            async frame =>
            {
                EnvelopeV1 envelope;
                try
                {
                    envelope = HLMessageParser.BuildEnvelope("fills", symbols, frame);
                }
                catch
                {
                    envelope = HLMessageParser.BuildEnvelope("fills", symbols, frame);
                }

                await writer.WriteAsync(envelope, cancellationToken);
            });
    }

    private static async Task RunStreamAsync(
        string[] symbols,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken,
        string envelopeType,
        Func<string, SubscriptionRequest> subscriptionFactory)
    {
        var config = HyperliquidConfig.Load();
        var registry = new SubscriptionRegistry();
        foreach (var symbol in symbols)
        {
            registry.Add(subscriptionFactory(symbol));
        }

        var runner = new WebSocketRunner(new JsonlWriter(_ => { }), registry);
        var options = new WebSocketRunnerOptions
        {
            MaxMessages = null,
            Timeout = null
        };

        await runner.RunAsync(
            config.WsUri,
            options,
            cancellationToken,
            async frame =>
            {
                EnvelopeV1 envelope;
                try
                {
                    envelope = HLMessageParser.BuildEnvelope(envelopeType, symbols, frame);
                }
                catch
                {
                    envelope = HLMessageParser.BuildEnvelope(envelopeType, symbols, frame);
                }

                await writer.WriteAsync(envelope, cancellationToken);
            });
    }
}
