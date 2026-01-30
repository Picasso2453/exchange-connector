using xws.tests.Fixtures;

namespace xws.tests;

public sealed class FixtureReplayTests
{
    [Fact]
    public void FixtureReplay_LoadsAtLeastOneLine()
    {
        var lines = FixtureLoader.LoadLines("sample-envelope.jsonl");

        Assert.NotEmpty(lines);
    }
}
