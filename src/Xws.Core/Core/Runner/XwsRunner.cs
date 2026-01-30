using xws.Core.Mux;
using xws.Core.Output;
using xws.Core.Subscriptions;
using xws.Core.WebSocket;
using xws.Exchanges.Hyperliquid;
using xws.Exchanges.Mexc;

namespace xws.Core.Runner;

public sealed class XwsRunner
{
    public OutputChannel Output { get; } = new();

    public async Task<int> RunHlTradesAsync(string symbol, WebSocketRunnerOptions options, string format, CancellationToken cancellationToken)
    {
        try
        {
            var config = HyperliquidConfig.Load();
            var subscription = HyperliquidWs.BuildTradesSubscription(symbol);
            IJsonlWriter writer = format == "raw"
                ? new JsonlWriter(line => Output.Writer.TryWrite(line))
                : new EnvelopeWriter("hl", "trades", null, new[] { symbol }, line => Output.Writer.TryWrite(line));
            var registry = new SubscriptionRegistry();
            registry.Add(subscription);
            var runner = new WebSocketRunner(writer, registry);
            return await runner.RunAsync(config.WsUri, options, cancellationToken);
        }
        finally
        {
            Output.Complete();
        }
    }

    public async Task<int> RunHlPositionsAsync(string user, WebSocketRunnerOptions options, string format, CancellationToken cancellationToken)
    {
        try
        {
            var config = HyperliquidConfig.Load();
            var subscription = HyperliquidWs.BuildClearinghouseStateSubscription(user);
            IJsonlWriter writer = format == "raw"
                ? new JsonlWriter(line => Output.Writer.TryWrite(line))
                : new EnvelopeWriter("hl", "positions", null, null, line => Output.Writer.TryWrite(line));
            var registry = new SubscriptionRegistry();
            registry.Add(subscription);
            var runner = new WebSocketRunner(writer, registry);
            return await runner.RunAsync(config.WsUri, options, cancellationToken);
        }
        finally
        {
            Output.Complete();
        }
    }

    public async Task<int> RunMexcSpotTradesAsync(string[] symbols, int? maxMessages, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        try
        {
            var config = MexcConfig.Load();
            var subscriber = new MexcSpotTradeSubscriber();
            return await subscriber.RunAsync(config.SpotWsUri, symbols, maxMessages, timeout, cancellationToken, Output.Writer);
        }
        finally
        {
            Output.Complete();
        }
    }

    public async Task<int> RunMuxTradesAsync(IReadOnlyCollection<MuxSubscription> subscriptions, MuxRunnerOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var runner = new MuxRunner(new JsonlWriter(line => Output.Writer.TryWrite(line)));
            var producers = subscriptions.Select(sub => (Func<System.Threading.Channels.ChannelWriter<EnvelopeV1>, CancellationToken, Task>)(async (writer, token) =>
            {
                if (sub.Exchange.Equals("hl", StringComparison.OrdinalIgnoreCase))
                {
                    await HyperliquidMuxSource.RunTradesAsync(sub.Symbols, writer, token);
                    return;
                }

                if (sub.Exchange.Equals("mexc", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(sub.Market, "spot", StringComparison.OrdinalIgnoreCase))
                    {
                        await MexcSpotMuxSource.RunTradesAsync(sub.Symbols, writer, token);
                        return;
                    }

                    if (string.Equals(sub.Market, "fut", StringComparison.OrdinalIgnoreCase))
                    {
                        await MexcFuturesTradeSource.RunTradesAsync(sub.Symbols, writer, token);
                        return;
                    }
                }

                Logger.Error($"unsupported mux exchange: {sub.Exchange}");
            })).ToList();

            return await runner.RunAsync(producers, options, cancellationToken);
        }
        finally
        {
            Output.Complete();
        }
    }
}
