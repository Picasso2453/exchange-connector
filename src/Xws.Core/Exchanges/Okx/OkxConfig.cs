using xws.Core.Env;

namespace xws.Exchanges.Okx;

public sealed class OkxConfig
{
    public Uri WsUri { get; }

    private OkxConfig(Uri wsUri)
    {
        WsUri = wsUri;
    }

    public static OkxConfig Load()
    {
        var wsOverride = EnvReader.GetOptional("XWS_OKX_WS_URL");
        var wsUri = wsOverride is not null
            ? new Uri(wsOverride)
            : new Uri(OkxWs.PublicUrl);

        return new OkxConfig(wsUri);
    }
}
