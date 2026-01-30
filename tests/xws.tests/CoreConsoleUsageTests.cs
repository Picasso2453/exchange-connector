namespace xws.tests;

public sealed class CoreConsoleUsageTests
{
    [Fact]
    public void CoreProject_DoesNotUseConsole()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
        var corePath = Path.Combine(repoRoot, "src", "Xws.Core");

        Assert.True(Directory.Exists(corePath), $"Expected Core path at {corePath}");

        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(corePath, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (text.Contains("Console.", StringComparison.Ordinal))
            {
                offenders.Add(file);
            }
        }

        Assert.True(offenders.Count == 0, $"Console usage found in: {string.Join(", ", offenders)}");
    }
}
