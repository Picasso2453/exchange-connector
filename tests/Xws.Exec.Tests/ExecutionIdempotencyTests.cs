using Xws.Exec;

namespace Xws.Exec.Tests;

public sealed class ExecutionIdempotencyTests
{
    [Fact]
    public void Mainnet_MissingClientOrderId_Fails()
    {
        var config = new ExecutionConfig(ExecutionMode.Mainnet, true, "1");
        var request = new PlaceOrderRequest("HYPE", OrderSide.Buy, OrderType.Market, 1m);

        var result = ExecutionSafety.ValidateIdempotency(config, request);

        Assert.False(result.Ok);
    }

    [Fact]
    public void Mainnet_WithClientOrderId_Passes()
    {
        var config = new ExecutionConfig(ExecutionMode.Mainnet, true, "1");
        var request = new PlaceOrderRequest("HYPE", OrderSide.Buy, OrderType.Market, 1m, ClientOrderId: "client-1");

        var result = ExecutionSafety.ValidateIdempotency(config, request);

        Assert.True(result.Ok);
    }

    [Fact]
    public void Paper_MissingClientOrderId_Passes()
    {
        var config = new ExecutionConfig(ExecutionMode.Paper, false, null);
        var request = new PlaceOrderRequest("HYPE", OrderSide.Buy, OrderType.Market, 1m);

        var result = ExecutionSafety.ValidateIdempotency(config, request);

        Assert.True(result.Ok);
    }
}
