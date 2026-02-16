using Xws.Exchanges.Mexc;
using xws.tests.Fixtures;

namespace xws.tests;

public sealed class LoadTests
{
    [Fact]
    public void MexcTradeDecoder_HandlesHighFrequencyLoad()
    {
        var line = FixtureLoader.LoadLines("mexc-fut-trades.jsonl").First();
        var symbols = new[] { "BTC_USDT" };
        var success = 0;

        for (var i = 0; i < 1000; i++)
        {
            if (MexcFuturesTradeDecoder.TryBuildEnvelope(line, symbols, out _))
            {
                success++;
            }
        }

        Assert.Equal(1000, success);
    }
}
