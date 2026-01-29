using System.Text.Json;

namespace xws.Exchanges.Hyperliquid;

public static class HyperliquidWs
{
    public const string MainnetUrl = "wss://api.hyperliquid.xyz/ws";

    public static string BuildTradesSubscription(string symbol)
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

        return JsonSerializer.Serialize(payload);
    }
}
