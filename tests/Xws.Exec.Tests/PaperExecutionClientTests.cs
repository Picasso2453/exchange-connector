using Xws.Exec;

namespace Xws.Exec.Tests;

public sealed class PaperExecutionClientTests
{
    [Fact]
    public async Task PlaceMarket_IsFilled_WithDeterministicIds()
    {
        var client = new PaperExecutionClient();

        var result = await client.PlaceAsync(new PlaceOrderRequest(
            "HYPE",
            OrderSide.Buy,
            OrderType.Market,
            1m), CancellationToken.None);

        Assert.Equal(OrderStatus.Filled, result.Status);
        Assert.Equal("000001", result.OrderId);
        Assert.Equal("paper-000001", result.ClientOrderId);
        Assert.Equal(ExecutionMode.Paper, result.Mode);
    }

    [Fact]
    public async Task PlaceLimit_IsOpen_AndCancelable()
    {
        var client = new PaperExecutionClient();

        var placed = await client.PlaceAsync(new PlaceOrderRequest(
            "HYPE",
            OrderSide.Sell,
            OrderType.Limit,
            2m,
            Price: 10m,
            ClientOrderId: "client-1"), CancellationToken.None);

        Assert.Equal(OrderStatus.Open, placed.Status);
        Assert.Equal("000001", placed.OrderId);
        Assert.Equal("client-1", placed.ClientOrderId);

        var cancelled = await client.CancelAsync(new CancelOrderRequest(OrderId: placed.OrderId), CancellationToken.None);

        Assert.True(cancelled.Success);
        Assert.Equal(placed.OrderId, cancelled.OrderId);
    }

    [Fact]
    public async Task CancelAll_ClearsOpenOrders()
    {
        var client = new PaperExecutionClient();

        await client.PlaceAsync(new PlaceOrderRequest(
            "HYPE",
            OrderSide.Buy,
            OrderType.Limit,
            1m,
            Price: 10m), CancellationToken.None);

        await client.PlaceAsync(new PlaceOrderRequest(
            "HYPE",
            OrderSide.Sell,
            OrderType.Limit,
            1m,
            Price: 11m), CancellationToken.None);

        var result = await client.CancelAllAsync(new CancelAllRequest(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.CancelledCount);
    }

    [Fact]
    public async Task Amend_OpenOrder_UpdatesPrice()
    {
        var client = new PaperExecutionClient();

        var placed = await client.PlaceAsync(new PlaceOrderRequest(
            "HYPE",
            OrderSide.Buy,
            OrderType.Limit,
            1m,
            Price: 10m), CancellationToken.None);

        var amended = await client.AmendAsync(new AmendOrderRequest(
            placed.OrderId,
            null,
            Price: 11m), CancellationToken.None);

        Assert.Equal(OrderStatus.Open, amended.Status);

        var orders = await client.QueryOrdersAsync(new QueryOrdersRequest(OrderQueryStatus.Open), CancellationToken.None);
        Assert.Contains(orders.Orders, o => o.OrderId == placed.OrderId && o.Price == 11m);
    }

    [Fact]
    public async Task QueryPositions_ReturnsFilledPosition()
    {
        var client = new PaperExecutionClient();

        await client.PlaceAsync(new PlaceOrderRequest(
            "HYPE",
            OrderSide.Buy,
            OrderType.Market,
            2m), CancellationToken.None);

        var positions = await client.QueryPositionsAsync(CancellationToken.None);
        Assert.Contains(positions.Positions, p => p.Symbol == "HYPE" && p.Size == 2m);
    }
}
