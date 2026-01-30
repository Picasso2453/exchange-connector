using Xws.Exec;

namespace Xws.Exec.Tests;

public sealed class ExecutionClientFactoryTests
{
    [Fact]
    public void Create_Paper_ReturnsPaperClient()
    {
        var config = new ExecutionConfig(ExecutionMode.Paper, false, null);
        var client = ExecutionClientFactory.Create(config);

        Assert.IsType<PaperExecutionClient>(client);
    }

    [Fact]
    public void Create_Testnet_ReturnsHyperliquidClient()
    {
        var config = new ExecutionConfig(ExecutionMode.Testnet, false, null);
        var client = ExecutionClientFactory.Create(config, new FakeRest());

        Assert.IsType<HyperliquidExecutionClient>(client);
    }

    [Fact]
    public void Create_Mainnet_ReturnsHyperliquidClient()
    {
        var config = new ExecutionConfig(ExecutionMode.Mainnet, true, "1");
        var client = ExecutionClientFactory.Create(config, new FakeRest());

        Assert.IsType<HyperliquidExecutionClient>(client);
    }

    private sealed class FakeRest : IHyperliquidRest
    {
        public Task<HyperliquidPlaceResult> PlaceOrderAsync(PlaceOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HyperliquidPlaceResult(null, null));
        }

        public Task<HyperliquidCancelResult> CancelOrderAsync(string orderId, string symbol, ExecutionConfig config, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HyperliquidCancelResult("success"));
        }

        public Task<IReadOnlyList<HyperliquidOpenOrder>> GetOpenOrdersAsync(string address, ExecutionConfig config, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<HyperliquidOpenOrder>>(Array.Empty<HyperliquidOpenOrder>());
        }

        public Task<HyperliquidCancelResult> CancelManyAsync(IReadOnlyList<string> orderIds, ExecutionConfig config, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HyperliquidCancelResult("success"));
        }
    }
}
