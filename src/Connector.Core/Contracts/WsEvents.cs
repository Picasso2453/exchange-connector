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

// ──────────────────────────────────────────────
// Trades
// ──────────────────────────────────────────────

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

// ──────────────────────────────────────────────
// Order Book
// ──────────────────────────────────────────────

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

// ──────────────────────────────────────────────
// Candles
// ──────────────────────────────────────────────

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

// ──────────────────────────────────────────────
// All Mids (mid prices for all assets)
// ──────────────────────────────────────────────

public sealed class AllMidsEntry
{
    public required string Symbol { get; init; }
    public required decimal Mid { get; init; }
}

public sealed class AllMidsEvent : UnifiedWsEvent
{
    public required AllMidsEntry[] Mids { get; init; }
}

// ──────────────────────────────────────────────
// Active Asset Context (funding, markPx, OI)
// ──────────────────────────────────────────────

public sealed class ActiveAssetCtxEvent : UnifiedWsEvent
{
    public required decimal MarkPrice { get; init; }
    public required decimal FundingRate { get; init; }
    public required decimal OpenInterest { get; init; }
    public decimal? PrevDayPrice { get; init; }
    public decimal? DayBaseVolume { get; init; }
    public decimal? DayNotionalVolume { get; init; }
    public decimal? OraclePrice { get; init; }
}

// ──────────────────────────────────────────────
// User Orders (open orders live stream)
// ──────────────────────────────────────────────

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
    public bool ReduceOnly { get; init; }
    public bool IsTrigger { get; init; }
    public decimal? TriggerPrice { get; init; }
    public string? TriggerCondition { get; init; }
    public decimal? OriginalSize { get; init; }
}

public sealed class UserOrderEvent : UnifiedWsEvent
{
    public required UserOrderEntry[] Orders { get; init; }
}

// ──────────────────────────────────────────────
// Fills
// ──────────────────────────────────────────────

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
    public string? Direction { get; init; }
    public decimal? ClosedPnl { get; init; }
    public decimal? StartPosition { get; init; }
    public string? FeeToken { get; init; }
}

public sealed class FillEvent : UnifiedWsEvent
{
    public required FillEntry[] Fills { get; init; }
    public bool IsSnapshot { get; init; }
}

// ──────────────────────────────────────────────
// Positions
// ──────────────────────────────────────────────

public sealed class PositionEntry
{
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required decimal Size { get; init; }
    public required decimal EntryPrice { get; init; }
    public required decimal UnrealizedPnl { get; init; }
    public decimal? LiquidationPrice { get; init; }
    public decimal? Leverage { get; init; }
    public string? MarginType { get; init; }
    public decimal? ReturnOnEquity { get; init; }
}

public sealed class PositionEvent : UnifiedWsEvent
{
    public required PositionEntry[] Positions { get; init; }
}

// ──────────────────────────────────────────────
// Balances
// ──────────────────────────────────────────────

public sealed class BalanceEntry
{
    public required string Asset { get; init; }
    public required decimal Total { get; init; }
    public required decimal Available { get; init; }
}

public sealed class BalanceEvent : UnifiedWsEvent
{
    public required BalanceEntry[] Balances { get; init; }
    public decimal? AccountValue { get; init; }
    public decimal? TotalMarginUsed { get; init; }
    public decimal? TotalRawUsd { get; init; }
    public decimal? Withdrawable { get; init; }
}

// ──────────────────────────────────────────────
// User Fundings
// ──────────────────────────────────────────────

public sealed class FundingEntry
{
    public required string Symbol { get; init; }
    public required decimal FundingRate { get; init; }
    public required decimal Payment { get; init; }
    public required decimal PositionSize { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed class UserFundingEvent : UnifiedWsEvent
{
    public required FundingEntry[] Fundings { get; init; }
    public bool IsSnapshot { get; init; }
}

// ──────────────────────────────────────────────
// Ledger (non-funding: deposits, withdrawals, transfers, liquidations)
// ──────────────────────────────────────────────

public sealed class LedgerEntry
{
    public required string Type { get; init; }
    public required decimal Amount { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? Hash { get; init; }
    public string? Symbol { get; init; }
    public string? Fee { get; init; }
}

public sealed class LedgerEvent : UnifiedWsEvent
{
    public required LedgerEntry[] Entries { get; init; }
    public bool IsSnapshot { get; init; }
}

// ──────────────────────────────────────────────
// Notifications
// ──────────────────────────────────────────────

public sealed class NotificationEvent : UnifiedWsEvent
{
    public required string Message { get; init; }
}

// ──────────────────────────────────────────────
// Open Orders (live snapshot of all open orders)
// ──────────────────────────────────────────────

public sealed class OpenOrdersEvent : UnifiedWsEvent
{
    public required UserOrderEntry[] Orders { get; init; }
}

// ──────────────────────────────────────────────
// TWAP State
// ──────────────────────────────────────────────

public sealed class TwapStateEntry
{
    public required string TwapId { get; init; }
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required decimal Size { get; init; }
    public required decimal FilledSize { get; init; }
    public required int DurationMinutes { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class TwapStateEvent : UnifiedWsEvent
{
    public required TwapStateEntry[] Twaps { get; init; }
}

// ──────────────────────────────────────────────
// TWAP Slice Fills
// ──────────────────────────────────────────────

public sealed class TwapSliceFillEntry
{
    public required string TwapId { get; init; }
    public required string Symbol { get; init; }
    public required decimal Price { get; init; }
    public required decimal Size { get; init; }
    public required string Side { get; init; }
    public required decimal Fee { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed class TwapSliceFillEvent : UnifiedWsEvent
{
    public required TwapSliceFillEntry[] Fills { get; init; }
    public bool IsSnapshot { get; init; }
}

// ──────────────────────────────────────────────
// TWAP History
// ──────────────────────────────────────────────

public sealed class TwapHistoryEntry
{
    public required string TwapId { get; init; }
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required decimal Size { get; init; }
    public required decimal FilledSize { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class TwapHistoryEvent : UnifiedWsEvent
{
    public required TwapHistoryEntry[] History { get; init; }
}

// ──────────────────────────────────────────────
// Active Asset Data (per-user per-asset leverage/trade limits)
// ──────────────────────────────────────────────

public sealed class ActiveAssetDataEvent : UnifiedWsEvent
{
    public required decimal Leverage { get; init; }
    public decimal? MaxTradeSizeLong { get; init; }
    public decimal? MaxTradeSizeShort { get; init; }
    public decimal? AvailableToTradeLong { get; init; }
    public decimal? AvailableToTradeShort { get; init; }
    public required decimal MarkPrice { get; init; }
}

// ──────────────────────────────────────────────
// Web Data (aggregate user info - HL-specific)
// ──────────────────────────────────────────────

public sealed class WebDataEvent : UnifiedWsEvent
{
    public required string Data { get; init; }
}
