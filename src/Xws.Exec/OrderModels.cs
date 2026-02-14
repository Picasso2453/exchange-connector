namespace Xws.Exec;

public enum OrderSide
{
    Buy = 0,
    Sell = 1
}

public enum OrderType
{
    Market = 0,
    Limit = 1
}

public enum OrderStatus
{
    Accepted = 0,
    Open = 1,
    Filled = 2,
    Cancelled = 3,
    Rejected = 4
}

public enum OrderQueryStatus
{
    Open = 0,
    Closed = 1,
    All = 2
}

public sealed record PlaceOrderRequest(
    string Symbol,
    OrderSide Side,
    OrderType Type,
    decimal Size,
    decimal? Price = null,
    string? ClientOrderId = null,
    bool ReduceOnly = false);

public sealed record PlaceOrderResult(
    OrderStatus Status,
    string? OrderId,
    string? ClientOrderId,
    ExecutionMode Mode,
    object? Raw = null);

public sealed record CancelOrderRequest(
    string? OrderId = null,
    string? ClientOrderId = null,
    string? Symbol = null);

public sealed record CancelOrderResult(
    bool Success,
    string? OrderId,
    string? ClientOrderId,
    ExecutionMode Mode,
    object? Raw = null);

public sealed record CancelAllRequest(
    string? Symbol = null);

public sealed record CancelAllResult(
    bool Success,
    int CancelledCount,
    ExecutionMode Mode,
    object? Raw = null);

public sealed record AmendOrderRequest(
    string? OrderId = null,
    string? ClientOrderId = null,
    decimal? Price = null,
    decimal? Size = null);

public sealed record AmendOrderResult(
    OrderStatus Status,
    string? OrderId,
    string? ClientOrderId,
    ExecutionMode Mode,
    object? Raw = null);

public sealed record QueryOrdersRequest(
    OrderQueryStatus Status,
    string? OrderId = null,
    string? Symbol = null);

public sealed record OrderState(
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

public sealed record QueryOrdersResult(
    ExecutionMode Mode,
    IReadOnlyList<OrderState> Orders,
    object? Raw = null);

public sealed record PositionState(
    string Symbol,
    decimal Size,
    decimal AvgEntryPrice,
    decimal MarkPrice,
    decimal UnrealizedPnl);

public sealed record QueryPositionsResult(
    ExecutionMode Mode,
    IReadOnlyList<PositionState> Positions,
    object? Raw = null);
