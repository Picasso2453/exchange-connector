using System.Threading.Channels;

using Xws.Data.Shared.Logging;

namespace xws.Commands;

public static class CommandHelpers
{
    private static readonly string[] SupportedExchanges = ["hl", "okx", "bybit", "mexc"];

    public static CancellationTokenSource CreateShutdownCts()
    {
        var cts = new CancellationTokenSource();
        var cancelLogged = 0;
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            if (Interlocked.Exchange(ref cancelLogged, 1) == 0)
            {
                Logger.Info("shutdown requested");
            }
            cts.Cancel();
        };
        return cts;
    }

    public static bool ValidateMaxMessagesTimeout(int? maxMessages, int? timeoutSeconds)
    {
        if (maxMessages.HasValue && maxMessages.Value <= 0)
        {
            Logger.Error("Invalid --max-messages. Value must be greater than 0. Provide a positive integer.");
            return false;
        }

        if (timeoutSeconds.HasValue && timeoutSeconds.Value <= 0)
        {
            Logger.Error("Invalid --timeout-seconds. Value must be greater than 0. Provide a positive integer.");
            return false;
        }

        if (timeoutSeconds.HasValue && !maxMessages.HasValue)
        {
            Logger.Error("Invalid --timeout-seconds. Timeout requires --max-messages. Provide --max-messages or remove --timeout-seconds.");
            return false;
        }

        return true;
    }

    public static bool ValidateFormat(string? format)
    {
        if (!IsValidFormat(format))
        {
            Logger.Error("Invalid --format. Supported values are envelope or raw. Use --format envelope for JSONL envelopes.");
            return false;
        }

        return true;
    }

    public static bool TryParseSub(string input, out ParsedSub parsed)
    {
        return TryParseSub(input, out parsed, out _);
    }

    public static bool TryParseSub(string input, out ParsedSub parsed, out string error)
    {
        parsed = new ParsedSub(string.Empty, null, Array.Empty<string>());
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            error = "subscription is empty";
            return false;
        }

        var parts = input.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            error = "subscription must be in exchange[.market]=SYM1,SYM2 format";
            return false;
        }

        var exchangePart = parts[0].Trim();
        var symbolsPart = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(exchangePart) || string.IsNullOrWhiteSpace(symbolsPart))
        {
            error = "subscription must include exchange and symbol list";
            return false;
        }

        var exchangePieces = exchangePart.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        var exchange = exchangePieces[0].Trim();
        var market = exchangePieces.Length > 1 ? exchangePieces[1].Trim() : null;
        if (string.IsNullOrWhiteSpace(exchange))
        {
            error = "exchange cannot be empty";
            return false;
        }

        if (!IsSupportedExchange(exchange))
        {
            error = "unsupported exchange (supported: hl, okx, bybit, mexc)";
            return false;
        }

        var symbols = symbolsPart
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
        if (symbols.Length == 0)
        {
            error = "at least one symbol is required";
            return false;
        }

        if (!SymbolValidation.ValidateSymbols(exchange, market, symbols, out var symbolError))
        {
            error = symbolError;
            return false;
        }

        parsed = new ParsedSub(exchange, market, symbols);
        return true;
    }

    public static async Task WriteOutputAsync(ChannelReader<string> reader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var line in reader.ReadAllAsync(cancellationToken))
            {
                Console.Out.WriteLine(line);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static bool IsValidFormat(string? format)
    {
        return string.Equals(format, "envelope", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "raw", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedExchange(string exchange)
    {
        return SupportedExchanges.Any(value => value.Equals(exchange, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record ParsedSub(string Exchange, string? Market, string[] Symbols);

public static class SymbolValidation
{
    private static readonly char[] AllowedHlChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    private static readonly char[] AllowedMexcSpotChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    private static readonly char[] AllowedMexcFutChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_".ToCharArray();
    private static readonly char[] AllowedOkxChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-".ToCharArray();
    private static readonly char[] AllowedBybitChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    public static bool IsValidSymbol(string exchange, string? market, string symbol, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(symbol))
        {
            error = "symbol cannot be empty";
            return false;
        }

        if (symbol.Length > 32)
        {
            error = "symbol is too long (max 32 chars)";
            return false;
        }

        return exchange.ToLowerInvariant() switch
        {
            "hl" => ValidateChars(symbol, AllowedHlChars, "hl symbol must be alphanumeric", out error),
            "mexc" => market?.Equals("fut", StringComparison.OrdinalIgnoreCase) == true
                ? ValidateChars(symbol, AllowedMexcFutChars, "mexc futures symbol must be alphanumeric or underscore", out error)
                : ValidateChars(symbol, AllowedMexcSpotChars, "mexc spot symbol must be alphanumeric", out error),
            "okx" => ValidateChars(symbol, AllowedOkxChars, "okx symbol must be alphanumeric or dash", out error),
            "bybit" => ValidateChars(symbol, AllowedBybitChars, "bybit symbol must be alphanumeric", out error),
            _ => ValidateChars(symbol, AllowedHlChars, "symbol must be alphanumeric", out error)
        };
    }

    public static bool ValidateSymbols(string exchange, string? market, string[] symbols, out string error)
    {
        foreach (var symbol in symbols)
        {
            if (!IsValidSymbol(exchange, market, symbol, out error))
            {
                return false;
            }
        }

        error = string.Empty;
        return true;
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
}
