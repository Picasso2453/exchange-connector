using Xws.Exec;

namespace Xws.Exec.Tests;

public sealed class HyperliquidCancelAllTests
{
    [Fact]
    public async Task CancelAll_ReturnsZero_WhenNoOpenOrders()
    {
        var rest = new CancelAllRest();
        var config = new ExecutionConfig(ExecutionMode.Testnet, false, null, UserAddress: "0xabc");
        var client = new HyperliquidExecutionClient(config, rest);

        var result = await client.CancelAllAsync(new CancelAllRequest(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(0, result.CancelledCount);
        Assert.Equal(0, rest.CancelCalls);
        Assert.Equal(1, rest.GetOpenOrdersCalls);
    }

    [Fact]
    public async Task CancelAll_CancelsAllOpenOrders()
    {
        var rest = new CancelAllRest
        {
            OpenOrders =
            [
                new HyperliquidOpenOrder("1", null, "HYPE"),
                new HyperliquidOpenOrder("2", null, "HYPE")
            ]
        };
        var config = new ExecutionConfig(ExecutionMode.Testnet, false, null, UserAddress: "0xabc");
        var client = new HyperliquidExecutionClient(config, rest);

        var result = await client.CancelAllAsync(new CancelAllRequest(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.CancelledCount);
        Assert.Equal(2, rest.CancelCalls);
    }

    [Fact]
    public async Task CancelAll_MainnetWithoutArming_FailsClosed()
    {
        var rest = new CancelAllRest
        {
            OpenOrders = [new HyperliquidOpenOrder("1", null, "HYPE")]
        };
        var config = new ExecutionConfig(ExecutionMode.Mainnet, false, null, UserAddress: "0xabc");
        var client = new HyperliquidExecutionClient(config, rest);

        var result = await client.CancelAllAsync(new CancelAllRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(0, rest.GetOpenOrdersCalls);
        Assert.Equal(0, rest.CancelCalls);
    }

    private sealed class CancelAllRest : IHyperliquidRest
    {
        public IReadOnlyList<HyperliquidOpenOrder> OpenOrders { get; set; } = Array.Empty<HyperliquidOpenOrder>();
        public int GetOpenOrdersCalls { get; private set; }
        public int CancelCalls { get; private set; }

        public Task<HyperliquidPlaceResult> PlaceOrderAsync(PlaceOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken)
            => Task.FromResult(new HyperliquidPlaceResult("1", "open"));

        public Task<HyperliquidCancelResult> CancelOrderAsync(string orderId, string symbol, ExecutionConfig config, CancellationToken cancellationToken)
        {
            CancelCalls++;
            return Task.FromResult(new HyperliquidCancelResult("success"));
        }

        public Task<IReadOnlyList<HyperliquidOpenOrder>> GetOpenOrdersAsync(string address, ExecutionConfig config, CancellationToken cancellationToken)
        {
            GetOpenOrdersCalls++;
            return Task.FromResult(OpenOrders);
        }

        public Task<HyperliquidCancelResult> CancelManyAsync(IReadOnlyList<string> orderIds, ExecutionConfig config, CancellationToken cancellationToken)
            => Task.FromResult(new HyperliquidCancelResult("success"));
    }
}
