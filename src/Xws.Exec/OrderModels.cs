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
    string? ClientOrderId = null);

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
