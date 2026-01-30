namespace Xws.Exec;

public sealed record ExecutionConfig(
    ExecutionMode Mode,
    bool ArmLiveFlag,
    string? ArmEnvValue,
    string? UserAddress = null,
    ExecutionCredentials? Credentials = null);

public sealed record ExecutionCredentials(
    string? ApiKey = null,
    string? ApiSecret = null,
    string? ApiPassphrase = null);
