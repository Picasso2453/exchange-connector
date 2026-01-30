namespace Xws.Exec;

public static class ExecutionClientFactory
{
    public static IExecutionClient Create(ExecutionConfig config, IHyperliquidRest? rest = null)
    {
        if (config.Mode == ExecutionMode.Paper)
        {
            return new PaperExecutionClient(config.Mode);
        }

        if (rest is null)
        {
            throw new ArgumentNullException(nameof(rest));
        }

        return new HyperliquidExecutionClient(config, rest);
    }
}
