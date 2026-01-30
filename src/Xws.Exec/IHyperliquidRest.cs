namespace Xws.Exec;

public interface IHyperliquidRest
{
    Task<object?> PlaceAsync(PlaceOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken);
    Task<object?> CancelAsync(CancelOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken);
    Task<IReadOnlyList<object>> GetOpenOrdersAsync(ExecutionConfig config, CancellationToken cancellationToken);
    Task<object?> CancelManyAsync(IReadOnlyList<string> orderIds, ExecutionConfig config, CancellationToken cancellationToken);
}
