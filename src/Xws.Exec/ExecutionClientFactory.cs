namespace Xws.Exec;

public static class ExecutionClientFactory
{
    public static IExecutionClient Create(ExecutionConfig config, IHyperliquidRest? rest = null)
    {
        if (config.Mode == ExecutionMode.Paper)
        {
            return new PaperExecutionClient(config.Mode, config.PaperStatePath);
        }

        if (rest is null)
        {
            throw new ArgumentNullException(nameof(rest));
        }

        return new HyperliquidExecutionClient(config, rest);
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
            return new OkxExecutionClient(config, okxRest);
        }

        if (string.Equals(exchange, "bybit", StringComparison.OrdinalIgnoreCase))
        {
            return new BybitExecutionClient(config, bybitRest);
        }

        if (string.Equals(exchange, "mexc", StringComparison.OrdinalIgnoreCase))
        {
            return new MexcExecutionClient(config);
        }

        return Create(config, hyperliquidRest);
    }
}
