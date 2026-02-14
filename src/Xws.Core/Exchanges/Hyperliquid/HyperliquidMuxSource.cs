using System.Text.Json;
using System.Threading.Channels;
using xws.Core.Output;
using xws.Core.Subscriptions;
using xws.Core.WebSocket;

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
            HyperliquidWs.BuildFundingSubscription);
    }

    public static async Task RunLiquidationsAsync(
        string[] symbols,
        string user,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        var config = HyperliquidConfig.Load();
        var registry = new SubscriptionRegistry();
        registry.Add(HyperliquidWs.BuildLiquidationsSubscription(user));

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
                    var payload = JsonSerializer.Deserialize<JsonElement>(frame);
                    envelope = new EnvelopeV1(
                        "xws.envelope.v1",
                        "hl",
                        null,
                        "liquidations",
                        symbols,
                        DateTimeOffset.UtcNow.ToString("O"),
                        payload,
                        "json");
                }
                catch
                {
                    envelope = new EnvelopeV1(
                        "xws.envelope.v1",
                        "hl",
                        null,
                        "liquidations",
                        symbols,
                        DateTimeOffset.UtcNow.ToString("O"),
                        frame,
                        "text");
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
            HyperliquidWs.BuildMarkPriceSubscription);
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
            HyperliquidWs.BuildTradesSubscription);
    }

    public static async Task RunFillsAsync(
        string[] symbols,
        string user,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        var config = HyperliquidConfig.Load();
        var registry = new SubscriptionRegistry();
        registry.Add(HyperliquidWs.BuildUserFillsSubscription(user));

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
                    var payload = JsonSerializer.Deserialize<JsonElement>(frame);
                    envelope = new EnvelopeV1(
                        "xws.envelope.v1",
                        "hl",
                        null,
                        "fills",
                        symbols,
                        DateTimeOffset.UtcNow.ToString("O"),
                        payload,
                        "json");
                }
                catch
                {
                    envelope = new EnvelopeV1(
                        "xws.envelope.v1",
                        "hl",
                        null,
                        "fills",
                        symbols,
                        DateTimeOffset.UtcNow.ToString("O"),
                        frame,
                        "text");
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
                    var payload = JsonSerializer.Deserialize<JsonElement>(frame);
                    envelope = new EnvelopeV1(
                        "xws.envelope.v1",
                        "hl",
                        null,
                        envelopeType,
                        symbols,
                        DateTimeOffset.UtcNow.ToString("O"),
                        payload,
                        "json");
                }
                catch
                {
                    envelope = new EnvelopeV1(
                        "xws.envelope.v1",
                        "hl",
                        null,
                        envelopeType,
                        symbols,
                        DateTimeOffset.UtcNow.ToString("O"),
                        frame,
                        "text");
                }

                await writer.WriteAsync(envelope, cancellationToken);
            });
    }
}
