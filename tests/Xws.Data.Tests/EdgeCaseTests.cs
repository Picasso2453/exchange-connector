using Xws.Exchanges.Mexc.WebSocket;
using Xws.Exchanges.Okx.WebSocket;

namespace Xws.Data.Tests;

public sealed class EdgeCaseTests
{
    [Fact]
    public void MexcParser_RejectsInvalidJson()
    {
        var ok = MEXCMessageParser.TryParseJson("{bad", out _);
        Assert.False(ok);
    }

    [Fact]
    public void OkxParser_FallsBackToTextOnInvalidJson()
    {
        var envelope = OKXMessageParser.BuildEnvelope("fut", "trades", new[] { "BTC-USDT-SWAP" }, "{bad");
        Assert.Equal("text", envelope.RawEncoding);
    }
}
