using Xws.Exchanges.Tests.Fixtures;

namespace Xws.Exchanges.Tests;

public sealed class OkxFixtureTests
{
    [Theory]
    [InlineData("okx-trades.jsonl", "trades")]
    [InlineData("okx-l2.jsonl", "l2")]
    [InlineData("okx-funding.jsonl", "funding")]
    [InlineData("okx-liquidations.jsonl", "liquidations")]
    [InlineData("okx-markprice.jsonl", "markprice")]
    public void Fixtures_EmitEnvelopeLines(string fixture, string stream)
    {
        var lines = FixtureLoader.LoadLines(fixture);

        foreach (var line in lines)
        {
            EnvelopeAssertions.AssertEnvelopeJsonLine(line, "okx", stream, "BTC-USDT-SWAP");
        }
    }
}
