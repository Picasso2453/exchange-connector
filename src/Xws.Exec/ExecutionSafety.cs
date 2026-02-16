namespace Xws.Exec;

/// <summary>
/// Safety guards for execution operations.
/// </summary>
public static class ExecutionSafety
{
    /// <summary>
    /// Validates that mainnet execution has been explicitly armed.
    /// </summary>
    /// <param name="config">Execution configuration.</param>
    /// <returns>Tuple with Ok flag and error message when invalid.</returns>
    public static (bool Ok, string? Error) ValidateArming(ExecutionConfig config)
    {
        if (config.Mode != ExecutionMode.Mainnet)
        {
            return (true, null);
        }

        if (!config.ArmLiveFlag)
        {
            return (false, "Live trading is blocked. Mainnet requires explicit arming. Re-run with --arm-live to confirm live trading.");
        }

        if (!string.Equals(config.ArmEnvValue, "1", StringComparison.Ordinal))
        {
            return (false, "Live trading is blocked. Mainnet requires XWS_EXEC_ARM=1. Set XWS_EXEC_ARM=1 and re-run.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates idempotency requirements for mainnet order placement.
    /// </summary>
    /// <param name="config">Execution configuration.</param>
    /// <param name="request">Order request to validate.</param>
    /// <returns>Tuple with Ok flag and error message when invalid.</returns>
    public static (bool Ok, string? Error) ValidateIdempotency(ExecutionConfig config, PlaceOrderRequest request)
    {
        if (config.Mode != ExecutionMode.Mainnet)
        {
            return (true, null);
        }

        if (string.IsNullOrWhiteSpace(request.ClientOrderId))
        {
            return (false, "Idempotency check failed. Mainnet requires a client order id. Provide --client-order-id.");
        }

        return (true, null);
    }
}
