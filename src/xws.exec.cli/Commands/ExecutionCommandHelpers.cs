using System.Text.Json;

using Xws.Exec;

namespace Xws.Exec.Cli;

public static class ExecutionCommandHelpers
{
    private static readonly string[] SensitiveTokens =
    [
        "KEY",
        "SECRET",
        "PASS",
        "TOKEN",
        "PRIVATE",
        "USER",
        "ADDR"
    ];
    private static readonly char[] AllowedHlChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    private static readonly char[] AllowedMexcSpotChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    private static readonly char[] AllowedMexcFutChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_".ToCharArray();
    private static readonly char[] AllowedOkxChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-".ToCharArray();
    private static readonly char[] AllowedBybitChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

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
        var text = message ?? "error";
        text = RedactSecrets(text);
        if (!text.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
        {
            text = $"Error: {text}";
        }
        Console.Error.WriteLine(text);
        Environment.ExitCode = exitCode;
    }

    public static bool ValidatePlaceInputs(OrderType orderType, decimal size, decimal? price, out string error)
    {
        if (orderType == OrderType.Limit && price is null)
        {
            error = "Missing required --price. Limit orders require a price. Provide --price or use --type market.";
            return false;
        }

        if (size <= 0)
        {
            error = "Invalid --size. Value must be greater than 0. Provide a positive number.";
            return false;
        }

        if (price.HasValue && price.Value < 0)
        {
            error = "Invalid --price. Value must be greater than or equal to 0. Provide a non-negative number.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool ValidateAmendInputs(decimal? size, decimal? price, out string error)
    {
        if (price is null && size is null)
        {
            error = "Nothing to amend. Amend requires --price, --size, or both. Provide at least one value.";
            return false;
        }

        if (size.HasValue && size.Value <= 0)
        {
            error = "Invalid --size. Value must be greater than 0. Provide a positive number.";
            return false;
        }

        if (price.HasValue && price.Value < 0)
        {
            error = "Invalid --price. Value must be greater than or equal to 0. Provide a non-negative number.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static ExecutionConfig BuildConfig(ExecutionMode mode, bool armLiveFlag)
    {
        var armEnv = Environment.GetEnvironmentVariable("XWS_EXEC_ARM");
        var paperStatePath = mode == ExecutionMode.Paper
            ? Path.Combine(Environment.CurrentDirectory, "artifacts", "paper", "state.json")
            : null;
        return new ExecutionConfig(mode, armLiveFlag, armEnv, PaperStatePath: paperStatePath);
    }

    public static bool IsValidSymbol(string exchange, string symbol, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(symbol))
        {
            error = "symbol is required";
            return false;
        }

        if (symbol.Length > 32)
        {
            error = "symbol is too long (max 32 chars)";
            return false;
        }

        return exchange switch
        {
            "hl" => ValidateChars(symbol, AllowedHlChars, "hl symbol must be alphanumeric", out error),
            "mexc" => ValidateChars(symbol, AllowedMexcFutChars, "mexc symbol must be alphanumeric or underscore", out error),
            "okx" => ValidateChars(symbol, AllowedOkxChars, "okx symbol must be alphanumeric or dash", out error),
            "bybit" => ValidateChars(symbol, AllowedBybitChars, "bybit symbol must be alphanumeric", out error),
            _ => ValidateChars(symbol, AllowedHlChars, "symbol must be alphanumeric", out error)
        };
    }

    private static bool ValidateChars(string symbol, char[] allowed, string message, out string error)
    {
        foreach (var ch in symbol)
        {
            if (Array.IndexOf(allowed, ch) < 0)
            {
                error = message;
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static string RedactSecrets(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        var redacted = message;
        foreach (var entry in Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>())
        {
            if (entry.Key is not string name || entry.Value is not string value)
            {
                continue;
            }

            if (!IsSensitiveName(name) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (redacted.Contains(value, StringComparison.Ordinal))
            {
                redacted = redacted.Replace(value, "[REDACTED]", StringComparison.Ordinal);
            }
        }

        return redacted;
    }

    private static bool IsSensitiveName(string name)
    {
        if (!name.StartsWith("XWS_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        foreach (var token in SensitiveTokens)
        {
            if (name.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
