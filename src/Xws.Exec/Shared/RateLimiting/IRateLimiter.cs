namespace Xws.Exec;

public interface IRateLimiter
{
    Task WaitAsync(CancellationToken cancellationToken);
}
