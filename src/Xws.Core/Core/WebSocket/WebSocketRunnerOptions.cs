namespace Xws.Data.WebSocket;

public sealed class WebSocketRunnerOptions
{
    public int? MaxMessages { get; init; }
    public TimeSpan? Timeout { get; init; }
    public TimeSpan? StaleTimeout { get; init; }
    public TimeSpan? PingInterval { get; init; }
    public string? PingPayload { get; init; }
}
