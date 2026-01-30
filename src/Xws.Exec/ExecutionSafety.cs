namespace Xws.Exec;

public static class ExecutionSafety
{
    public static (bool Ok, string? Error) ValidateArming(ExecutionConfig config)
    {
        if (config.Mode != ExecutionMode.Mainnet)
        {
            return (true, null);
        }

        if (!config.ArmLiveFlag)
        {
            return (false, "mainnet requires --arm-live");
        }

        if (!string.Equals(config.ArmEnvValue, "1", StringComparison.Ordinal))
        {
            return (false, "mainnet requires XWS_EXEC_ARM=1");
        }

        return (true, null);
    }

    public static (bool Ok, string? Error) ValidateIdempotency(ExecutionConfig config, PlaceOrderRequest request)
    {
        if (config.Mode != ExecutionMode.Mainnet)
        {
            return (true, null);
        }

        if (string.IsNullOrWhiteSpace(request.ClientOrderId))
        {
            return (false, "mainnet requires clientOrderId");
        }

        return (true, null);
    }
}
