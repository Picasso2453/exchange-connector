using System.Text.Json;

namespace Xws.Exchanges.Tests;

public static class EnvelopeAssertions
{
    public static void AssertEnvelopeJsonLine(
        string line,
        string? expectedExchange = null,
        string? expectedStream = null,
        string? expectedSymbol = null)
    {
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("type", out _));
        Assert.True(root.TryGetProperty("exchange", out var exchange));
        Assert.True(root.TryGetProperty("stream", out var stream));
        Assert.True(root.TryGetProperty("receivedAt", out _));
        Assert.True(root.TryGetProperty("raw", out _));
        Assert.True(root.TryGetProperty("rawEncoding", out _));

        if (!string.IsNullOrWhiteSpace(expectedExchange))
        {
            Assert.Equal(expectedExchange, exchange.GetString());
        }

        if (!string.IsNullOrWhiteSpace(expectedStream))
        {
            Assert.Equal(expectedStream, stream.GetString());
        }

        if (!string.IsNullOrWhiteSpace(expectedSymbol)
            && root.TryGetProperty("symbols", out var symbols)
            && symbols.ValueKind == JsonValueKind.Array)
        {
            Assert.Contains(
                expectedSymbol,
                symbols.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()));
        }
    }
}
