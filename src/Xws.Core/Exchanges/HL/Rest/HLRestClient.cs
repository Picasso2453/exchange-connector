using System.Net.Http;
using System.Net.Http.Json;

namespace xws.Exchanges.Hyperliquid.Rest;

public static class HLRestClient
{
    public const string MainnetUrl = "https://api.hyperliquid.xyz";
    public const string TestnetUrl = "https://api.hyperliquid-testnet.xyz";
    private static readonly HttpClient SharedClient = CreateClient();

    public static async Task<string> PostInfoAsync(Uri baseUri, string requestType, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "info"));
        var payload = new { type = requestType };
        request.Content = JsonContent.Create(payload);
        var response = await SharedClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };
        var client = new HttpClient(handler);
        return client;
    }
}
