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
        public Task<object?> PlaceAsync(PlaceOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken)
        {
            return Task.FromResult<object?>(null);
        }

        public Task<object?> CancelAsync(CancelOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken)
        {
            return Task.FromResult<object?>(null);
        }

        public Task<IReadOnlyList<object>> GetOpenOrdersAsync(ExecutionConfig config, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<object>>(Array.Empty<object>());
        }

        public Task<object?> CancelManyAsync(IReadOnlyList<string> orderIds, ExecutionConfig config, CancellationToken cancellationToken)
        {
            return Task.FromResult<object?>(null);
        }
    }
}
