using System.Text.Json;

namespace Connector.Core.Contracts;

/// <summary>
/// Base for all unified WebSocket events emitted to stdout.
/// </summary>
public abstract class UnifiedWsEvent
{
    public required UnifiedExchange Exchange { get; init; }
    public required UnifiedWsChannel Channel { get; init; }
    public required string Symbol { get; init; }
    public long? Sequence { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }
    public RawPayload? Raw { get; init; }
}

public sealed class TradeEntry
{
    public required string TradeId { get; init; }
    public required decimal Price { get; init; }
    public required decimal Size { get; init; }
    public required string Side { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed class TradesEvent : UnifiedWsEvent
{
    public required TradeEntry[] Trades { get; init; }
}

public sealed class PriceLevel
{
    public required decimal Price { get; init; }
    public required decimal Size { get; init; }
}

public sealed class OrderBookL1Event : UnifiedWsEvent
{
    public required PriceLevel BestBid { get; init; }
    public required PriceLevel BestAsk { get; init; }
}

public sealed class OrderBookL2Event : UnifiedWsEvent
{
    public required PriceLevel[] Bids { get; init; }
    public required PriceLevel[] Asks { get; init; }
    public bool IsSnapshot { get; init; }
}

public sealed class CandleEntry
{
    public required DateTimeOffset OpenTime { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required decimal Volume { get; init; }
}

public sealed class CandleEvent : UnifiedWsEvent
{
    public required CandleEntry Candle { get; init; }
}

public sealed class UserOrderEntry
{
    public required string OrderId { get; init; }
    public string? ClientOrderId { get; init; }
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required string OrderType { get; init; }
    public required decimal Price { get; init; }
    public required decimal Size { get; init; }
    public required decimal FilledSize { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed class UserOrderEvent : UnifiedWsEvent
{
    public required UserOrderEntry[] Orders { get; init; }
}

public sealed class FillEntry
{
    public required string TradeId { get; init; }
    public required string OrderId { get; init; }
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required decimal Price { get; init; }
    public required decimal Size { get; init; }
    public required decimal Fee { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed class FillEvent : UnifiedWsEvent
{
    public required FillEntry[] Fills { get; init; }
}

public sealed class PositionEntry
{
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required decimal Size { get; init; }
    public required decimal EntryPrice { get; init; }
    public required decimal UnrealizedPnl { get; init; }
    public decimal? LiquidationPrice { get; init; }
    public decimal? Leverage { get; init; }
}

public sealed class PositionEvent : UnifiedWsEvent
{
    public required PositionEntry[] Positions { get; init; }
}

public sealed class BalanceEntry
{
    public required string Asset { get; init; }
    public required decimal Total { get; init; }
    public required decimal Available { get; init; }
}

public sealed class BalanceEvent : UnifiedWsEvent
{
    public required BalanceEntry[] Balances { get; init; }
}
