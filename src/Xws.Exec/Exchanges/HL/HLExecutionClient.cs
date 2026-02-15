using System.Linq;

namespace Xws.Exec;

public sealed class HLExecutionClient : IExecutionClient
{
    private readonly ExecutionConfig _config;
    private readonly IHyperliquidRest _rest;

    public HLExecutionClient(ExecutionConfig config, IHyperliquidRest rest)
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
        if (_config.Mode == ExecutionMode.Mainnet)
        {
            var arming = ExecutionSafety.ValidateArming(_config);
            if (!arming.Ok)
            {
                return Task.FromResult(new CancelAllResult(
                    false,
                    0,
                    _config.Mode,
                    arming.Error));
            }
        }

        return CancelAllInternalAsync(request, cancellationToken);
    }

    public Task<AmendOrderResult> AmendAsync(AmendOrderRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AmendOrderResult(
            OrderStatus.Rejected,
            request.OrderId,
            request.ClientOrderId,
            _config.Mode,
            "amend not implemented"));
    }

    public Task<QueryOrdersResult> QueryOrdersAsync(QueryOrdersRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new QueryOrdersResult(_config.Mode, Array.Empty<OrderState>(), "query orders not implemented"));
    }

    public Task<QueryPositionsResult> QueryPositionsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new QueryPositionsResult(_config.Mode, Array.Empty<PositionState>(), "query positions not implemented"));
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

    private async Task<CancelAllResult> CancelAllInternalAsync(CancelAllRequest request, CancellationToken cancellationToken)
    {
        var address = _config.UserAddress ?? _config.HyperliquidCredentials?.AccountAddress;
        if (string.IsNullOrWhiteSpace(address))
        {
            return new CancelAllResult(false, 0, _config.Mode, "account address required");
        }

        var openOrders = await _rest.GetOpenOrdersAsync(address, _config, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.Symbol))
        {
            openOrders = openOrders
                .Where(order => string.Equals(order.Symbol, request.Symbol, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (openOrders.Count == 0)
        {
            return new CancelAllResult(true, 0, _config.Mode);
        }

        var cancelled = 0;
        foreach (var order in openOrders)
        {
            if (string.IsNullOrWhiteSpace(order.OrderId))
            {
                return new CancelAllResult(false, cancelled, _config.Mode, "orderId required");
            }

            if (string.IsNullOrWhiteSpace(order.Symbol))
            {
                return new CancelAllResult(false, cancelled, _config.Mode, "symbol required");
            }

            var result = await _rest.CancelOrderAsync(order.OrderId, order.Symbol, _config, cancellationToken);
            var ok = string.Equals(result.Status, "success", StringComparison.OrdinalIgnoreCase);
            if (!ok)
            {
                return new CancelAllResult(false, cancelled, _config.Mode, result.Raw ?? result.Status);
            }

            cancelled++;
        }

        return new CancelAllResult(true, cancelled, _config.Mode);
    }
}
