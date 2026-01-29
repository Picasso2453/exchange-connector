using System.Net.Http.Json;

namespace xws.Exchanges.Hyperliquid;

public static class HyperliquidHttp
{
    public const string MainnetUrl = "https://api.hyperliquid.xyz/info";
    public const string TestnetUrl = "https://api.hyperliquid-testnet.xyz/info";

    private static readonly HttpClient Client = new();

    public static async Task<string> PostInfoAsync(Uri baseUri, string type, CancellationToken cancellationToken)
    {
        var request = new { type };
        using var response = await Client.PostAsJsonAsync(baseUri, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
