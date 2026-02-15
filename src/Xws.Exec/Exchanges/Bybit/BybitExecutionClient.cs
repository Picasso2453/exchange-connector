namespace Xws.Exec;

public sealed class BybitExecutionClient : IExecutionClient
{
    private readonly ExecutionConfig _config;
    private readonly IBybitRest? _rest;
    private readonly PaperExecutionClient _paper;

    public BybitExecutionClient(ExecutionConfig config, IBybitRest? rest = null)
    {
        _config = config;
        _rest = rest;
        _paper = new PaperExecutionClient(config.Mode, config.PaperStatePath);
    }

    public Task<PlaceOrderResult> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        if (_config.Mode == ExecutionMode.Paper)
        {
            return _paper.PlaceAsync(request, cancellationToken);
        }

        return Task.FromResult(new PlaceOrderResult(
            OrderStatus.Rejected,
            null,
            request.ClientOrderId,
            _config.Mode,
            "bybit execution not implemented"));
    }

    public Task<CancelOrderResult> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken)
    {
        if (_config.Mode == ExecutionMode.Paper)
        {
            return _paper.CancelAsync(request, cancellationToken);
        }

        return Task.FromResult(new CancelOrderResult(
            false,
            request.OrderId,
            request.ClientOrderId,
            _config.Mode,
            "bybit execution not implemented"));
    }

    public Task<CancelAllResult> CancelAllAsync(CancelAllRequest request, CancellationToken cancellationToken)
    {
        if (_config.Mode == ExecutionMode.Paper)
        {
            return _paper.CancelAllAsync(request, cancellationToken);
        }

        return Task.FromResult(new CancelAllResult(
            false,
            0,
            _config.Mode,
            "bybit execution not implemented"));
    }

    public Task<AmendOrderResult> AmendAsync(AmendOrderRequest request, CancellationToken cancellationToken)
    {
        if (_config.Mode == ExecutionMode.Paper)
        {
            return _paper.AmendAsync(request, cancellationToken);
        }

        return Task.FromResult(new AmendOrderResult(
            OrderStatus.Rejected,
            request.OrderId,
            request.ClientOrderId,
            _config.Mode,
            "bybit amend not implemented"));
    }

    public Task<QueryOrdersResult> QueryOrdersAsync(QueryOrdersRequest request, CancellationToken cancellationToken)
    {
        if (_config.Mode == ExecutionMode.Paper)
        {
            return _paper.QueryOrdersAsync(request, cancellationToken);
        }

        return Task.FromResult(new QueryOrdersResult(
            _config.Mode,
            Array.Empty<OrderState>(),
            "bybit query orders not implemented"));
    }

    public Task<QueryPositionsResult> QueryPositionsAsync(CancellationToken cancellationToken)
    {
        if (_config.Mode == ExecutionMode.Paper)
        {
            return _paper.QueryPositionsAsync(cancellationToken);
        }

        return Task.FromResult(new QueryPositionsResult(
            _config.Mode,
            Array.Empty<PositionState>(),
            "bybit query positions not implemented"));
    }
}
