using System.Collections.Concurrent;
using System.Linq;

namespace Xws.Exec;

public sealed class PaperExecutionClient : IExecutionClient
{
    private readonly ExecutionMode _mode;
    private long _orderSequence;
    private long _clientOrderSequence;
    private readonly ConcurrentDictionary<string, PaperOrder> _openOrders = new();

    public PaperExecutionClient()
        : this(ExecutionMode.Paper)
    {
    }

    public PaperExecutionClient(ExecutionMode mode)
    {
        _mode = mode;
    }

    public Task<PlaceOrderResult> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Size <= 0)
        {
            return Task.FromResult(new PlaceOrderResult(
                OrderStatus.Rejected,
                null,
                request.ClientOrderId,
                _mode,
                "size must be greater than 0"));
        }

        var orderId = NextOrderId();
        var clientOrderId = request.ClientOrderId ?? NextClientOrderId();

        if (request.Type == OrderType.Market)
        {
            return Task.FromResult(new PlaceOrderResult(
                OrderStatus.Filled,
                orderId,
                clientOrderId,
                _mode));
        }

        var order = new PaperOrder(
            orderId,
            clientOrderId,
            request.Symbol,
            request.Side,
            request.Type,
            request.Size,
            request.Price,
            request.ReduceOnly);

        _openOrders[orderId] = order;
        return Task.FromResult(new PlaceOrderResult(
            OrderStatus.Open,
            orderId,
            clientOrderId,
            _mode));
    }

    public Task<CancelOrderResult> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.OrderId)
            && _openOrders.TryRemove(request.OrderId, out var removed))
        {
            return Task.FromResult(new CancelOrderResult(
                true,
                removed.OrderId,
                removed.ClientOrderId,
                _mode));
        }

        if (!string.IsNullOrWhiteSpace(request.ClientOrderId))
        {
            var match = _openOrders.Values.FirstOrDefault(o => o.ClientOrderId == request.ClientOrderId);
            if (match is not null && _openOrders.TryRemove(match.OrderId, out var removedByClient))
            {
                return Task.FromResult(new CancelOrderResult(
                    true,
                    removedByClient.OrderId,
                    removedByClient.ClientOrderId,
                    _mode));
            }
        }

        return Task.FromResult(new CancelOrderResult(
            false,
            request.OrderId,
            request.ClientOrderId,
            _mode,
            "order not found"));
    }

    public Task<CancelAllResult> CancelAllAsync(CancelAllRequest request, CancellationToken cancellationToken)
    {
        var count = 0;
        foreach (var orderId in _openOrders.Keys)
        {
            if (_openOrders.TryRemove(orderId, out _))
            {
                count++;
            }
        }

        return Task.FromResult(new CancelAllResult(
            true,
            count,
            _mode));
    }

    private string NextOrderId()
    {
        var id = Interlocked.Increment(ref _orderSequence);
        return id.ToString("D6");
    }

    private string NextClientOrderId()
    {
        var id = Interlocked.Increment(ref _clientOrderSequence);
        return $"paper-{id:D6}";
    }

    private sealed record PaperOrder(
        string OrderId,
        string ClientOrderId,
        string Symbol,
        OrderSide Side,
        OrderType Type,
        decimal Size,
        decimal? Price,
        bool ReduceOnly);
}
