using Connector.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Connector.Core.Transport;

/// <summary>
/// REST transport using HttpClient with rate limiting and auth hooks.
/// </summary>
public sealed class RestTransport : IRestTransport
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public RestTransport(HttpClient httpClient, ILogger<RestTransport> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TransportRestResponse> SendAsync(TransportRestRequest request, CancellationToken ct)
    {
        var uriBuilder = new UriBuilder(_httpClient.BaseAddress!)
        {
            Path = request.Path
        };

        if (request.QueryParams is { Count: > 0 })
        {
            var query = string.Join("&",
                request.QueryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            uriBuilder.Query = query;
        }

        using var httpRequest = new HttpRequestMessage(request.Method, uriBuilder.Uri);

        if (request.Body is not null)
        {
            httpRequest.Content = new StringContent(
                request.Body,
                System.Text.Encoding.UTF8,
                request.ContentType ?? "application/json");
        }

        if (request.Headers is not null)
        {
            foreach (var (key, value) in request.Headers)
            {
                httpRequest.Headers.TryAddWithoutValidation(key, value);
            }
        }

        _logger.LogDebug("REST {Method} {Uri}", request.Method, uriBuilder.Uri);

        var httpResponse = await _httpClient.SendAsync(httpRequest, ct);
        var body = await httpResponse.Content.ReadAsStringAsync(ct);

        var responseHeaders = httpResponse.Headers
            .ToDictionary(h => h.Key, h => string.Join(",", h.Value));

        return new TransportRestResponse
        {
            StatusCode = (int)httpResponse.StatusCode,
            Body = body,
            Headers = responseHeaders
        };
    }
}
