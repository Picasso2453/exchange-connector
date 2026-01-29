using xws.Exchanges.Mexc;

namespace xws.tests;

public sealed class MexcSpotProtoDecoderTests
{
    [Fact]
    public void Decode_KnownWrapperPayload_ReturnsJson()
    {
        const string fixtureBase64 =
            "Ci9zcG90QHB1YmxpYy5hZ2dyZS5kZWFscy52My5hcGkucGJAMTAwbXNAQlRDVVNEVA==";
        var payload = Convert.FromBase64String(fixtureBase64);

        Assert.True(MexcSpotProtoDecoder.TryDecodeToJsonElement(payload, out var json));
        var channel = json.GetProperty("channel").GetString();
        Assert.Equal("spot@public.aggre.deals.v3.api.pb@100ms@BTCUSDT", channel);
    }
}
