using System.Text.Json;
using xws.Core.Output;

namespace xws.Exchanges.Okx.WebSocket;

public static class OKXMessageParser
{
    public static EnvelopeV1 BuildEnvelope(string? market, string stream, string[] symbols, string text)
    {
        try
        {
            var payloadJson = JsonSerializer.Deserialize<JsonElement>(text);
            return new EnvelopeV1(
                "xws.envelope.v1",
                "okx",
                market,
                stream,
                symbols,
                DateTimeOffset.UtcNow.ToString("O"),
                payloadJson,
                "json");
        }
        catch
        {
            return new EnvelopeV1(
                "xws.envelope.v1",
                "okx",
                market,
                stream,
                symbols,
                DateTimeOffset.UtcNow.ToString("O"),
                text,
                "text");
        }
    }
}
