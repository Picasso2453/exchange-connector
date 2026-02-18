namespace Connector.Core.Contracts;

/// <summary>
/// Unified REST operations.
/// </summary>
public enum UnifiedRestOperation
{
    // --- Trading ---
    PlaceOrder,
    CancelOrder,
    CancelOrderByClientId,
    ModifyOrder,
    BatchModifyOrders,
    PlaceTwapOrder,
    CancelTwapOrder,
    ScheduleCancel,

    // --- Account ---
    UpdateLeverage,
    UpdateIsolatedMargin,
    Transfer,
    ApproveAgent,

    // --- Info: Market Data ---
    GetCandles,
    GetL2Book,
    GetAllMids,
    GetMeta,
    GetFundingHistory,
    GetPredictedFundings,

    // --- Info: User Data ---
    GetBalances,
    GetPositions,
    GetOpenOrders,
    GetFrontendOpenOrders,
    GetFills,
    GetFillsByTime,
    GetOrderStatus,
    GetHistoricalOrders,
    GetUserFunding,
    GetUserRateLimit,
    GetUserFees,
    GetSubAccounts,
    GetActiveAssetData,
    GetPortfolio,

    // --- Info: Spot ---
    GetSpotMeta,
    GetSpotBalances
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

// ════════════════════════════════════════════════
// Trading Requests
// ════════════════════════════════════════════════

public sealed class PlaceOrderRequest : UnifiedRestRequest<PlaceOrderResponse>
{
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required string OrderType { get; init; }
    public required decimal Size { get; init; }
    public decimal? Price { get; init; }
    public string? ClientOrderId { get; init; }
    public bool ReduceOnly { get; init; }
    public string? TimeInForce { get; init; }
    public decimal? TriggerPrice { get; init; }
    public string? TriggerType { get; init; }

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

public sealed class CancelOrderByClientIdRequest : UnifiedRestRequest<CancelOrderResponse>
{
    public required string Symbol { get; init; }
    public required string ClientOrderId { get; init; }

    public CancelOrderByClientIdRequest()
    {
        Operation = UnifiedRestOperation.CancelOrderByClientId;
        AuthRequired = true;
    }
}

public sealed class ModifyOrderRequest : UnifiedRestRequest<PlaceOrderResponse>
{
    public required string Symbol { get; init; }
    public required string OrderId { get; init; }
    public required string Side { get; init; }
    public required decimal Size { get; init; }
    public required decimal Price { get; init; }
    public string? OrderType { get; init; }
    public bool ReduceOnly { get; init; }
    public string? ClientOrderId { get; init; }

    public ModifyOrderRequest()
    {
        Operation = UnifiedRestOperation.ModifyOrder;
        AuthRequired = true;
    }
}

public sealed class BatchModifyOrdersRequest : UnifiedRestRequest<BatchModifyOrdersResponse>
{
    public required ModifyOrderRequest[] Modifications { get; init; }

    public BatchModifyOrdersRequest()
    {
        Operation = UnifiedRestOperation.BatchModifyOrders;
        AuthRequired = true;
    }
}

public sealed class PlaceTwapOrderRequest : UnifiedRestRequest<PlaceTwapOrderResponse>
{
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required decimal Size { get; init; }
    public required int DurationMinutes { get; init; }
    public bool ReduceOnly { get; init; }
    public bool Randomize { get; init; }

    public PlaceTwapOrderRequest()
    {
        Operation = UnifiedRestOperation.PlaceTwapOrder;
        AuthRequired = true;
    }
}

public sealed class CancelTwapOrderRequest : UnifiedRestRequest<CancelOrderResponse>
{
    public required string Symbol { get; init; }
    public required string TwapId { get; init; }

    public CancelTwapOrderRequest()
    {
        Operation = UnifiedRestOperation.CancelTwapOrder;
        AuthRequired = true;
    }
}

public sealed class ScheduleCancelRequest : UnifiedRestRequest<ScheduleCancelResponse>
{
    public long? CancelTime { get; init; }

    public ScheduleCancelRequest()
    {
        Operation = UnifiedRestOperation.ScheduleCancel;
        AuthRequired = true;
    }
}

// ════════════════════════════════════════════════
// Account Requests
// ════════════════════════════════════════════════

public sealed class UpdateLeverageRequest : UnifiedRestRequest<UpdateLeverageResponse>
{
    public required string Symbol { get; init; }
    public required int Leverage { get; init; }
    public required bool IsCross { get; init; }

    public UpdateLeverageRequest()
    {
        Operation = UnifiedRestOperation.UpdateLeverage;
        AuthRequired = true;
    }
}

public sealed class UpdateIsolatedMarginRequest : UnifiedRestRequest<UpdateIsolatedMarginResponse>
{
    public required string Symbol { get; init; }
    public required bool IsBuy { get; init; }
    public required decimal Amount { get; init; }

    public UpdateIsolatedMarginRequest()
    {
        Operation = UnifiedRestOperation.UpdateIsolatedMargin;
        AuthRequired = true;
    }
}

public sealed class TransferRequest : UnifiedRestRequest<TransferResponse>
{
    public required decimal Amount { get; init; }
    public required bool ToPerp { get; init; }

    public TransferRequest()
    {
        Operation = UnifiedRestOperation.Transfer;
        AuthRequired = true;
    }
}

public sealed class ApproveAgentRequest : UnifiedRestRequest<ApproveAgentResponse>
{
    public required string AgentAddress { get; init; }
    public string? AgentName { get; init; }

    public ApproveAgentRequest()
    {
        Operation = UnifiedRestOperation.ApproveAgent;
        AuthRequired = true;
    }
}

// ════════════════════════════════════════════════
// Info: Market Data Requests
// ════════════════════════════════════════════════

public sealed class GetCandlesRequest : UnifiedRestRequest<GetCandlesResponse>
{
    public required string Symbol { get; init; }
    public required string Interval { get; init; }
    public long? StartTime { get; init; }
    public long? EndTime { get; init; }

    public GetCandlesRequest()
    {
        Operation = UnifiedRestOperation.GetCandles;
        AuthRequired = false;
    }
}

public sealed class GetL2BookRequest : UnifiedRestRequest<GetL2BookResponse>
{
    public required string Symbol { get; init; }
    public int? SignificantFigures { get; init; }
    public int? Mantissa { get; init; }

    public GetL2BookRequest()
    {
        Operation = UnifiedRestOperation.GetL2Book;
        AuthRequired = false;
    }
}

public sealed class GetAllMidsRequest : UnifiedRestRequest<GetAllMidsResponse>
{
    public GetAllMidsRequest()
    {
        Operation = UnifiedRestOperation.GetAllMids;
        AuthRequired = false;
    }
}

public sealed class GetMetaRequest : UnifiedRestRequest<GetMetaResponse>
{
    public bool IncludeAssetCtxs { get; init; }

    public GetMetaRequest()
    {
        Operation = UnifiedRestOperation.GetMeta;
        AuthRequired = false;
    }
}

public sealed class GetFundingHistoryRequest : UnifiedRestRequest<GetFundingHistoryResponse>
{
    public required string Symbol { get; init; }
    public required long StartTime { get; init; }
    public long? EndTime { get; init; }

    public GetFundingHistoryRequest()
    {
        Operation = UnifiedRestOperation.GetFundingHistory;
        AuthRequired = false;
    }
}

public sealed class GetPredictedFundingsRequest : UnifiedRestRequest<GetPredictedFundingsResponse>
{
    public GetPredictedFundingsRequest()
    {
        Operation = UnifiedRestOperation.GetPredictedFundings;
        AuthRequired = false;
    }
}

// ════════════════════════════════════════════════
// Info: User Data Requests
// ════════════════════════════════════════════════

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

public sealed class GetFrontendOpenOrdersRequest : UnifiedRestRequest<GetFrontendOpenOrdersResponse>
{
    public GetFrontendOpenOrdersRequest()
    {
        Operation = UnifiedRestOperation.GetFrontendOpenOrders;
        AuthRequired = true;
    }
}

public sealed class GetFillsRequest : UnifiedRestRequest<GetFillsResponse>
{
    public string? Symbol { get; init; }

    public GetFillsRequest()
    {
        Operation = UnifiedRestOperation.GetFills;
        AuthRequired = true;
    }
}

public sealed class GetFillsByTimeRequest : UnifiedRestRequest<GetFillsResponse>
{
    public required long StartTime { get; init; }
    public long? EndTime { get; init; }
    public bool AggregateByTime { get; init; }

    public GetFillsByTimeRequest()
    {
        Operation = UnifiedRestOperation.GetFillsByTime;
        AuthRequired = true;
    }
}

public sealed class GetOrderStatusRequest : UnifiedRestRequest<GetOrderStatusResponse>
{
    public required string OrderId { get; init; }

    public GetOrderStatusRequest()
    {
        Operation = UnifiedRestOperation.GetOrderStatus;
        AuthRequired = true;
    }
}

public sealed class GetHistoricalOrdersRequest : UnifiedRestRequest<GetHistoricalOrdersResponse>
{
    public GetHistoricalOrdersRequest()
    {
        Operation = UnifiedRestOperation.GetHistoricalOrders;
        AuthRequired = true;
    }
}

public sealed class GetUserFundingRequest : UnifiedRestRequest<GetUserFundingResponse>
{
    public required long StartTime { get; init; }
    public long? EndTime { get; init; }

    public GetUserFundingRequest()
    {
        Operation = UnifiedRestOperation.GetUserFunding;
        AuthRequired = true;
    }
}

public sealed class GetUserRateLimitRequest : UnifiedRestRequest<GetUserRateLimitResponse>
{
    public GetUserRateLimitRequest()
    {
        Operation = UnifiedRestOperation.GetUserRateLimit;
        AuthRequired = true;
    }
}

public sealed class GetUserFeesRequest : UnifiedRestRequest<GetUserFeesResponse>
{
    public GetUserFeesRequest()
    {
        Operation = UnifiedRestOperation.GetUserFees;
        AuthRequired = true;
    }
}

public sealed class GetSubAccountsRequest : UnifiedRestRequest<GetSubAccountsResponse>
{
    public GetSubAccountsRequest()
    {
        Operation = UnifiedRestOperation.GetSubAccounts;
        AuthRequired = true;
    }
}

public sealed class GetActiveAssetDataRequest : UnifiedRestRequest<GetActiveAssetDataResponse>
{
    public required string Symbol { get; init; }

    public GetActiveAssetDataRequest()
    {
        Operation = UnifiedRestOperation.GetActiveAssetData;
        AuthRequired = true;
    }
}

public sealed class GetPortfolioRequest : UnifiedRestRequest<GetPortfolioResponse>
{
    public GetPortfolioRequest()
    {
        Operation = UnifiedRestOperation.GetPortfolio;
        AuthRequired = true;
    }
}

// ════════════════════════════════════════════════
// Info: Spot Requests
// ════════════════════════════════════════════════

public sealed class GetSpotMetaRequest : UnifiedRestRequest<GetSpotMetaResponse>
{
    public bool IncludeAssetCtxs { get; init; }

    public GetSpotMetaRequest()
    {
        Operation = UnifiedRestOperation.GetSpotMeta;
        AuthRequired = false;
    }
}

public sealed class GetSpotBalancesRequest : UnifiedRestRequest<GetSpotBalancesResponse>
{
    public GetSpotBalancesRequest()
    {
        Operation = UnifiedRestOperation.GetSpotBalances;
        AuthRequired = true;
    }
}

// ════════════════════════════════════════════════
// Response types
// ════════════════════════════════════════════════

public sealed class PlaceOrderResponse
{
    public required string OrderId { get; init; }
    public string? ClientOrderId { get; init; }
    public required string Status { get; init; }
    public string? FilledStatus { get; init; }
}

public sealed class CancelOrderResponse
{
    public required string OrderId { get; init; }
    public required string Status { get; init; }
}

public sealed class BatchModifyOrdersResponse
{
    public required PlaceOrderResponse[] Results { get; init; }
}

public sealed class PlaceTwapOrderResponse
{
    public required string TwapId { get; init; }
    public required string Status { get; init; }
}

public sealed class ScheduleCancelResponse
{
    public required string Status { get; init; }
}

public sealed class UpdateLeverageResponse
{
    public required string Status { get; init; }
}

public sealed class UpdateIsolatedMarginResponse
{
    public required string Status { get; init; }
}

public sealed class TransferResponse
{
    public required string Status { get; init; }
}

public sealed class ApproveAgentResponse
{
    public required string Status { get; init; }
}

public sealed class GetBalancesResponse
{
    public required BalanceEntry[] Balances { get; init; }
    public decimal? AccountValue { get; init; }
    public decimal? Withdrawable { get; init; }
}

public sealed class GetPositionsResponse
{
    public required PositionEntry[] Positions { get; init; }
}

public sealed class GetOpenOrdersResponse
{
    public required UserOrderEntry[] Orders { get; init; }
}

public sealed class GetFrontendOpenOrdersResponse
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

public sealed class GetL2BookResponse
{
    public required string Symbol { get; init; }
    public required PriceLevel[] Bids { get; init; }
    public required PriceLevel[] Asks { get; init; }
}

public sealed class MidPrice
{
    public required string Symbol { get; init; }
    public required decimal Mid { get; init; }
}

public sealed class GetAllMidsResponse
{
    public required MidPrice[] Mids { get; init; }
}

public sealed class AssetMeta
{
    public required string Name { get; init; }
    public required int SzDecimals { get; init; }
    public decimal? MaxLeverage { get; init; }
}

public sealed class AssetContext
{
    public required string Symbol { get; init; }
    public required decimal MarkPrice { get; init; }
    public required decimal FundingRate { get; init; }
    public required decimal OpenInterest { get; init; }
    public decimal? PrevDayPrice { get; init; }
    public decimal? DayNotionalVolume { get; init; }
}

public sealed class GetMetaResponse
{
    public required AssetMeta[] Assets { get; init; }
    public AssetContext[]? AssetContexts { get; init; }
}

public sealed class FundingHistoryEntry
{
    public required string Symbol { get; init; }
    public required decimal FundingRate { get; init; }
    public required decimal Premium { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed class GetFundingHistoryResponse
{
    public required FundingHistoryEntry[] History { get; init; }
}

public sealed class PredictedFundingEntry
{
    public required string Symbol { get; init; }
    public required string Venue { get; init; }
    public required decimal FundingRate { get; init; }
    public DateTimeOffset? NextFundingTime { get; init; }
}

public sealed class GetPredictedFundingsResponse
{
    public required PredictedFundingEntry[] Predictions { get; init; }
}

public sealed class GetOrderStatusResponse
{
    public required string Status { get; init; }
    public UserOrderEntry? Order { get; init; }
}

public sealed class GetHistoricalOrdersResponse
{
    public required UserOrderEntry[] Orders { get; init; }
}

public sealed class UserFundingEntry
{
    public required string Symbol { get; init; }
    public required decimal FundingRate { get; init; }
    public required decimal Payment { get; init; }
    public required decimal PositionSize { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? Hash { get; init; }
}

public sealed class GetUserFundingResponse
{
    public required UserFundingEntry[] Fundings { get; init; }
}

public sealed class GetUserRateLimitResponse
{
    public required decimal CumulativeVolume { get; init; }
    public required int RequestsUsed { get; init; }
    public required int RequestsCap { get; init; }
}

public sealed class GetUserFeesResponse
{
    public required decimal DailyVolume { get; init; }
    public required decimal MakerRate { get; init; }
    public required decimal TakerRate { get; init; }
}

public sealed class SubAccountEntry
{
    public required string Name { get; init; }
    public required string Address { get; init; }
}

public sealed class GetSubAccountsResponse
{
    public required SubAccountEntry[] SubAccounts { get; init; }
}

public sealed class GetActiveAssetDataResponse
{
    public required decimal Leverage { get; init; }
    public decimal? MaxTradeSizeLong { get; init; }
    public decimal? MaxTradeSizeShort { get; init; }
    public required decimal MarkPrice { get; init; }
}

public sealed class PortfolioTimeseries
{
    public required decimal[] AccountValues { get; init; }
    public required decimal[] PnlValues { get; init; }
    public required decimal Volume { get; init; }
}

public sealed class GetPortfolioResponse
{
    public PortfolioTimeseries? Day { get; init; }
    public PortfolioTimeseries? Week { get; init; }
    public PortfolioTimeseries? Month { get; init; }
    public PortfolioTimeseries? AllTime { get; init; }
}

public sealed class SpotTokenMeta
{
    public required string Name { get; init; }
    public required int TokenId { get; init; }
    public required int Decimals { get; init; }
}

public sealed class SpotAssetContext
{
    public required string Symbol { get; init; }
    public required decimal MarkPrice { get; init; }
    public decimal? PrevDayPrice { get; init; }
    public decimal? DayNotionalVolume { get; init; }
}

public sealed class GetSpotMetaResponse
{
    public required SpotTokenMeta[] Tokens { get; init; }
    public SpotAssetContext[]? AssetContexts { get; init; }
}

public sealed class SpotBalanceEntry
{
    public required string Asset { get; init; }
    public required int TokenId { get; init; }
    public required decimal Total { get; init; }
    public required decimal Hold { get; init; }
    public decimal? EntryNotional { get; init; }
}

public sealed class GetSpotBalancesResponse
{
    public required SpotBalanceEntry[] Balances { get; init; }
}
