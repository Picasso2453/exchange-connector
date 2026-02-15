using System.Text.Json;

namespace xws.Exchanges.Mexc.WebSocket;

public static class MEXCMessageParser
{
    public static bool TryParseJson(string text, out JsonElement payload)
    {
        payload = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            payload = JsonSerializer.Deserialize<JsonElement>(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
