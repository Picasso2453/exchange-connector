using xws.Config;

namespace xws.tests;

[Collection("EnvironmentVariables")]
public sealed class DotEnvLoaderTests
{
    [Fact]
    public void ParseLine_ParsesKeyValue()
    {
        Assert.True(DotEnvLoader.TryParseLine("A=1", out var key, out var value));
        Assert.Equal("A", key);
        Assert.Equal("1", value);
    }

    [Fact]
    public void ParseLine_IgnoresCommentsAndBlankLines()
    {
        Assert.False(DotEnvLoader.TryParseLine("", out _, out _));
        Assert.False(DotEnvLoader.TryParseLine("   ", out _, out _));
        Assert.False(DotEnvLoader.TryParseLine("# comment", out _, out _));
    }

    [Fact]
    public void ParseLine_TrimsWhitespace()
    {
        Assert.True(DotEnvLoader.TryParseLine(" A = 1 ", out var key, out var value));
        Assert.Equal("A", key);
        Assert.Equal("1", value);
    }

    [Fact]
    public void ParseLine_StripsQuotedValues()
    {
        Assert.True(DotEnvLoader.TryParseLine("A=\"hello world\"", out var key, out var value));
        Assert.Equal("A", key);
        Assert.Equal("hello world", value);
    }

    [Fact]
    public void Load_DoesNotOverrideExistingEnv()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "XWS_TEST_KEY=from-file");

        Environment.SetEnvironmentVariable("XWS_TEST_KEY", "from-env");
        try
        {
            var result = DotEnvLoader.Load(tempFile, required: true);
            Assert.True(result.Loaded);
            Assert.Equal("from-env", Environment.GetEnvironmentVariable("XWS_TEST_KEY"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("XWS_TEST_KEY", null);
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_RequiredMissingFile_ReturnsError()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".env");
        var result = DotEnvLoader.Load(missing, required: true);

        Assert.False(result.Loaded);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Load_OptionalMissingFile_IsNoop()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".env");
        var result = DotEnvLoader.Load(missing, required: false);

        Assert.False(result.Loaded);
        Assert.Null(result.Error);
    }
}
