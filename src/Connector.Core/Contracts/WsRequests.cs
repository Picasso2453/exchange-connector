namespace Connector.Core.Contracts;

/// <summary>
/// Base for all unified WebSocket requests.
/// </summary>
public abstract class UnifiedWsRequest
{
    public required string CorrelationId { get; init; }
    public required UnifiedExchange Exchange { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Subscribe to one or more symbols on a channel.
/// </summary>
public sealed class UnifiedWsSubscribeRequest : UnifiedWsRequest
{
    public required UnifiedWsChannel Channel { get; init; }
    public required string[] Symbols { get; init; }
    public string? Interval { get; init; }
    public int? Depth { get; init; }
    public Dictionary<string, string>? Options { get; init; }
}

/// <summary>
/// Unsubscribe from a channel.
/// </summary>
public sealed class UnifiedWsUnsubscribeRequest : UnifiedWsRequest
{
    public required UnifiedWsChannel Channel { get; init; }
    public required string[] Symbols { get; init; }
}
