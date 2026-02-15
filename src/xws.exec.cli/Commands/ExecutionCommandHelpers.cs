using System.Text.Json;
using Xws.Exec;

namespace Xws.Exec.Cli;

public static class ExecutionCommandHelpers
{
    public static bool TryParseMode(string value, out ExecutionMode mode)
    {
        if (string.Equals(value, "paper", StringComparison.OrdinalIgnoreCase))
        {
            mode = ExecutionMode.Paper;
            return true;
        }

        if (string.Equals(value, "testnet", StringComparison.OrdinalIgnoreCase))
        {
            mode = ExecutionMode.Testnet;
            return true;
        }

        if (string.Equals(value, "mainnet", StringComparison.OrdinalIgnoreCase))
        {
            mode = ExecutionMode.Mainnet;
            return true;
        }

        mode = ExecutionMode.Paper;
        return false;
    }

    public static bool TryParseExchange(string value, out string exchange)
    {
        if (string.Equals(value, "hl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "okx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "bybit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "mexc", StringComparison.OrdinalIgnoreCase))
        {
            exchange = value.ToLowerInvariant();
            return true;
        }

        exchange = "hl";
        return false;
    }

    public static bool TryParseOrderStatus(string value, out OrderQueryStatus status)
    {
        if (string.Equals(value, "open", StringComparison.OrdinalIgnoreCase))
        {
            status = OrderQueryStatus.Open;
            return true;
        }

        if (string.Equals(value, "closed", StringComparison.OrdinalIgnoreCase))
        {
            status = OrderQueryStatus.Closed;
            return true;
        }

        if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase))
        {
            status = OrderQueryStatus.All;
            return true;
        }

        status = OrderQueryStatus.Open;
        return false;
    }

    public static bool TryParseSide(string value, out OrderSide side)
    {
        if (string.Equals(value, "buy", StringComparison.OrdinalIgnoreCase))
        {
            side = OrderSide.Buy;
            return true;
        }

        if (string.Equals(value, "sell", StringComparison.OrdinalIgnoreCase))
        {
            side = OrderSide.Sell;
            return true;
        }

        side = OrderSide.Buy;
        return false;
    }

    public static bool TryParseType(string value, out OrderType type)
    {
        if (string.Equals(value, "market", StringComparison.OrdinalIgnoreCase))
        {
            type = OrderType.Market;
            return true;
        }

        if (string.Equals(value, "limit", StringComparison.OrdinalIgnoreCase))
        {
            type = OrderType.Limit;
            return true;
        }

        type = OrderType.Market;
        return false;
    }

    public static void WriteJson<T>(T payload)
    {
        var line = JsonSerializer.Serialize(payload);
        Console.Out.WriteLine(line);
    }

    public static void Fail(string? message, int exitCode)
    {
        Console.Error.WriteLine(message ?? "error");
        Environment.ExitCode = exitCode;
    }

    public static ExecutionConfig BuildConfig(ExecutionMode mode, bool armLiveFlag)
    {
        var armEnv = Environment.GetEnvironmentVariable("XWS_EXEC_ARM");
        var paperStatePath = mode == ExecutionMode.Paper
            ? Path.Combine(Environment.CurrentDirectory, "artifacts", "paper", "state.json")
            : null;
        return new ExecutionConfig(mode, armLiveFlag, armEnv, PaperStatePath: paperStatePath);
    }
}
