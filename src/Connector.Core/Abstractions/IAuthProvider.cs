using Connector.Core.Transport;

namespace Connector.Core.Abstractions;

/// <summary>
/// Authentication provider. Applied to both WS (auth handshake) and REST (headers/signatures).
/// </summary>
public interface IAuthProvider
{
    bool IsAuthenticated { get; }
    Task<TransportWsMessage?> GetWsAuthMessageAsync(CancellationToken ct);
    void ApplyRestAuth(TransportRestRequest request);
}
