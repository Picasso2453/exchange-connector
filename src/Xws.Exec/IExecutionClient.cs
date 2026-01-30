namespace Xws.Exec;

public interface IExecutionClient
{
    Task<PlaceOrderResult> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken);
    Task<CancelOrderResult> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken);
    Task<CancelAllResult> CancelAllAsync(CancelAllRequest request, CancellationToken cancellationToken);
}
