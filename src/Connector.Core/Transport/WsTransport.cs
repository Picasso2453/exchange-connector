using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using Connector.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Connector.Core.Transport;

/// <summary>
/// WebSocket transport with exponential backoff reconnect.
/// </summary>
public sealed class WsTransport : IWsTransport
{
    private readonly ILogger _logger;
    private ClientWebSocket? _ws;
    private Uri? _uri;
    private bool _disposed;

    // Reconnect policy
    private const int InitialBackoffMs = 500;
    private const int MaxBackoffMs = 30_000;
    private const double BackoffMultiplier = 2.0;
    private const double JitterFactor = 0.25;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public WsTransport(ILogger<WsTransport> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(Uri uri, CancellationToken ct)
    {
        _uri = uri;
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(uri, ct);
        _logger.LogInformation("WebSocket connected to {Uri}", uri);
    }

    public async Task DisconnectAsync(CancellationToken ct)
    {
        if (_ws is { State: WebSocketState.Open })
        {
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error during WS close");
            }
        }
        _ws?.Dispose();
        _ws = null;
    }

    public async Task SendAsync(TransportWsMessage message, CancellationToken ct)
    {
        if (_ws is null || _ws.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket not connected");

        var bytes = Encoding.UTF8.GetBytes(message.Payload);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    public async IAsyncEnumerable<TransportWsInbound> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var buffer = new byte[16384];
        var backoffMs = InitialBackoffMs;

        while (!ct.IsCancellationRequested)
        {
            if (_ws is null || _ws.State != WebSocketState.Open)
            {
                if (_uri is not null)
                {
                    await ReconnectWithBackoffAsync(backoffMs, ct);
                    backoffMs = InitialBackoffMs; // reset after successful connect
                }
                else
                {
                    yield break;
                }
            }

            WebSocketReceiveResult result;
            try
            {
                result = await _ws!.ReceiveAsync(buffer, ct);
                backoffMs = InitialBackoffMs; // reset on successful receive
            }
            catch (OperationCanceledException) { yield break; }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, "WebSocket receive error, will reconnect");
                _ws?.Dispose();
                _ws = null;
                backoffMs = Math.Min((int)(backoffMs * BackoffMultiplier), MaxBackoffMs);
                continue;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogInformation("Server initiated close");
                _ws?.Dispose();
                _ws = null;
                continue;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var payload = Encoding.UTF8.GetString(buffer, 0, result.Count);

                // Handle multi-frame messages
                if (!result.EndOfMessage)
                {
                    var sb = new StringBuilder(payload);
                    while (!result.EndOfMessage && !ct.IsCancellationRequested)
                    {
                        result = await _ws.ReceiveAsync(buffer, ct);
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    payload = sb.ToString();
                }

                yield return new TransportWsInbound
                {
                    Payload = payload,
                    ReceivedAt = DateTimeOffset.UtcNow
                };
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                // Collect full binary message
                var data = new byte[result.Count];
                Array.Copy(buffer, data, result.Count);

                if (!result.EndOfMessage)
                {
                    using var ms = new MemoryStream();
                    ms.Write(data, 0, data.Length);
                    while (!result.EndOfMessage && !ct.IsCancellationRequested)
                    {
                        result = await _ws.ReceiveAsync(buffer, ct);
                        ms.Write(buffer, 0, result.Count);
                    }
                    data = ms.ToArray();
                }

                yield return new TransportWsInbound
                {
                    Payload = Encoding.UTF8.GetString(data),
                    ReceivedAt = DateTimeOffset.UtcNow,
                    RawBytes = data
                };
            }
        }
    }

    private async Task ReconnectWithBackoffAsync(int backoffMs, CancellationToken ct)
    {
        // Add jitter
        var jitter = (int)(backoffMs * JitterFactor * Random.Shared.NextDouble());
        var delay = backoffMs + jitter;

        _logger.LogInformation("Reconnecting in {DelayMs}ms", delay);
        await Task.Delay(delay, ct);

        _ws?.Dispose();
        _ws = new ClientWebSocket();

        try
        {
            await _ws.ConnectAsync(_uri!, ct);
            _logger.LogInformation("Reconnected to {Uri}", _uri);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reconnect failed");
            _ws?.Dispose();
            _ws = null;
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await DisconnectAsync(CancellationToken.None);
    }
}
