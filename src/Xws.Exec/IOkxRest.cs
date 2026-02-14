namespace Xws.Exec;

public interface IOkxRest
{
    Task<object?> PlaceOrderAsync(PlaceOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken);
    Task<object?> CancelOrderAsync(string orderId, ExecutionConfig config, CancellationToken cancellationToken);
    Task<object?> CancelAllAsync(CancelAllRequest request, ExecutionConfig config, CancellationToken cancellationToken);
    Task<object?> AmendOrderAsync(AmendOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken);
    Task<object?> QueryOrdersAsync(QueryOrdersRequest request, ExecutionConfig config, CancellationToken cancellationToken);
    Task<object?> QueryPositionsAsync(ExecutionConfig config, CancellationToken cancellationToken);
}
