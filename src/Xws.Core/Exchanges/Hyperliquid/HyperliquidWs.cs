using System.Text.Json;
using xws.Core.Subscriptions;

namespace xws.Exchanges.Hyperliquid;

public static class HyperliquidWs
{
    public const string MainnetUrl = "wss://api.hyperliquid.xyz/ws";
    public const string TestnetUrl = "wss://api.hyperliquid-testnet.xyz/ws";

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

    public static SubscriptionRequest BuildL2BookSubscription(string symbol)
    {
        var payload = new
        {
            method = "subscribe",
            subscription = new
            {
                type = "l2Book",
                coin = symbol
            }
        };

        var key = new SubscriptionKey("l2Book", $"coin={symbol}");
        return new SubscriptionRequest(key, JsonSerializer.Serialize(payload));
    }

    public static SubscriptionRequest BuildCandleSubscription(string symbol, string interval)
    {
        var payload = new
        {
            method = "subscribe",
            subscription = new
            {
                type = "candle",
                coin = symbol,
                interval
            }
        };

        var key = new SubscriptionKey("candle", $"coin={symbol};interval={interval}");
        return new SubscriptionRequest(key, JsonSerializer.Serialize(payload));
    }

    public static SubscriptionRequest BuildClearinghouseStateSubscription(string user)
    {
        var payload = new
        {
            method = "subscribe",
            subscription = new
            {
                type = "clearinghouseState",
                user
            }
        };

        var key = new SubscriptionKey("clearinghouseState", $"user={user}");
        return new SubscriptionRequest(key, JsonSerializer.Serialize(payload));
    }

    public static SubscriptionRequest BuildFundingSubscription(string symbol)
    {
        var payload = new
        {
            method = "subscribe",
            subscription = new
            {
                type = "activeAssetCtx",
                coin = symbol
            }
        };

        var key = new SubscriptionKey("activeAssetCtx", $"coin={symbol}");
        return new SubscriptionRequest(key, JsonSerializer.Serialize(payload));
    }

    public static SubscriptionRequest BuildLiquidationsSubscription(string user)
    {
        var payload = new
        {
            method = "subscribe",
            subscription = new
            {
                type = "userEvents",
                user
            }
        };

        var key = new SubscriptionKey("userEvents", $"user={user}");
        return new SubscriptionRequest(key, JsonSerializer.Serialize(payload));
    }

    public static SubscriptionRequest BuildMarkPriceSubscription(string symbol)
    {
        var payload = new
        {
            method = "subscribe",
            subscription = new
            {
                type = "activeAssetCtx",
                coin = symbol
            }
        };

        var key = new SubscriptionKey("activeAssetCtx", $"coin={symbol}");
        return new SubscriptionRequest(key, JsonSerializer.Serialize(payload));
    }

    public static SubscriptionRequest BuildUserFillsSubscription(string user)
    {
        var payload = new
        {
            method = "subscribe",
            subscription = new
            {
                type = "userFills",
                user
            }
        };

        var key = new SubscriptionKey("userFills", $"user={user}");
        return new SubscriptionRequest(key, JsonSerializer.Serialize(payload));
    }
}
