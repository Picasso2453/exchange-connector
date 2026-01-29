using xws.Core.Mux;
using xws.Core.Output;
using xws.Core.Subscriptions;
using xws.Core.WebSocket;
using xws.Exchanges.Hyperliquid;
using xws.Exchanges.Mexc;

namespace xws.Core.Runner;

public sealed class XwsRunner
{
    public async Task<int> RunHlTradesAsync(string symbol, WebSocketRunnerOptions options, string format, CancellationToken cancellationToken)
    {
        var config = HyperliquidConfig.Load();
        var subscription = HyperliquidWs.BuildTradesSubscription(symbol);
        IJsonlWriter writer = format == "raw"
            ? new JsonlWriter()
            : new EnvelopeWriter("hl", "trades", null, new[] { symbol });
        var registry = new SubscriptionRegistry();
        registry.Add(subscription);
        var runner = new WebSocketRunner(writer, registry);
        return await runner.RunAsync(config.WsUri, options, cancellationToken);
    }

    public async Task<int> RunHlPositionsAsync(string user, WebSocketRunnerOptions options, string format, CancellationToken cancellationToken)
    {
        var config = HyperliquidConfig.Load();
        var subscription = HyperliquidWs.BuildClearinghouseStateSubscription(user);
        IJsonlWriter writer = format == "raw"
            ? new JsonlWriter()
            : new EnvelopeWriter("hl", "positions", null, null);
        var registry = new SubscriptionRegistry();
        registry.Add(subscription);
        var runner = new WebSocketRunner(writer, registry);
        return await runner.RunAsync(config.WsUri, options, cancellationToken);
    }

    public async Task<int> RunMexcSpotTradesAsync(string[] symbols, int? maxMessages, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        var config = MexcConfig.Load();
        var subscriber = new MexcSpotTradeSubscriber();
        return await subscriber.RunAsync(config.SpotWsUri, symbols, maxMessages, timeout, cancellationToken);
    }

    public async Task<int> RunMuxTradesAsync(IReadOnlyCollection<MuxSubscription> subscriptions, MuxRunnerOptions options, CancellationToken cancellationToken)
    {
        var runner = new MuxRunner(new JsonlWriter());
        var producers = subscriptions.Select(sub => (Func<System.Threading.Channels.ChannelWriter<EnvelopeV1>, CancellationToken, Task>)(async (writer, token) =>
        {
            if (sub.Exchange.Equals("hl", StringComparison.OrdinalIgnoreCase))
            {
                await HyperliquidMuxSource.RunTradesAsync(sub.Symbols, writer, token);
                return;
            }

            if (sub.Exchange.Equals("mexc", StringComparison.OrdinalIgnoreCase)
                && string.Equals(sub.Market, "spot", StringComparison.OrdinalIgnoreCase))
            {
                await MexcSpotMuxSource.RunTradesAsync(sub.Symbols, writer, token);
                return;
            }

            Logger.Error($"unsupported mux exchange: {sub.Exchange}");
        })).ToList();

        return await runner.RunAsync(producers, options, cancellationToken);
    }
}
