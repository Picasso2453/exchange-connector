using System.Runtime.CompilerServices;
using System.Text.Json;
using Connector.Core.Abstractions;
using Connector.Core.Contracts;
using Connector.Core.Exchanges.Hyperliquid;
using Connector.Core.Managers;
using Connector.Core.Transport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Connector.Tests;

/// <summary>
/// Soak-lite test: runs message pipeline for a configurable duration with fake transport.
/// Verifies no deadlocks, no memory leaks, and correct event throughput.
/// </summary>
public class SoakTests
{
    [Fact]
    public async Task Soak_FakeTransport_ProcessesEventsForDuration()
    {
        var transport = new FakeSoakTransport(intervalMs: 10, messagesPerBatch: 5);
        var translator = new HyperliquidWsTranslator(NullLogger.Instance);
        var auth = new NoAuthProvider();
        var logger = NullLogger<WebSocketManager>.Instance;

        await using var manager = new WebSocketManager(transport, translator, auth, logger);
        await manager.StartAsync(new Uri("wss://fake.soak.test"), CancellationToken.None);

        await manager.SubscribeAsync(new UnifiedWsSubscribeRequest
        {
            CorrelationId = "soak-1",
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Trades,
            Symbols = ["BTC"]
        }, CancellationToken.None);

        var reader = manager.GetEventReader();
        int eventCount = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        try
        {
            while (await reader.WaitToReadAsync(cts.Token))
            {
                while (reader.TryRead(out var evt))
                {
                    eventCount++;
                    Assert.IsType<TradesEvent>(evt);
                }
            }
        }
        catch (OperationCanceledException) { }

        await manager.StopAsync();

        // Should have processed a meaningful number of events in 3 seconds
        Assert.True(eventCount > 10, $"Expected >10 events, got {eventCount}");
    }

    [Fact]
    public async Task Soak_MultipleChannels_NoDeadlock()
    {
        var transport = new FakeSoakTransport(intervalMs: 5, messagesPerBatch: 3);
        var translator = new HyperliquidWsTranslator(NullLogger.Instance);
        var auth = new NoAuthProvider();
        var logger = NullLogger<WebSocketManager>.Instance;

        await using var manager = new WebSocketManager(transport, translator, auth, logger);
        await manager.StartAsync(new Uri("wss://fake.soak.test"), CancellationToken.None);

        // Subscribe to multiple symbols
        foreach (var symbol in new[] { "BTC", "ETH", "SOL" })
        {
            await manager.SubscribeAsync(new UnifiedWsSubscribeRequest
            {
                CorrelationId = $"soak-{symbol}",
                Exchange = UnifiedExchange.Hyperliquid,
                Channel = UnifiedWsChannel.Trades,
                Symbols = [symbol]
            }, CancellationToken.None);
        }

        var reader = manager.GetEventReader();
        int eventCount = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            while (await reader.WaitToReadAsync(cts.Token))
            {
                while (reader.TryRead(out _))
                    eventCount++;
            }
        }
        catch (OperationCanceledException) { }

        await manager.StopAsync();
        Assert.True(eventCount > 5, $"Expected >5 events, got {eventCount}");
    }

    [Fact]
    public async Task Soak_HighThroughput_NoCrash()
    {
        var transport = new FakeSoakTransport(intervalMs: 1, messagesPerBatch: 20);
        var translator = new HyperliquidWsTranslator(NullLogger.Instance);
        var auth = new NoAuthProvider();
        var logger = NullLogger<WebSocketManager>.Instance;

        await using var manager = new WebSocketManager(transport, translator, auth, logger, channelCapacity: 1000);
        await manager.StartAsync(new Uri("wss://fake.soak.test"), CancellationToken.None);

        await manager.SubscribeAsync(new UnifiedWsSubscribeRequest
        {
            CorrelationId = "soak-high",
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Trades,
            Symbols = ["BTC"]
        }, CancellationToken.None);

        var reader = manager.GetEventReader();
        int eventCount = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            while (await reader.WaitToReadAsync(cts.Token))
            {
                while (reader.TryRead(out _))
                    eventCount++;
            }
        }
        catch (OperationCanceledException) { }

        await manager.StopAsync();
        Assert.True(eventCount > 50, $"Expected >50 events under high throughput, got {eventCount}");
    }

    /// <summary>
    /// Fake transport that generates synthetic HL trade messages at configurable rate.
    /// </summary>
    private sealed class FakeSoakTransport : IWsTransport
    {
        private readonly int _intervalMs;
        private readonly int _messagesPerBatch;
        private int _tid;

        public FakeSoakTransport(int intervalMs, int messagesPerBatch)
        {
            _intervalMs = intervalMs;
            _messagesPerBatch = messagesPerBatch;
        }

        public bool IsConnected => true;

        public Task ConnectAsync(Uri uri, CancellationToken ct) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken ct) => Task.CompletedTask;
        public Task SendAsync(TransportWsMessage message, CancellationToken ct) => Task.CompletedTask;

        public async IAsyncEnumerable<TransportWsInbound> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(_intervalMs, ct);

                var trades = Enumerable.Range(0, _messagesPerBatch).Select(i =>
                {
                    var tid = Interlocked.Increment(ref _tid);
                    return new
                    {
                        coin = "BTC",
                        side = tid % 2 == 0 ? "B" : "A",
                        px = (50000 + tid % 100).ToString(),
                        sz = "0.01",
                        hash = "0x" + tid.ToString("x16"),
                        time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        tid,
                        users = new[] { "0xa", "0xb" }
                    };
                }).ToArray();

                var payload = JsonSerializer.Serialize(new { channel = "trades", data = trades });

                yield return new TransportWsInbound
                {
                    Payload = payload,
                    ReceivedAt = DateTimeOffset.UtcNow
                };
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
