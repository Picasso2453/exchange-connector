using Xws.Exec.Cli;

namespace Xws.Exec.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void TryParseMode_AcceptsKnownValues()
    {
        Assert.True(ExecutionCommandHelpers.TryParseMode("paper", out var paper));
        Assert.Equal(ExecutionMode.Paper, paper);

        Assert.True(ExecutionCommandHelpers.TryParseMode("testnet", out var testnet));
        Assert.Equal(ExecutionMode.Testnet, testnet);

        Assert.True(ExecutionCommandHelpers.TryParseMode("mainnet", out var mainnet));
        Assert.Equal(ExecutionMode.Mainnet, mainnet);
    }

    [Fact]
    public void TryParseExchange_RejectsUnknownValue()
    {
        var ok = ExecutionCommandHelpers.TryParseExchange("unknown", out _);
        Assert.False(ok);
    }

    [Fact]
    public void IsValidSymbol_RejectsInvalidCharacters()
    {
        var ok = ExecutionCommandHelpers.IsValidSymbol("okx", "BTC/USDT", out var error);

        Assert.False(ok);
        Assert.Contains("okx", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryParseSide_RejectsUnknownValue()
    {
        var ok = ExecutionCommandHelpers.TryParseSide("hold", out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryParseType_RejectsUnknownValue()
    {
        var ok = ExecutionCommandHelpers.TryParseType("stop", out _);
        Assert.False(ok);
    }

    [Fact]
    public void IsValidSymbol_RejectsEmptyValue()
    {
        var ok = ExecutionCommandHelpers.IsValidSymbol("hl", "", out var error);

        Assert.False(ok);
        Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePlaceInputs_RejectsInvalidValues()
    {
        Assert.False(ExecutionCommandHelpers.ValidatePlaceInputs(OrderType.Limit, 1m, null, out var missingPrice));
        Assert.Contains("--price", missingPrice, StringComparison.OrdinalIgnoreCase);

        Assert.False(ExecutionCommandHelpers.ValidatePlaceInputs(OrderType.Market, -1m, null, out var badSize));
        Assert.Contains("--size", badSize, StringComparison.OrdinalIgnoreCase);

        Assert.False(ExecutionCommandHelpers.ValidatePlaceInputs(OrderType.Market, 1m, -0.01m, out var badPrice));
        Assert.Contains("--price", badPrice, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAmendInputs_RejectsInvalidValues()
    {
        Assert.False(ExecutionCommandHelpers.ValidateAmendInputs(null, null, out var missingFields));
        Assert.Contains("amend", missingFields, StringComparison.OrdinalIgnoreCase);

        Assert.False(ExecutionCommandHelpers.ValidateAmendInputs(-1m, null, out var badSize));
        Assert.Contains("--size", badSize, StringComparison.OrdinalIgnoreCase);

        Assert.False(ExecutionCommandHelpers.ValidateAmendInputs(null, -0.01m, out var badPrice));
        Assert.Contains("--price", badPrice, StringComparison.OrdinalIgnoreCase);
    }
}
