using System.Runtime.CompilerServices;
using Connector.Core.Abstractions;
using Connector.Core.Contracts;
using Connector.Core.Managers;
using Connector.Core.Transport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Connector.Tests;

public class ManagerLifecycleTests
{
    [Fact]
    public async Task WebSocketManager_StartStop_DoesNotThrow()
    {
        var transport = new FakeWsTransport();
        var translator = new FakeWsTranslator();
        var auth = new NoAuthProvider();
        var logger = NullLogger<WebSocketManager>.Instance;

        await using var manager = new WebSocketManager(transport, translator, auth, logger);
        await manager.StartAsync(new Uri("wss://fake.example.com"), CancellationToken.None);
        await manager.StopAsync();
    }

    [Fact]
    public async Task WebSocketManager_Subscribe_SendsTransportMessage()
    {
        var transport = new FakeWsTransport();
        var translator = new FakeWsTranslator();
        var auth = new NoAuthProvider();
        var logger = NullLogger<WebSocketManager>.Instance;

        await using var manager = new WebSocketManager(transport, translator, auth, logger);
        await manager.StartAsync(new Uri("wss://fake.example.com"), CancellationToken.None);

        await manager.SubscribeAsync(new UnifiedWsSubscribeRequest
        {
            CorrelationId = "test",
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Trades,
            Symbols = ["BTC"]
        }, CancellationToken.None);

        Assert.Single(transport.SentMessages);
        await manager.StopAsync();
    }

    [Fact]
    public async Task RestManager_ThrowsOnAuthRequiredWithoutProvider()
    {
        var transport = new FakeRestTransport();
        var translator = new FakeRestTranslator();
        var auth = new NoAuthProvider();
        var logger = NullLogger<RestManager>.Instance;

        var manager = new RestManager(transport, translator, auth, logger);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await manager.ExecuteAsync(new GetBalancesRequest
            {
                Exchange = UnifiedExchange.Hyperliquid
            }, CancellationToken.None);
        });
    }

    // --- Fakes ---

    private sealed class NoAuthProvider : IAuthProvider
    {
        public bool IsAuthenticated => false;
        public Task<TransportWsMessage?> GetWsAuthMessageAsync(CancellationToken ct) => Task.FromResult<TransportWsMessage?>(null);
        public void ApplyRestAuth(TransportRestRequest request) { }
    }

    private sealed class FakeWsTransport : IWsTransport
    {
        public List<TransportWsMessage> SentMessages { get; } = [];
        public bool IsConnected { get; private set; }

        public Task ConnectAsync(Uri uri, CancellationToken ct)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken ct)
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task SendAsync(TransportWsMessage message, CancellationToken ct)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<TransportWsInbound> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken ct)
        {
            // Block until cancelled
            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException) { }
            yield break;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeWsTranslator : IWsTranslator
    {
        public IEnumerable<TransportWsMessage> ToExchangeSubscribe(UnifiedWsSubscribeRequest request)
        {
            yield return new TransportWsMessage { Payload = $"subscribe:{request.Channel}" };
        }

        public IEnumerable<TransportWsMessage> ToExchangeUnsubscribe(UnifiedWsUnsubscribeRequest request)
        {
            yield return new TransportWsMessage { Payload = $"unsubscribe:{request.Channel}" };
        }

        public IEnumerable<UnifiedWsEvent> FromExchangeMessage(TransportWsInbound inbound)
        {
            yield break;
        }
    }

    private sealed class FakeRestTransport : IRestTransport
    {
        public Task<TransportRestResponse> SendAsync(TransportRestRequest request, CancellationToken ct)
        {
            return Task.FromResult(new TransportRestResponse
            {
                StatusCode = 200,
                Body = "{}"
            });
        }
    }

    private sealed class FakeRestTranslator : IRestTranslator
    {
        public TransportRestRequest ToExchangeRequest<TResponse>(UnifiedRestRequest<TResponse> request)
        {
            return new TransportRestRequest
            {
                Method = HttpMethod.Get,
                Path = "/fake"
            };
        }

        public TResponse FromExchangeResponse<TResponse>(UnifiedRestRequest<TResponse> request, TransportRestResponse response)
        {
            throw new NotImplementedException();
        }
    }
}
