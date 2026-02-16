using Xws.Exchanges.Mexc;
using xws.tests.Fixtures;

namespace xws.tests;

public sealed class MexcFuturesFundingDecoderTests
{
    [Fact]
    public void Decode_FundingFixtures_EmitsEnvelopeLines()
    {
        var lines = FixtureLoader.LoadLines("mexc-fut-funding.jsonl");

        foreach (var line in lines)
        {
            Assert.True(MexcFuturesFundingDecoder.TryBuildEnvelope(line, Array.Empty<string>(), out var envelope));
            var json = System.Text.Json.JsonSerializer.Serialize(envelope);
            EnvelopeAssertions.AssertEnvelopeJsonLine(json, "mexc", "funding", "BTC_USDT");
        }
    }
}
