using Connector.Core.Abstractions;
using Connector.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace Connector.Core.Exchanges.Hyperliquid;

public sealed class HyperliquidAdapter : IExchangeAdapter
{
    private readonly HyperliquidConfig _config;
    private readonly ILoggerFactory _loggerFactory;
    private readonly bool _includeRaw;

    public UnifiedExchange ExchangeId => UnifiedExchange.Hyperliquid;

    public HyperliquidAdapter(HyperliquidConfig config, ILoggerFactory loggerFactory, bool includeRaw = false)
    {
        _config = config;
        _loggerFactory = loggerFactory;
        _includeRaw = includeRaw;
    }

    public IWsTranslator CreateWsTranslator()
    {
        return new HyperliquidWsTranslator(
            _loggerFactory.CreateLogger<HyperliquidWsTranslator>(),
            _includeRaw,
            _config.UserAddress);
    }

    public IRestTranslator CreateRestTranslator()
    {
        return new HyperliquidRestTranslator();
    }
}
