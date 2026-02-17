using Connector.Core.Abstractions;
using Connector.Core.Contracts;

namespace Connector.Core.Exchanges;

/// <summary>
/// Registry mapping exchange IDs to adapter factories.
/// </summary>
public sealed class ExchangeRegistry
{
    private readonly Dictionary<UnifiedExchange, IExchangeAdapter> _adapters = new();

    public void Register(IExchangeAdapter adapter)
    {
        _adapters[adapter.ExchangeId] = adapter;
    }

    public IExchangeAdapter Get(UnifiedExchange exchange)
    {
        if (!_adapters.TryGetValue(exchange, out var adapter))
            throw new InvalidOperationException($"No adapter registered for {exchange}");
        return adapter;
    }

    public IEnumerable<UnifiedExchange> RegisteredExchanges => _adapters.Keys;
}
