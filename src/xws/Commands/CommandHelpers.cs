using System.Threading.Channels;
using xws.Core.Shared.Logging;

namespace xws.Commands;

public static class CommandHelpers
{
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
            Logger.Error("--max-messages must be greater than 0");
            return false;
        }

        if (timeoutSeconds.HasValue && timeoutSeconds.Value <= 0)
        {
            Logger.Error("--timeout-seconds must be greater than 0");
            return false;
        }

        if (timeoutSeconds.HasValue && !maxMessages.HasValue)
        {
            Logger.Error("--timeout-seconds requires --max-messages");
            return false;
        }

        return true;
    }

    public static bool ValidateFormat(string? format)
    {
        if (!IsValidFormat(format))
        {
            Logger.Error("--format must be envelope or raw");
            return false;
        }

        return true;
    }

    public static bool TryParseSub(string input, out ParsedSub parsed)
    {
        parsed = new ParsedSub(string.Empty, null, Array.Empty<string>());
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var parts = input.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        var exchangePart = parts[0].Trim();
        var symbolsPart = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(exchangePart) || string.IsNullOrWhiteSpace(symbolsPart))
        {
            return false;
        }

        var exchangePieces = exchangePart.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        var exchange = exchangePieces[0].Trim();
        var market = exchangePieces.Length > 1 ? exchangePieces[1].Trim() : null;
        if (string.IsNullOrWhiteSpace(exchange))
        {
            return false;
        }

        var symbols = symbolsPart
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
        if (symbols.Length == 0)
        {
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
}

public sealed record ParsedSub(string Exchange, string? Market, string[] Symbols);
