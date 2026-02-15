using System.Text.Json;
using xws.Core.Output;

namespace xws.Exchanges.Hyperliquid.WebSocket;

public static class HLMessageParser
{
    public static EnvelopeV1 BuildEnvelope(string stream, string[] symbols, string frame)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(frame);
            return new EnvelopeV1(
                "xws.envelope.v1",
                "hl",
                null,
                stream,
                symbols,
                DateTimeOffset.UtcNow.ToString("O"),
                payload,
                "json");
        }
        catch
        {
            return new EnvelopeV1(
                "xws.envelope.v1",
                "hl",
                null,
                stream,
                symbols,
                DateTimeOffset.UtcNow.ToString("O"),
                frame,
                "text");
        }
    }
}
