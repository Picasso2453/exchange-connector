namespace Xws.Exec;

public sealed record ExecutionConfig(
    ExecutionMode Mode,
    bool ArmLiveFlag,
    string? ArmEnvValue,
    string? UserAddress = null,
    HyperliquidCredentials? HyperliquidCredentials = null);

public sealed record HyperliquidCredentials(
    string AccountAddress,
    string PrivateKey);
