using System.Text.Json;
using xws.Core.Output;

namespace xws.Exchanges.Mexc;

public static class MexcFuturesMarkPriceDecoder
{
    public static bool TryBuildEnvelope(string json, string[] fallbackSymbols, out EnvelopeV1 envelope)
    {
        envelope = default!;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(json);
            if (payload.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!payload.TryGetProperty("channel", out var channel))
            {
                return false;
            }

            var channelValue = channel.GetString();
            if (!string.Equals(channelValue, "push.markPrice", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] symbols = fallbackSymbols;
            if (payload.TryGetProperty("symbol", out var symbolElement)
                && symbolElement.ValueKind == JsonValueKind.String)
            {
                var symbol = symbolElement.GetString();
                if (!string.IsNullOrWhiteSpace(symbol))
                {
                    symbols = new[] { symbol! };
                }
            }

            envelope = new EnvelopeV1(
                "xws.envelope.v1",
                "mexc",
                "fut",
                "markprice",
                symbols,
                DateTimeOffset.UtcNow.ToString("O"),
                payload,
                "json");

            return true;
        }
        catch
        {
            return false;
        }
    }
}
