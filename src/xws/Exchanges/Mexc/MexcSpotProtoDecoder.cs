using System.Text.Json;
using Google.Protobuf;

namespace xws.Exchanges.Mexc;

public static class MexcSpotProtoDecoder
{
    public static bool TryDecodeToJsonElement(byte[] payload, out JsonElement jsonElement)
    {
        try
        {
            var wrapper = PushDataV3ApiWrapper.Parser.ParseFrom(payload);
            var json = JsonFormatter.Default.Format(wrapper);
            jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
            return true;
        }
        catch
        {
            jsonElement = default;
            return false;
        }
    }
}
