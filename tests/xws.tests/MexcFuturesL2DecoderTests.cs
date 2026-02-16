using Xws.Exchanges.Mexc;
using xws.tests.Fixtures;

namespace xws.tests;

public sealed class MexcFuturesL2DecoderTests
{
    [Fact]
    public void Decode_L2Fixtures_EmitsEnvelopeLines()
    {
        var lines = FixtureLoader.LoadLines("mexc-fut-l2.jsonl");

        foreach (var line in lines)
        {
            Assert.True(MexcFuturesL2Decoder.TryBuildEnvelope(line, Array.Empty<string>(), out var envelope));
            var json = System.Text.Json.JsonSerializer.Serialize(envelope);
            EnvelopeAssertions.AssertEnvelopeJsonLine(json, "mexc", "l2", "BTC_USDT");
        }
    }
}
