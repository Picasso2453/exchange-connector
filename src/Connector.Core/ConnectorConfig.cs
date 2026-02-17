using Connector.Core.Contracts;

namespace Connector.Core;

/// <summary>
/// Configuration for a connector instance.
/// </summary>
public sealed class ConnectorConfig
{
    public required UnifiedExchange Exchange { get; init; }
    public required string[] Symbols { get; init; }
    public required UnifiedWsChannel[] Channels { get; init; }
    public bool NoAuth { get; init; }
    public bool IncludeRaw { get; init; }
    public string? ConfigFilePath { get; init; }
}
