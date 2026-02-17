namespace Connector.Core.Contracts;

/// <summary>
/// Unified REST operations.
/// </summary>
public enum UnifiedRestOperation
{
    PlaceOrder,
    CancelOrder,
    GetBalances,
    GetPositions,
    GetOpenOrders,
    GetFills,
    GetCandles
}

/// <summary>
/// Base for all unified REST requests. TResponse is the typed response model.
/// </summary>
public abstract class UnifiedRestRequest<TResponse>
{
    public required UnifiedExchange Exchange { get; init; }
    public UnifiedRestOperation Operation { get; init; }
    public bool AuthRequired { get; init; }
    public Dictionary<string, string>? Params { get; init; }
}

// --- Request types ---

public sealed class PlaceOrderRequest : UnifiedRestRequest<PlaceOrderResponse>
{
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required string OrderType { get; init; }
    public required decimal Size { get; init; }
    public decimal? Price { get; init; }
    public string? ClientOrderId { get; init; }
    public bool ReduceOnly { get; init; }

    public PlaceOrderRequest()
    {
        Operation = UnifiedRestOperation.PlaceOrder;
        AuthRequired = true;
    }
}

public sealed class CancelOrderRequest : UnifiedRestRequest<CancelOrderResponse>
{
    public required string Symbol { get; init; }
    public required string OrderId { get; init; }

    public CancelOrderRequest()
    {
        Operation = UnifiedRestOperation.CancelOrder;
        AuthRequired = true;
    }
}

public sealed class GetBalancesRequest : UnifiedRestRequest<GetBalancesResponse>
{
    public GetBalancesRequest()
    {
        Operation = UnifiedRestOperation.GetBalances;
        AuthRequired = true;
    }
}

public sealed class GetPositionsRequest : UnifiedRestRequest<GetPositionsResponse>
{
    public GetPositionsRequest()
    {
        Operation = UnifiedRestOperation.GetPositions;
        AuthRequired = true;
    }
}

public sealed class GetOpenOrdersRequest : UnifiedRestRequest<GetOpenOrdersResponse>
{
    public GetOpenOrdersRequest()
    {
        Operation = UnifiedRestOperation.GetOpenOrders;
        AuthRequired = true;
    }
}

public sealed class GetFillsRequest : UnifiedRestRequest<GetFillsResponse>
{
    public required string Symbol { get; init; }
    public int? Limit { get; init; }

    public GetFillsRequest()
    {
        Operation = UnifiedRestOperation.GetFills;
        AuthRequired = true;
    }
}

public sealed class GetCandlesRequest : UnifiedRestRequest<GetCandlesResponse>
{
    public required string Symbol { get; init; }
    public required string Interval { get; init; }
    public int? Limit { get; init; }

    public GetCandlesRequest()
    {
        Operation = UnifiedRestOperation.GetCandles;
        AuthRequired = false;
    }
}

// --- Response types ---

public sealed class PlaceOrderResponse
{
    public required string OrderId { get; init; }
    public string? ClientOrderId { get; init; }
    public required string Status { get; init; }
}

public sealed class CancelOrderResponse
{
    public required string OrderId { get; init; }
    public required string Status { get; init; }
}

public sealed class GetBalancesResponse
{
    public required BalanceEntry[] Balances { get; init; }
}

public sealed class GetPositionsResponse
{
    public required PositionEntry[] Positions { get; init; }
}

public sealed class GetOpenOrdersResponse
{
    public required UserOrderEntry[] Orders { get; init; }
}

public sealed class GetFillsResponse
{
    public required FillEntry[] Fills { get; init; }
}

public sealed class GetCandlesResponse
{
    public required CandleEntry[] Candles { get; init; }
}
