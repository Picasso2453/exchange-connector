namespace Connector.Core.Abstractions;

/// <summary>
/// Rate limiter abstraction. Callers await before executing a rate-limited action.
/// </summary>
public interface IRateLimiter
{
    Task WaitAsync(CancellationToken ct);
}
