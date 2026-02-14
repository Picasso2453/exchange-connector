namespace Xws.Exec;

public sealed class MexcExecutionClient : IExecutionClient
{
    private readonly ExecutionConfig _config;
    private readonly PaperExecutionClient _paper;

    public MexcExecutionClient(ExecutionConfig config)
    {
        _config = config;
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
            "mexc execution not implemented"));
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
            "mexc execution not implemented"));
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
            "mexc execution not implemented"));
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
            "mexc amend not implemented"));
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
            "mexc query orders not implemented"));
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
            "mexc query positions not implemented"));
    }
}
