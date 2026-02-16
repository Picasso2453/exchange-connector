namespace Xws.Exec;

/// <summary>
/// Token-bucket rate limiter.
/// </summary>
public sealed class TokenBucketRateLimiter : IRateLimiter
{
    private readonly int _capacity;
    private readonly double _refillPerSecond;
    private double _tokens;
    private DateTimeOffset _lastRefill;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Creates a token bucket limiter.
    /// </summary>
    /// <param name="capacity">Maximum token capacity.</param>
    /// <param name="refillPerSecond">Tokens added per second.</param>
    public TokenBucketRateLimiter(int capacity, int refillPerSecond)
    {
        _capacity = Math.Max(1, capacity);
        _refillPerSecond = Math.Max(1, refillPerSecond);
        _tokens = _capacity;
        _lastRefill = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _gate.WaitAsync(cancellationToken);
            var shouldDelay = false;
            TimeSpan delay = default;
            try
            {
                Refill();
                if (_tokens >= 1d)
                {
                    _tokens -= 1d;
                    return;
                }

                var deficit = 1d - _tokens;
                var delaySeconds = deficit / _refillPerSecond;
                if (delaySeconds < 0.001)
                {
                    delaySeconds = 0.001;
                }
                delay = TimeSpan.FromSeconds(delaySeconds);
                shouldDelay = true;
            }
            finally
            {
                _gate.Release();
            }

            if (shouldDelay)
            {
                Console.Error.WriteLine("Warning: rate limit throttling request. Request rate exceeded configured limit. Reduce rate or increase limits.");
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private void Refill()
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;
        if (elapsed <= 0)
        {
            return;
        }

        var refill = elapsed * _refillPerSecond;
        _tokens = Math.Min(_capacity, _tokens + refill);
        _lastRefill = now;
    }
}
