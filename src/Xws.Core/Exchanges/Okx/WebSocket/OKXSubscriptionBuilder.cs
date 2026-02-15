using System.Text.Json;

namespace xws.Exchanges.Okx.WebSocket;

public static class OKXSubscriptionBuilder
{
    public static string BuildSubscribePayload(string channel, string[] symbols)
    {
        var args = symbols.Select(symbol => new { channel, instId = symbol }).ToArray();
        return JsonSerializer.Serialize(new { op = "subscribe", args });
    }
}
