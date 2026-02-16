namespace Xws.Data.Mux;

public sealed class MuxRunnerOptions
{
    public int? MaxMessages { get; init; }
    public TimeSpan? Timeout { get; init; }
}
