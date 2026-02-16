using Xws.Exchanges.Tests.Fixtures;

namespace Xws.Exchanges.Tests;

public sealed class HyperliquidFixtureTests
{
    [Theory]
    [InlineData("hl-funding.jsonl", "funding")]
    [InlineData("hl-liquidations.jsonl", "liquidations")]
    [InlineData("hl-markprice.jsonl", "markprice")]
    [InlineData("hl-fills.jsonl", "fills")]
    public void Fixtures_EmitEnvelopeLines(string fixture, string stream)
    {
        var lines = FixtureLoader.LoadLines(fixture);

        foreach (var line in lines)
        {
            EnvelopeAssertions.AssertEnvelopeJsonLine(line, "hl", stream, "SOL");
        }
    }
}
