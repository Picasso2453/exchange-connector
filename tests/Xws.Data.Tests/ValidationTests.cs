using xws.Commands;

namespace Xws.Data.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void SymbolValidation_RejectsInvalidHlSymbol()
    {
        var ok = SymbolValidation.IsValidSymbol("hl", null, "SOL!", out var error);

        Assert.False(ok);
        Assert.Contains("hl symbol", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SymbolValidation_RejectsMexcSpotUnderscore()
    {
        var ok = SymbolValidation.IsValidSymbol("mexc", "spot", "BTC_USDT", out var error);

        Assert.False(ok);
        Assert.Contains("mexc spot", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SymbolValidation_AllowsMexcFuturesUnderscore()
    {
        var ok = SymbolValidation.IsValidSymbol("mexc", "fut", "BTC_USDT", out var error);

        Assert.True(ok);
        Assert.True(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void SymbolValidation_AllowsOkxDash()
    {
        var ok = SymbolValidation.IsValidSymbol("okx", null, "BTC-USDT-SWAP", out var error);

        Assert.True(ok);
        Assert.True(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryParseSub_RejectsInvalidFormat()
    {
        var ok = CommandHelpers.TryParseSub("hl", out _, out var error);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryParseSub_ParsesValidFormat()
    {
        var ok = CommandHelpers.TryParseSub("hl=SOL", out var parsed, out var error);

        Assert.True(ok);
        Assert.True(string.IsNullOrWhiteSpace(error));
        Assert.Equal("hl", parsed.Exchange);
        Assert.Equal(new[] { "SOL" }, parsed.Symbols);
    }

    [Fact]
    public void TryParseSub_RejectsUnsupportedExchange()
    {
        var ok = CommandHelpers.TryParseSub("kraken=BTC", out _, out var error);

        Assert.False(ok);
        Assert.Contains("unsupported exchange", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateMaxMessagesTimeout_RejectsNonPositiveValues()
    {
        Assert.False(CommandHelpers.ValidateMaxMessagesTimeout(0, null));
        Assert.False(CommandHelpers.ValidateMaxMessagesTimeout(null, 0));
    }

    [Fact]
    public void ValidateMaxMessagesTimeout_RequiresMaxMessagesWhenTimeoutProvided()
    {
        Assert.False(CommandHelpers.ValidateMaxMessagesTimeout(null, 5));
    }
}
