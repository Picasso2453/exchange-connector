using Connector.Core.Abstractions;

namespace Connector.Core.Transport;

/// <summary>
/// Simple token-bucket rate limiter.
/// </summary>
public sealed class TokenBucketRateLimiter : IRateLimiter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly int _maxTokens;
    private readonly TimeSpan _refillInterval;
    private int _tokens;
    private DateTimeOffset _lastRefill;

    public TokenBucketRateLimiter(int maxTokens, TimeSpan refillInterval)
    {
        _maxTokens = maxTokens;
        _refillInterval = refillInterval;
        _tokens = maxTokens;
        _lastRefill = DateTimeOffset.UtcNow;
    }

    public async Task WaitAsync(CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            await _semaphore.WaitAsync(ct);
            try
            {
                Refill();
                if (_tokens > 0)
                {
                    _tokens--;
                    return;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            // Wait for next refill opportunity
            await Task.Delay(50, ct);
        }
    }

    private void Refill()
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = now - _lastRefill;
        if (elapsed >= _refillInterval)
        {
            var refills = (int)(elapsed / _refillInterval);
            _tokens = Math.Min(_maxTokens, _tokens + refills);
            _lastRefill = now;
        }
    }
}
