using System.Text.Json;

namespace Xws.Exec;

internal sealed record PaperOrder(
    string OrderId,
    string ClientOrderId,
    string Symbol,
    OrderSide Side,
    OrderType Type,
    decimal Size,
    decimal? Price,
    decimal FilledSize,
    OrderStatus Status,
    DateTimeOffset UpdatedAt);

internal sealed record PaperStateSnapshot(
    int Version,
    long OrderSequence,
    long ClientOrderSequence,
    List<PaperOrder> Orders,
    List<PositionState> Positions)
{
    public static PaperStateSnapshot Empty { get; } = new(
        Version: PaperStateStore.CurrentVersion,
        OrderSequence: 0,
        ClientOrderSequence: 0,
        Orders: new List<PaperOrder>(),
        Positions: new List<PositionState>());
}

internal static class PaperStateStore
{
    public const int CurrentVersion = 1;

    public static PaperStateSnapshot LoadOrEmpty(string path, Action<string>? warn = null)
    {
        if (!File.Exists(path))
        {
            return PaperStateSnapshot.Empty;
        }

        try
        {
            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<PaperStateSnapshot>(json);
            if (state is null)
            {
                throw new InvalidOperationException("paper state file is invalid");
            }

            if (state.Version <= 0)
            {
                warn?.Invoke("paper state missing version; assuming version 1");
                state = state with { Version = CurrentVersion };
            }

            return state;
        }
        catch (Exception ex)
        {
            warn?.Invoke($"paper state file corrupt, resetting: {ex.Message}");
            TryPreserveCorruptFile(path, warn);
            return PaperStateSnapshot.Empty;
        }
    }

    public static void Save(string path, PaperStateSnapshot state)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(state);
        File.WriteAllText(path, json);
    }

    private static void TryPreserveCorruptFile(string path, Action<string>? warn)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            var corruptPath = $"{path}.corrupt.{timestamp}";
            File.Copy(path, corruptPath, overwrite: true);
        }
        catch (Exception ex)
        {
            warn?.Invoke($"failed to preserve corrupt state: {ex.Message}");
        }
    }
}
