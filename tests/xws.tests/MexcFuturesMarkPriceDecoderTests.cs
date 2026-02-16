using Xws.Exchanges.Mexc;
using xws.tests.Fixtures;

namespace xws.tests;

public sealed class MexcFuturesMarkPriceDecoderTests
{
    [Fact]
    public void Decode_MarkPriceFixtures_EmitsEnvelopeLines()
    {
        var lines = FixtureLoader.LoadLines("mexc-fut-markprice.jsonl");

        foreach (var line in lines)
        {
            Assert.True(MexcFuturesMarkPriceDecoder.TryBuildEnvelope(line, Array.Empty<string>(), out var envelope));
            var json = System.Text.Json.JsonSerializer.Serialize(envelope);
            EnvelopeAssertions.AssertEnvelopeJsonLine(json, "mexc", "markprice", "BTC_USDT");
        }
    }
}
