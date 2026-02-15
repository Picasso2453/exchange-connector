using xws.Core.Env;
using xws.Exchanges.Bybit.WebSocket;

namespace xws.Exchanges.Bybit;

public sealed class BybitConfig
{
    public Uri SpotWsUri { get; }
    public Uri FuturesWsUri { get; }

    private BybitConfig(Uri spotWsUri, Uri futuresWsUri)
    {
        SpotWsUri = spotWsUri;
        FuturesWsUri = futuresWsUri;
    }

    public static BybitConfig Load()
    {
        var spotOverride = EnvReader.GetOptional("XWS_BYBIT_SPOT_WS_URL");
        var futOverride = EnvReader.GetOptional("XWS_BYBIT_FUT_WS_URL");

        var spotUri = spotOverride is not null
            ? new Uri(spotOverride)
            : new Uri(BybitWebSocketClient.SpotPublicUrl);

        var futuresUri = futOverride is not null
            ? new Uri(futOverride)
            : new Uri(BybitWebSocketClient.FuturesPublicUrl);

        return new BybitConfig(spotUri, futuresUri);
    }
}
