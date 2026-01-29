namespace xws.Core.WebSocket;

public sealed class WebSocketRunnerOptions
{
    public int? MaxMessages { get; init; }
    public TimeSpan? Timeout { get; init; }
}
