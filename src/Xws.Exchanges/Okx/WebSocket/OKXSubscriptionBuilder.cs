using System.Text.Json;

namespace Xws.Exchanges.Okx.WebSocket;

public static class OKXSubscriptionBuilder
{
    public static string BuildSubscribePayload(string channel, string[] symbols)
    {
        var args = symbols.Select(symbol => new { channel, instId = symbol }).ToArray();
        return JsonSerializer.Serialize(new { op = "subscribe", args });
    }
}
