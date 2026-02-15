namespace Xws.Exec;

public static class ExecutionClientFactory
{
    public static IExecutionClient Create(ExecutionConfig config, IHyperliquidRest? rest = null)
    {
        if (config.Mode == ExecutionMode.Paper)
        {
            return new PaperExecutionClient(config.Mode, config.PaperStatePath);
        }

        var resolved = rest ?? new HyperliquidRest();
        var limiter = RateLimiterFactory.CreateHyperliquid();
        var throttled = new RateLimitedHyperliquidRest(resolved, limiter);
        return new HLExecutionClient(config, throttled);
    }

    public static IExecutionClient Create(
        ExecutionConfig config,
        string exchange,
        IHyperliquidRest? hyperliquidRest = null,
        IOkxRest? okxRest = null,
        IBybitRest? bybitRest = null)
    {
        if (string.Equals(exchange, "okx", StringComparison.OrdinalIgnoreCase))
        {
            var throttled = okxRest is null
                ? null
                : new RateLimitedOkxRest(okxRest, RateLimiterFactory.CreateOkx());
            return new OkxExecutionClient(config, throttled);
        }

        if (string.Equals(exchange, "bybit", StringComparison.OrdinalIgnoreCase))
        {
            var throttled = bybitRest is null
                ? null
                : new RateLimitedBybitRest(bybitRest, RateLimiterFactory.CreateBybit());
            return new BybitExecutionClient(config, throttled);
        }

        if (string.Equals(exchange, "mexc", StringComparison.OrdinalIgnoreCase))
        {
            return new MexcExecutionClient(config);
        }

        return Create(config, hyperliquidRest);
    }
}
