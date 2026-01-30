using Xws.Exec;

namespace Xws.Exec.Tests;

public sealed class ExecutionSafetyTests
{
    [Fact]
    public void Mainnet_MissingFlag_Fails()
    {
        var config = new ExecutionConfig(ExecutionMode.Mainnet, false, "1");
        var result = ExecutionSafety.ValidateArming(config);

        Assert.False(result.Ok);
    }

    [Fact]
    public void Mainnet_MissingEnv_Fails()
    {
        var config = new ExecutionConfig(ExecutionMode.Mainnet, true, null);
        var result = ExecutionSafety.ValidateArming(config);

        Assert.False(result.Ok);
    }

    [Fact]
    public void Mainnet_WithFlagAndEnv_Passes()
    {
        var config = new ExecutionConfig(ExecutionMode.Mainnet, true, "1");
        var result = ExecutionSafety.ValidateArming(config);

        Assert.True(result.Ok);
    }

    [Fact]
    public void Paper_IgnoresArming()
    {
        var config = new ExecutionConfig(ExecutionMode.Paper, false, null);
        var result = ExecutionSafety.ValidateArming(config);

        Assert.True(result.Ok);
    }

    [Fact]
    public void Testnet_IgnoresArming()
    {
        var config = new ExecutionConfig(ExecutionMode.Testnet, false, null);
        var result = ExecutionSafety.ValidateArming(config);

        Assert.True(result.Ok);
    }
}
