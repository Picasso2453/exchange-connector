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
        if (_config.Mode == ExecutionMode.Mainnet)
        {
            var arming = ExecutionSafety.ValidateArming(_config);
            if (!arming.Ok)
            {
                return Task.FromResult(new PlaceOrderResult(
                    OrderStatus.Rejected,
                    null,
                    request.ClientOrderId,
                    _config.Mode,
                    arming.Error));
            }

            var idempotency = ExecutionSafety.ValidateIdempotency(_config, request);
            if (!idempotency.Ok)
            {
                return Task.FromResult(new PlaceOrderResult(
                    OrderStatus.Rejected,
                    null,
                    request.ClientOrderId,
                    _config.Mode,
                    idempotency.Error));
            }
        }

        return PlaceInternalAsync(request, cancellationToken);
    }

    public Task<CancelOrderResult> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken)
    {
        if (_config.Mode == ExecutionMode.Mainnet)
        {
            var arming = ExecutionSafety.ValidateArming(_config);
            if (!arming.Ok)
            {
                return Task.FromResult(new CancelOrderResult(
                    false,
                    request.OrderId,
                    request.ClientOrderId,
                    _config.Mode,
                    arming.Error));
            }
        }

        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            return Task.FromResult(new CancelOrderResult(
                false,
                request.OrderId,
                request.ClientOrderId,
                _config.Mode,
                "orderId required"));
        }

        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            return Task.FromResult(new CancelOrderResult(
                false,
                request.OrderId,
                request.ClientOrderId,
                _config.Mode,
                "symbol required"));
        }

        return CancelInternalAsync(request.OrderId, request.Symbol, cancellationToken);
    }

    public Task<CancelAllResult> CancelAllAsync(CancelAllRequest request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Hyperliquid execution not implemented in slice 6");
    }

    private async Task<PlaceOrderResult> PlaceInternalAsync(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _rest.PlaceOrderAsync(request, _config, cancellationToken);
        return new PlaceOrderResult(
            OrderStatus.Accepted,
            result.OrderId,
            request.ClientOrderId,
            _config.Mode,
            result.Raw ?? result.Status);
    }

    private async Task<CancelOrderResult> CancelInternalAsync(string orderId, string symbol, CancellationToken cancellationToken)
    {
        var result = await _rest.CancelOrderAsync(orderId, symbol, _config, cancellationToken);
        return new CancelOrderResult(
            string.Equals(result.Status, "success", StringComparison.OrdinalIgnoreCase),
            orderId,
            null,
            _config.Mode,
            result.Raw ?? result.Status);
    }
}
