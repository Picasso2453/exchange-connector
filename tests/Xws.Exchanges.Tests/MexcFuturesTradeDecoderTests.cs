using Xws.Exchanges.Mexc;
using Xws.Exchanges.Tests.Fixtures;

namespace Xws.Exchanges.Tests;

public sealed class MexcFuturesTradeDecoderTests
{
    [Fact]
    public void Decode_TradeFixtures_EmitsEnvelopeLines()
    {
        var lines = FixtureLoader.LoadLines("mexc-fut-trades.jsonl");

        foreach (var line in lines)
        {
            Assert.True(MexcFuturesTradeDecoder.TryBuildEnvelope(line, Array.Empty<string>(), out var envelope));
            var json = System.Text.Json.JsonSerializer.Serialize(envelope);
            EnvelopeAssertions.AssertEnvelopeJsonLine(json, "mexc", "trades", "BTC_USDT");
        }
    }
}
