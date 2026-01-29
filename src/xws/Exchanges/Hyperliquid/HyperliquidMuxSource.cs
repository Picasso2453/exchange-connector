using System.Text.Json;
using System.Threading.Channels;
using xws.Core.Output;
using xws.Core.Subscriptions;
using xws.Core.WebSocket;

namespace xws.Exchanges.Hyperliquid;

public static class HyperliquidMuxSource
{
    public static async Task RunTradesAsync(
        string[] symbols,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        var config = HyperliquidConfig.Load();
        var registry = new SubscriptionRegistry();
        foreach (var symbol in symbols)
        {
            registry.Add(HyperliquidWs.BuildTradesSubscription(symbol));
        }

        var runner = new WebSocketRunner(new JsonlWriter(), registry);
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
                var rawEncoding = "json";
                try
                {
                    var payload = JsonSerializer.Deserialize<JsonElement>(frame);
                    envelope = new EnvelopeV1(
                        "xws.envelope.v1",
                        "hl",
                        null,
                        "trades",
                        symbols,
                        DateTimeOffset.UtcNow.ToString("O"),
                        payload,
                        rawEncoding);
                }
                catch
                {
                    envelope = new EnvelopeV1(
                        "xws.envelope.v1",
                        "hl",
                        null,
                        "trades",
                        symbols,
                        DateTimeOffset.UtcNow.ToString("O"),
                        frame,
                        "text");
                }

                await writer.WriteAsync(envelope, cancellationToken);
            });
    }
}
