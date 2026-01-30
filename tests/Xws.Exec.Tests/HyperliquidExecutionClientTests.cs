using Xws.Exec;

namespace Xws.Exec.Tests;

public sealed class HyperliquidExecutionClientTests
{
    [Fact]
    public async Task PlaceAsync_CallsRest_AndMapsResult()
    {
        var rest = new CapturingRest
        {
            PlaceResult = new HyperliquidPlaceResult("123", "open")
        };
        var config = new ExecutionConfig(ExecutionMode.Testnet, false, null);
        var client = new HyperliquidExecutionClient(config, rest);

        var request = new PlaceOrderRequest("HYPE", OrderSide.Buy, OrderType.Market, 1m);
        var result = await client.PlaceAsync(request, CancellationToken.None);

        Assert.Equal("HYPE", rest.LastPlaceRequest?.Symbol);
        Assert.Equal("123", result.OrderId);
        Assert.Equal(ExecutionMode.Testnet, result.Mode);
    }

    [Fact]
    public async Task PlaceAsync_MainnetWithoutClientOrderId_FailsClosed()
    {
        var rest = new CapturingRest();
        var config = new ExecutionConfig(ExecutionMode.Mainnet, true, "1");
        var client = new HyperliquidExecutionClient(config, rest);

        var request = new PlaceOrderRequest("HYPE", OrderSide.Buy, OrderType.Market, 1m);
        var result = await client.PlaceAsync(request, CancellationToken.None);

        Assert.Equal(OrderStatus.Rejected, result.Status);
        Assert.Null(rest.LastPlaceRequest);
    }

    [Fact]
    public async Task CancelAsync_CallsRest_WhenSymbolProvided()
    {
        var rest = new CapturingRest
        {
            CancelResult = new HyperliquidCancelResult("success")
        };
        var config = new ExecutionConfig(ExecutionMode.Testnet, false, null);
        var client = new HyperliquidExecutionClient(config, rest);

        var request = new CancelOrderRequest(OrderId: "1", Symbol: "HYPE");
        var result = await client.CancelAsync(request, CancellationToken.None);

        Assert.Equal("1", rest.LastCancelOrderId);
        Assert.Equal("HYPE", rest.LastCancelSymbol);
        Assert.True(result.Success);
    }

    private sealed class CapturingRest : IHyperliquidRest
    {
        public PlaceOrderRequest? LastPlaceRequest { get; private set; }
        public string? LastCancelOrderId { get; private set; }
        public string? LastCancelSymbol { get; private set; }
        public HyperliquidPlaceResult PlaceResult { get; set; } = new HyperliquidPlaceResult(null, null);
        public HyperliquidCancelResult CancelResult { get; set; } = new HyperliquidCancelResult("success");

        public Task<HyperliquidPlaceResult> PlaceOrderAsync(PlaceOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken)
        {
            LastPlaceRequest = request;
            return Task.FromResult(PlaceResult);
        }

        public Task<HyperliquidCancelResult> CancelOrderAsync(string orderId, string symbol, ExecutionConfig config, CancellationToken cancellationToken)
        {
            LastCancelOrderId = orderId;
            LastCancelSymbol = symbol;
            return Task.FromResult(CancelResult);
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
