using Connector.Core.Abstractions;
using Connector.Core.Transport;

namespace Connector.Core.Exchanges.Hyperliquid;

/// <summary>
/// No-op auth provider for public-only connections.
/// </summary>
public sealed class NoAuthProvider : IAuthProvider
{
    public bool IsAuthenticated => false;

    public Task<TransportWsMessage?> GetWsAuthMessageAsync(CancellationToken ct)
        => Task.FromResult<TransportWsMessage?>(null);

    public void ApplyRestAuth(TransportRestRequest request) { }
}
