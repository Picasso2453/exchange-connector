namespace Xws.Exec;

public sealed class RateLimitedHyperliquidRest : IHyperliquidRest
{
    private readonly IHyperliquidRest _inner;
    private readonly IRateLimiter _limiter;

    public RateLimitedHyperliquidRest(IHyperliquidRest inner, IRateLimiter limiter)
    {
        _inner = inner;
        _limiter = limiter;
    }

    public async Task<HyperliquidPlaceResult> PlaceOrderAsync(PlaceOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken)
    {
        await _limiter.WaitAsync(cancellationToken);
        return await _inner.PlaceOrderAsync(request, config, cancellationToken);
    }

    public async Task<HyperliquidCancelResult> CancelOrderAsync(string orderId, string symbol, ExecutionConfig config, CancellationToken cancellationToken)
    {
        await _limiter.WaitAsync(cancellationToken);
        return await _inner.CancelOrderAsync(orderId, symbol, config, cancellationToken);
    }

    public async Task<IReadOnlyList<HyperliquidOpenOrder>> GetOpenOrdersAsync(string address, ExecutionConfig config, CancellationToken cancellationToken)
    {
        await _limiter.WaitAsync(cancellationToken);
        return await _inner.GetOpenOrdersAsync(address, config, cancellationToken);
    }

    public async Task<HyperliquidCancelResult> CancelManyAsync(IReadOnlyList<string> orderIds, ExecutionConfig config, CancellationToken cancellationToken)
    {
        await _limiter.WaitAsync(cancellationToken);
        return await _inner.CancelManyAsync(orderIds, config, cancellationToken);
    }
}
