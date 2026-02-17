using Connector.Core.Contracts;

namespace Connector.Core.Abstractions;

/// <summary>
/// Factory for exchange-specific translators.
/// Each exchange implements this to register its capabilities.
/// </summary>
public interface IExchangeAdapter
{
    UnifiedExchange ExchangeId { get; }
    IWsTranslator CreateWsTranslator();
    IRestTranslator CreateRestTranslator();
}
