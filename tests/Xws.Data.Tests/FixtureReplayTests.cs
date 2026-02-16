using Xws.Data.Tests.Fixtures;

namespace Xws.Data.Tests;

public sealed class FixtureReplayTests
{
    [Fact]
    public void FixtureReplay_LoadsAtLeastOneLine()
    {
        var lines = FixtureLoader.LoadLines("sample-envelope.jsonl");

        Assert.NotEmpty(lines);
        foreach (var line in lines)
        {
            EnvelopeAssertions.AssertEnvelopeJsonLine(line, "mexc", "trades", "BTCUSDT");
        }
    }
}
