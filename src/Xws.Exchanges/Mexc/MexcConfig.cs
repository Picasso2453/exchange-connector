using Xws.Data.Env;

namespace Xws.Exchanges.Mexc;

public sealed class MexcConfig
{
    public Uri SpotWsUri { get; }
    public Uri FuturesWsUri { get; }

    private MexcConfig(Uri spotWsUri, Uri futuresWsUri)
    {
        SpotWsUri = spotWsUri;
        FuturesWsUri = futuresWsUri;
    }

    public static MexcConfig Load()
    {
        var spotOverride = EnvReader.GetOptional("XWS_MEXC_SPOT_WS_URL");
        var futOverride = EnvReader.GetOptional("XWS_MEXC_FUT_WS_URL");

        var spotUri = spotOverride is not null
            ? new Uri(spotOverride)
            : new Uri("wss://wbs-api.mexc.com/ws");

        var futUri = futOverride is not null
            ? new Uri(futOverride)
            : new Uri("wss://contract.mexc.com/edge");

        return new MexcConfig(spotUri, futUri);
    }
}
