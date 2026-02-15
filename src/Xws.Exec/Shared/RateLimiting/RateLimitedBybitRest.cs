namespace Xws.Exec;

public sealed class RateLimitedBybitRest : IBybitRest
{
    private readonly IBybitRest _inner;
    private readonly IRateLimiter _limiter;

    public RateLimitedBybitRest(IBybitRest inner, IRateLimiter limiter)
    {
        _inner = inner;
        _limiter = limiter;
    }

    public async Task<object?> PlaceOrderAsync(PlaceOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken)
    {
        await _limiter.WaitAsync(cancellationToken);
        return await _inner.PlaceOrderAsync(request, config, cancellationToken);
    }

    public async Task<object?> CancelOrderAsync(string orderId, ExecutionConfig config, CancellationToken cancellationToken)
    {
        await _limiter.WaitAsync(cancellationToken);
        return await _inner.CancelOrderAsync(orderId, config, cancellationToken);
    }

    public async Task<object?> CancelAllAsync(CancelAllRequest request, ExecutionConfig config, CancellationToken cancellationToken)
    {
        await _limiter.WaitAsync(cancellationToken);
        return await _inner.CancelAllAsync(request, config, cancellationToken);
    }

    public async Task<object?> AmendOrderAsync(AmendOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken)
    {
        await _limiter.WaitAsync(cancellationToken);
        return await _inner.AmendOrderAsync(request, config, cancellationToken);
    }

    public async Task<object?> QueryOrdersAsync(QueryOrdersRequest request, ExecutionConfig config, CancellationToken cancellationToken)
    {
        await _limiter.WaitAsync(cancellationToken);
        return await _inner.QueryOrdersAsync(request, config, cancellationToken);
    }

    public async Task<object?> QueryPositionsAsync(ExecutionConfig config, CancellationToken cancellationToken)
    {
        await _limiter.WaitAsync(cancellationToken);
        return await _inner.QueryPositionsAsync(config, cancellationToken);
    }
}
