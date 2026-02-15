using System.Text.Json;
using Xunit;

namespace Xws.Exec.Tests;

public sealed class PaperStateStoreTests
{
    [Fact]
    public async Task CorruptStateFile_IsRecoveredAndVersioned()
    {
        var root = Path.Combine(Path.GetTempPath(), "xws-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var statePath = Path.Combine(root, "state.json");
        await File.WriteAllTextAsync(statePath, "{not-json");

        var client = new PaperExecutionClient(ExecutionMode.Paper, statePath);
        var request = new PlaceOrderRequest("SOL", OrderSide.Buy, OrderType.Market, 1m, null, "test-001", false);
        var result = await client.PlaceAsync(request, CancellationToken.None);

        Assert.Equal(OrderStatus.Filled, result.Status);
        Assert.True(File.Exists(statePath));

        var json = await File.ReadAllTextAsync(statePath);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("Version", out var version));
        Assert.Equal(1, version.GetInt32());

        var corruptFiles = Directory.GetFiles(root, "state.json.corrupt.*");
        Assert.NotEmpty(corruptFiles);
    }
}
