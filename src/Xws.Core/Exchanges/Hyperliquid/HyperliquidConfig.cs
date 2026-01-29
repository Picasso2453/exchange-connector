using xws.Core.Env;

namespace xws.Exchanges.Hyperliquid;

public sealed class HyperliquidConfig
{
    public Uri WsUri { get; }
    public Uri HttpUri { get; }

    private HyperliquidConfig(Uri wsUri, Uri httpUri)
    {
        WsUri = wsUri;
        HttpUri = httpUri;
    }

    public static HyperliquidConfig Load()
    {
        var network = EnvReader.GetOptional("XWS_HL_NETWORK")?.ToLowerInvariant() ?? "mainnet";
        if (network != "mainnet" && network != "testnet")
        {
            throw new InvalidOperationException("XWS_HL_NETWORK must be mainnet or testnet");
        }

        var wsOverride = EnvReader.GetOptional("XWS_HL_WS_URL");
        var httpOverride = EnvReader.GetOptional("XWS_HL_HTTP_URL");

        var wsUri = wsOverride is not null
            ? new Uri(wsOverride)
            : new Uri(network == "testnet" ? HyperliquidWs.TestnetUrl : HyperliquidWs.MainnetUrl);

        var httpUri = httpOverride is not null
            ? new Uri(httpOverride)
            : new Uri(network == "testnet" ? HyperliquidHttp.TestnetUrl : HyperliquidHttp.MainnetUrl);

        return new HyperliquidConfig(wsUri, httpUri);
    }
}
