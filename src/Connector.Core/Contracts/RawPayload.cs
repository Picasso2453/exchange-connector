namespace Connector.Core.Contracts;

/// <summary>
/// Optional raw exchange payload for escape-hatch access.
/// Not populated by default; enable via config flag.
/// </summary>
public sealed class RawPayload
{
    public string? RawJson { get; init; }
    public byte[]? RawBytes { get; init; }
    public string? ExchangeMessageType { get; init; }
}
