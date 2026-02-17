using Connector.Core.Abstractions;
using Connector.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace Connector.Core.Managers;

/// <summary>
/// Manages REST request execution with translation, auth, and rate limiting.
/// </summary>
public sealed class RestManager
{
    private readonly IRestTransport _transport;
    private readonly IRestTranslator _translator;
    private readonly IAuthProvider _authProvider;
    private readonly IRateLimiter? _rateLimiter;
    private readonly ILogger<RestManager> _logger;

    public RestManager(
        IRestTransport transport,
        IRestTranslator translator,
        IAuthProvider authProvider,
        ILogger<RestManager> logger,
        IRateLimiter? rateLimiter = null)
    {
        _transport = transport;
        _translator = translator;
        _authProvider = authProvider;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<TResponse> ExecuteAsync<TResponse>(
        UnifiedRestRequest<TResponse> request,
        CancellationToken ct)
    {
        if (_rateLimiter is not null)
            await _rateLimiter.WaitAsync(ct);

        var transportRequest = _translator.ToExchangeRequest(request);

        if (request.AuthRequired)
        {
            if (!_authProvider.IsAuthenticated)
                throw new InvalidOperationException(
                    $"Auth required for {request.Operation} but no auth provider configured");

            _authProvider.ApplyRestAuth(transportRequest);
        }

        _logger.LogDebug("REST {Method} {Path}", transportRequest.Method, transportRequest.Path);

        var response = await _transport.SendAsync(transportRequest, ct);

        if (response.StatusCode >= 400)
        {
            _logger.LogError("REST error {StatusCode}: {Body}", response.StatusCode, response.Body);
            throw new ConnectorException(
                $"REST {request.Operation} failed with status {response.StatusCode}",
                response.StatusCode,
                response.Body);
        }

        return _translator.FromExchangeResponse(request, response);
    }
}
