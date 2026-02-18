namespace Connector.Core.Contracts;

/// <summary>
/// Unified WebSocket subscription channels.
/// Public channels require no authentication.
/// Private channels require auth (user address or API key).
/// </summary>
public enum UnifiedWsChannel
{
    // --- Public ---
    Trades,
    OrderBookL1,
    OrderBookL2,
    Candles,
    AllMids,
    ActiveAssetCtx,

    // --- Private ---
    UserOrders,
    Fills,
    Positions,
    Balances,
    UserFundings,
    Ledger,
    Notifications,
    OpenOrders,

    // --- TWAP ---
    TwapState,
    TwapSliceFills,
    TwapHistory,

    // --- Composite ---
    ActiveAssetData,
    WebData
}
