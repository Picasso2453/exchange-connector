using System.Text.Json;

namespace Xws.Exchanges.Bybit.WebSocket;

public static class BybitSubscriptionBuilder
{
    public static string BuildSubscribePayload(string channelPrefix, string[] symbols)
    {
        var args = symbols.Select(symbol => $"{channelPrefix}.{symbol}").ToArray();
        return JsonSerializer.Serialize(new { op = "subscribe", args });
    }
}
