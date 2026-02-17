namespace Connector.Core.Contracts;

/// <summary>
/// Unified WebSocket subscription channels.
/// Public channels require no authentication.
/// Private channels (UserOrders, Fills, Positions, Balances) require auth.
/// </summary>
public enum UnifiedWsChannel
{
    Trades,
    OrderBookL1,
    OrderBookL2,
    Candles,
    UserOrders,
    Fills,
    Positions,
    Balances
}
