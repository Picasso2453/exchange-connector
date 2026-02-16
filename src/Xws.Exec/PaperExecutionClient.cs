using System.Collections.Concurrent;
namespace Xws.Exec;

/// <summary>
/// In-memory paper execution client with optional state persistence.
/// </summary>
public sealed class PaperExecutionClient : IExecutionClient
{
    private readonly ExecutionMode _mode;
    private readonly string? _statePath;
    private long _orderSequence;
    private long _clientOrderSequence;
    private readonly ConcurrentDictionary<string, PaperOrder> _orders = new();
    private readonly ConcurrentDictionary<string, string> _ordersByClientId = new();
    private readonly ConcurrentDictionary<string, PositionState> _positions = new();
    private const decimal DefaultFillPrice = 100m;

    /// <summary>
    /// Creates a paper execution client in paper mode.
    /// </summary>
    public PaperExecutionClient()
        : this(ExecutionMode.Paper)
    {
    }

    /// <summary>
    /// Creates a paper execution client for the specified mode and optional state path.
    /// </summary>
    /// <param name="mode">Execution mode (paper/testnet/mainnet).</param>
    /// <param name="statePath">Optional path for persisting paper state.</param>
    public PaperExecutionClient(ExecutionMode mode, string? statePath = null)
    {
        _mode = mode;
        _statePath = statePath;
        if (!string.IsNullOrWhiteSpace(_statePath))
        {
            LoadState(_statePath);
        }
    }

    /// <summary>
    /// Places a paper order and updates local state.
    /// </summary>
    /// <param name="request">Order request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Placement result.</returns>
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

        var updatedAt = DateTimeOffset.UtcNow;
        if (request.Type == OrderType.Market)
        {
            var fillPrice = request.Price ?? DefaultFillPrice;
            ApplyFill(request, fillPrice, updatedAt);

            var filledOrder = new PaperOrder(
                orderId,
                clientOrderId,
                request.Symbol,
                request.Side,
                request.Type,
                request.Size,
                request.Price,
                request.Size,
                OrderStatus.Filled,
                updatedAt);
            _orders[orderId] = filledOrder;
            _ordersByClientId[clientOrderId] = orderId;
            PersistState();

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
            0m,
            OrderStatus.Open,
            updatedAt);

        _orders[orderId] = order;
        _ordersByClientId[clientOrderId] = orderId;
        PersistState();
        return Task.FromResult(new PlaceOrderResult(
            OrderStatus.Open,
            orderId,
            clientOrderId,
            _mode));
    }

    /// <summary>
    /// Cancels a paper order and updates local state.
    /// </summary>
    /// <param name="request">Cancel request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cancel result.</returns>
    public Task<CancelOrderResult> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.OrderId)
            && _orders.TryGetValue(request.OrderId, out var removed)
            && removed.Status == OrderStatus.Open)
        {
            _orders[removed.OrderId] = removed with
            {
                Status = OrderStatus.Cancelled,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            PersistState();
            return Task.FromResult(new CancelOrderResult(
                true,
                removed.OrderId,
                removed.ClientOrderId,
                _mode));
        }

        if (!string.IsNullOrWhiteSpace(request.ClientOrderId)
            && _ordersByClientId.TryGetValue(request.ClientOrderId, out var mappedOrderId)
            && _orders.TryGetValue(mappedOrderId, out var mappedOrder)
            && mappedOrder.Status == OrderStatus.Open)
        {
            _orders[mappedOrder.OrderId] = mappedOrder with
            {
                Status = OrderStatus.Cancelled,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            PersistState();
            return Task.FromResult(new CancelOrderResult(
                true,
                mappedOrder.OrderId,
                mappedOrder.ClientOrderId,
                _mode));
        }

        return Task.FromResult(new CancelOrderResult(
            false,
            request.OrderId,
            request.ClientOrderId,
            _mode,
            "order not found"));
    }

    /// <summary>
    /// Cancels all open paper orders.
    /// </summary>
    /// <param name="request">Cancel-all request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cancel-all result.</returns>
    public Task<CancelAllResult> CancelAllAsync(CancelAllRequest request, CancellationToken cancellationToken)
    {
        var count = 0;
        foreach (var order in _orders.Values)
        {
            if (order.Status != OrderStatus.Open)
            {
                continue;
            }

            _orders[order.OrderId] = order with
            {
                Status = OrderStatus.Cancelled,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            count++;
        }

        if (count > 0)
        {
            PersistState();
        }

        return Task.FromResult(new CancelAllResult(
            true,
            count,
            _mode));
    }

    /// <summary>
    /// Amends a paper order and updates local state.
    /// </summary>
    /// <param name="request">Amend request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Amend result.</returns>
    public Task<AmendOrderResult> AmendAsync(AmendOrderRequest request, CancellationToken cancellationToken)
    {
        var match = ResolveOrder(request);
        if (match is null)
        {
            return Task.FromResult(new AmendOrderResult(
                OrderStatus.Rejected,
                request.OrderId,
                request.ClientOrderId,
                _mode,
                "order not found"));
        }

        if (match.Status != OrderStatus.Open)
        {
            return Task.FromResult(new AmendOrderResult(
                OrderStatus.Rejected,
                match.OrderId,
                match.ClientOrderId,
                _mode,
                "order not open"));
        }

        var amended = match with
        {
            Price = request.Price ?? match.Price,
            Size = request.Size ?? match.Size,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _orders[amended.OrderId] = amended;
        PersistState();

        return Task.FromResult(new AmendOrderResult(
            amended.Status,
            amended.OrderId,
            amended.ClientOrderId,
            _mode));
    }

    /// <summary>
    /// Queries paper orders using filters.
    /// </summary>
    /// <param name="request">Query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query result.</returns>
    public Task<QueryOrdersResult> QueryOrdersAsync(QueryOrdersRequest request, CancellationToken cancellationToken)
    {
        var results = new List<OrderState>();
        foreach (var order in _orders.Values)
        {
            if (!string.IsNullOrWhiteSpace(request.OrderId)
                && !string.Equals(order.OrderId, request.OrderId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(request.Symbol)
                && !string.Equals(order.Symbol, request.Symbol, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (request.Status == OrderQueryStatus.Open && order.Status != OrderStatus.Open)
            {
                continue;
            }

            if (request.Status == OrderQueryStatus.Closed && order.Status == OrderStatus.Open)
            {
                continue;
            }

            results.Add(new OrderState(
                order.OrderId,
                order.ClientOrderId,
                order.Symbol,
                order.Side,
                order.Type,
                order.Size,
                order.Price,
                order.FilledSize,
                order.Status,
                order.UpdatedAt));
        }

        results.Sort(static (a, b) => string.CompareOrdinal(a.OrderId, b.OrderId));
        return Task.FromResult(new QueryOrdersResult(_mode, results));
    }

    /// <summary>
    /// Queries current paper positions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Positions result.</returns>
    public Task<QueryPositionsResult> QueryPositionsAsync(CancellationToken cancellationToken)
    {
        var positions = _positions.Values.ToList();
        positions.Sort(static (a, b) => string.CompareOrdinal(a.Symbol, b.Symbol));
        return Task.FromResult(new QueryPositionsResult(_mode, positions));
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

    private void ApplyFill(PlaceOrderRequest request, decimal fillPrice, DateTimeOffset timestamp)
    {
        var signedSize = request.Side == OrderSide.Buy ? request.Size : -request.Size;
        var symbol = request.Symbol;

        _positions.AddOrUpdate(
            symbol,
            _ =>
            {
                var size = signedSize;
                var avg = fillPrice;
                var mark = fillPrice;
                var pnl = (mark - avg) * size;
                return new PositionState(symbol, size, avg, mark, pnl);
            },
            (_, existing) =>
            {
                var newSize = existing.Size + signedSize;
                decimal avgEntry;
                if (existing.Size == 0 || Math.Sign(existing.Size) == Math.Sign(signedSize))
                {
                    avgEntry = newSize == 0
                        ? 0m
                        : ((existing.AvgEntryPrice * existing.Size) + (fillPrice * signedSize)) / newSize;
                }
                else
                {
                    avgEntry = newSize == 0 ? 0m : fillPrice;
                }

                var mark = fillPrice;
                var pnl = (mark - avgEntry) * newSize;
                return new PositionState(symbol, newSize, avgEntry, mark, pnl);
            });
    }

    private PaperOrder? ResolveOrder(AmendOrderRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.OrderId)
            && _orders.TryGetValue(request.OrderId, out var order))
        {
            return order;
        }

        if (!string.IsNullOrWhiteSpace(request.ClientOrderId))
        {
            if (_ordersByClientId.TryGetValue(request.ClientOrderId, out var mappedOrderId)
                && _orders.TryGetValue(mappedOrderId, out var mappedOrder))
            {
                return mappedOrder;
            }

            foreach (var candidate in _orders.Values)
            {
                if (string.Equals(candidate.ClientOrderId, request.ClientOrderId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private void LoadState(string path)
    {
        var state = PaperStateStore.LoadOrEmpty(path, message => Console.Error.WriteLine($"Warning: {message}"));

        _orderSequence = state.OrderSequence;
        _clientOrderSequence = state.ClientOrderSequence;
        foreach (var order in state.Orders)
        {
            _orders[order.OrderId] = order;
            _ordersByClientId[order.ClientOrderId] = order.OrderId;
        }

        foreach (var position in state.Positions)
        {
            _positions[position.Symbol] = position;
        }
    }

    private void PersistState()
    {
        if (string.IsNullOrWhiteSpace(_statePath))
        {
            return;
        }

        var state = new PaperStateSnapshot(
            PaperStateStore.CurrentVersion,
            _orderSequence,
            _clientOrderSequence,
            _orders.Values.OrderBy(o => o.OrderId).ToList(),
            _positions.Values.OrderBy(p => p.Symbol).ToList());

        PaperStateStore.Save(_statePath, state);
    }
}
