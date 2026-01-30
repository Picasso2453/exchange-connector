namespace Xws.Exec;

public interface IHyperliquidRest
{
    Task<HyperliquidPlaceResult> PlaceOrderAsync(PlaceOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken);
    Task<HyperliquidCancelResult> CancelOrderAsync(string orderId, string symbol, ExecutionConfig config, CancellationToken cancellationToken);
    Task<IReadOnlyList<HyperliquidOpenOrder>> GetOpenOrdersAsync(string address, ExecutionConfig config, CancellationToken cancellationToken);
    Task<HyperliquidCancelResult> CancelManyAsync(IReadOnlyList<string> orderIds, ExecutionConfig config, CancellationToken cancellationToken);
}

public sealed record HyperliquidPlaceResult(string? OrderId, string? Status, object? Raw = null);
public sealed record HyperliquidCancelResult(string? Status, object? Raw = null);
public sealed record HyperliquidOpenOrder(string OrderId, string? ClientOrderId, string? Symbol);
