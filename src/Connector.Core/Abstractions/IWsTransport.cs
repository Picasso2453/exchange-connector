using Connector.Core.Transport;

namespace Connector.Core.Abstractions;

/// <summary>
/// Low-level WebSocket transport: connect, send, receive, reconnect.
/// </summary>
public interface IWsTransport : IAsyncDisposable
{
    Task ConnectAsync(Uri uri, CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task SendAsync(TransportWsMessage message, CancellationToken ct);
    IAsyncEnumerable<TransportWsInbound> ReceiveAsync(CancellationToken ct);
    bool IsConnected { get; }
}
