using Xws.Exchanges.Tests.Fixtures;

namespace Xws.Exchanges.Tests;

public sealed class BybitFixtureTests
{
    [Theory]
    [InlineData("bybit-funding.jsonl", "funding")]
    [InlineData("bybit-liquidations.jsonl", "liquidations")]
    [InlineData("bybit-markprice.jsonl", "markprice")]
    public void Fixtures_EmitEnvelopeLines(string fixture, string stream)
    {
        var lines = FixtureLoader.LoadLines(fixture);

        foreach (var line in lines)
        {
            EnvelopeAssertions.AssertEnvelopeJsonLine(line, "bybit", stream, "BTCUSDT");
        }
    }
}
