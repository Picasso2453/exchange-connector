using System.Threading.Channels;
using Connector.Core.Abstractions;
using Connector.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace Connector.Core.Managers;

/// <summary>
/// Manages WebSocket lifecycle: connect, subscribe, translate, fanout events.
/// Per-subscription ordering via Channel&lt;T&gt; per subscription key.
/// </summary>
public sealed class WebSocketManager : IAsyncDisposable
{
    private readonly IWsTransport _transport;
    private readonly IWsTranslator _translator;
    private readonly IAuthProvider _authProvider;
    private readonly IRateLimiter? _rateLimiter;
    private readonly ILogger<WebSocketManager> _logger;
    private readonly Channel<UnifiedWsEvent> _outputChannel;
    private readonly CancellationTokenSource _cts = new();
    private Task? _receiveLoop;
    private bool _started;

    public WebSocketManager(
        IWsTransport transport,
        IWsTranslator translator,
        IAuthProvider authProvider,
        ILogger<WebSocketManager> logger,
        IRateLimiter? rateLimiter = null,
        int channelCapacity = 10_000)
    {
        _transport = transport;
        _translator = translator;
        _authProvider = authProvider;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _outputChannel = Channel.CreateBounded<UnifiedWsEvent>(
            new BoundedChannelOptions(channelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false,
                SingleWriter = false
            });
    }

    public async Task StartAsync(Uri wsUri, CancellationToken ct)
    {
        if (_started) return;
        _started = true;

        _logger.LogInformation("Connecting to {Uri}", wsUri);
        await _transport.ConnectAsync(wsUri, ct);

        if (_authProvider.IsAuthenticated)
        {
            var authMsg = await _authProvider.GetWsAuthMessageAsync(ct);
            if (authMsg is not null)
            {
                _logger.LogInformation("Sending WS auth handshake");
                await _transport.SendAsync(authMsg, ct);
            }
        }

        _receiveLoop = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("WebSocket manager started");
    }

    public async Task StopAsync()
    {
        if (!_started) return;
        _cts.Cancel();

        try
        {
            await _transport.DisconnectAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during disconnect");
        }

        if (_receiveLoop is not null)
        {
            try { await _receiveLoop; }
            catch (OperationCanceledException) { }
        }

        _outputChannel.Writer.TryComplete();
        _logger.LogInformation("WebSocket manager stopped");
    }

    public async Task SubscribeAsync(UnifiedWsSubscribeRequest request, CancellationToken ct)
    {
        var messages = _translator.ToExchangeSubscribe(request);
        foreach (var msg in messages)
        {
            if (_rateLimiter is not null)
                await _rateLimiter.WaitAsync(ct);

            await _transport.SendAsync(msg, ct);
            _logger.LogDebug("Sent subscribe for {Channel} {Symbols}", request.Channel, request.Symbols);
        }
    }

    public async Task UnsubscribeAsync(UnifiedWsUnsubscribeRequest request, CancellationToken ct)
    {
        var messages = _translator.ToExchangeUnsubscribe(request);
        foreach (var msg in messages)
        {
            if (_rateLimiter is not null)
                await _rateLimiter.WaitAsync(ct);

            await _transport.SendAsync(msg, ct);
        }
    }

    public ChannelReader<UnifiedWsEvent> GetEventReader() => _outputChannel.Reader;

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var inbound in _transport.ReceiveAsync(ct))
            {
                List<UnifiedWsEvent> events;
                try
                {
                    // Materialize to list inside try to catch lazy enumeration exceptions
                    events = _translator.FromExchangeMessage(inbound).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to translate inbound message");
                    continue;
                }

                foreach (var evt in events)
                {
                    if (!_outputChannel.Writer.TryWrite(evt))
                    {
                        _logger.LogWarning("Output channel full, dropping oldest event");
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Receive loop failed");
        }
        finally
        {
            _outputChannel.Writer.TryComplete();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts.Dispose();
        await _transport.DisposeAsync();
    }
}
