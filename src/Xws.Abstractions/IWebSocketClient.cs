namespace Xws.Abstractions;

public interface IWebSocketClient
{
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task SendAsync(string message, CancellationToken cancellationToken);
    Task<string?> ReceiveAsync(CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
}
