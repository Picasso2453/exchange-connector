namespace Xws.Exec;

public sealed class HyperliquidExecutionClient : IExecutionClient
{
    private readonly ExecutionConfig _config;
    private readonly IHyperliquidRest _rest;

    public HyperliquidExecutionClient(ExecutionConfig config, IHyperliquidRest rest)
    {
        _config = config;
        _rest = rest;
    }

    public Task<PlaceOrderResult> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Hyperliquid execution not implemented in slice 6");
    }

    public Task<CancelOrderResult> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Hyperliquid execution not implemented in slice 6");
    }

    public Task<CancelAllResult> CancelAllAsync(CancelAllRequest request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Hyperliquid execution not implemented in slice 6");
    }
}
