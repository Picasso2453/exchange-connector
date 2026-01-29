using System.Text.Json;
using xws.Core.Subscriptions;

namespace xws.Exchanges.Hyperliquid;

public static class HyperliquidWs
{
    public const string MainnetUrl = "wss://api.hyperliquid.xyz/ws";

    public static SubscriptionRequest BuildTradesSubscription(string symbol)
    {
        var payload = new
        {
            method = "subscribe",
            subscription = new
            {
                type = "trades",
                coin = symbol
            }
        };

        var key = new SubscriptionKey("trades", $"coin={symbol}");
        return new SubscriptionRequest(key, JsonSerializer.Serialize(payload));
    }
}
