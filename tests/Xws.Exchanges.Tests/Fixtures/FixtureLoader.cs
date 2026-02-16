namespace Xws.Exchanges.Tests.Fixtures;

public static class FixtureLoader
{
    public static IReadOnlyList<string> LoadLines(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("file name is required", nameof(fileName));
        }

        var path = GetFixturePath(fileName);
        return File.ReadAllLines(path);
    }

    private static string GetFixturePath(string fileName)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));

        var fixtures = Path.Combine(repoRoot, "tests", "xws.tests", "Fixtures");
        return Path.Combine(fixtures, fileName);
    }
}
