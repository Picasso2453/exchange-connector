using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;

namespace Xws.Exec;

public sealed class PaperExecutionClient : IExecutionClient
{
    private readonly ExecutionMode _mode;
    private readonly string? _statePath;
    private long _orderSequence;
    private long _clientOrderSequence;
    private readonly ConcurrentDictionary<string, PaperOrder> _orders = new();
    private readonly ConcurrentDictionary<string, PositionState> _positions = new();
    private const decimal DefaultFillPrice = 100m;

    public PaperExecutionClient()
        : this(ExecutionMode.Paper)
    {
    }

    public PaperExecutionClient(ExecutionMode mode, string? statePath = null)
    {
        _mode = mode;
        _statePath = statePath;
        if (!string.IsNullOrWhiteSpace(_statePath))
        {
            LoadState(_statePath);
        }
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
        PersistState();
        return Task.FromResult(new PlaceOrderResult(
            OrderStatus.Open,
            orderId,
            clientOrderId,
            _mode));
    }

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

        if (!string.IsNullOrWhiteSpace(request.ClientOrderId))
        {
            var match = _orders.Values.FirstOrDefault(o => o.ClientOrderId == request.ClientOrderId
                && o.Status == OrderStatus.Open);
            if (match is not null)
            {
                _orders[match.OrderId] = match with
                {
                    Status = OrderStatus.Cancelled,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                PersistState();
                return Task.FromResult(new CancelOrderResult(
                    true,
                    match.OrderId,
                    match.ClientOrderId,
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
        foreach (var order in _orders.Values.Where(o => o.Status == OrderStatus.Open))
        {
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

    public Task<QueryOrdersResult> QueryOrdersAsync(QueryOrdersRequest request, CancellationToken cancellationToken)
    {
        IEnumerable<PaperOrder> orders = _orders.Values;

        if (!string.IsNullOrWhiteSpace(request.OrderId))
        {
            orders = orders.Where(o => o.OrderId == request.OrderId);
        }

        if (!string.IsNullOrWhiteSpace(request.Symbol))
        {
            orders = orders.Where(o => string.Equals(o.Symbol, request.Symbol, StringComparison.OrdinalIgnoreCase));
        }

        orders = request.Status switch
        {
            OrderQueryStatus.Open => orders.Where(o => o.Status == OrderStatus.Open),
            OrderQueryStatus.Closed => orders.Where(o => o.Status != OrderStatus.Open),
            _ => orders
        };

        var results = orders
            .OrderBy(o => o.OrderId)
            .Select(o => new OrderState(
                o.OrderId,
                o.ClientOrderId,
                o.Symbol,
                o.Side,
                o.Type,
                o.Size,
                o.Price,
                o.FilledSize,
                o.Status,
                o.UpdatedAt))
            .ToList();

        return Task.FromResult(new QueryOrdersResult(_mode, results));
    }

    public Task<QueryPositionsResult> QueryPositionsAsync(CancellationToken cancellationToken)
    {
        var positions = _positions.Values
            .OrderBy(p => p.Symbol)
            .ToList();
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
            return _orders.Values.FirstOrDefault(o => o.ClientOrderId == request.ClientOrderId);
        }

        return null;
    }

    private void LoadState(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var json = File.ReadAllText(path);
        var state = JsonSerializer.Deserialize<PaperState>(json);
        if (state is null)
        {
            throw new InvalidOperationException("paper state file is invalid");
        }

        _orderSequence = state.OrderSequence;
        _clientOrderSequence = state.ClientOrderSequence;
        foreach (var order in state.Orders)
        {
            _orders[order.OrderId] = order;
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

        var directory = Path.GetDirectoryName(_statePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var state = new PaperState(
            _orderSequence,
            _clientOrderSequence,
            _orders.Values.OrderBy(o => o.OrderId).ToList(),
            _positions.Values.OrderBy(p => p.Symbol).ToList());

        var json = JsonSerializer.Serialize(state);
        File.WriteAllText(_statePath, json);
    }

    private sealed record PaperOrder(
        string OrderId,
        string ClientOrderId,
        string Symbol,
        OrderSide Side,
        OrderType Type,
        decimal Size,
        decimal? Price,
        decimal FilledSize,
        OrderStatus Status,
        DateTimeOffset UpdatedAt);

    private sealed record PaperState(
        long OrderSequence,
        long ClientOrderSequence,
        List<PaperOrder> Orders,
        List<PositionState> Positions);
}
