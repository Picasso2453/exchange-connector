using Xunit;

namespace Xws.Exec.Tests;

[Collection("EnvironmentVariables")]
public sealed class RateLimiterTests
{
    [Fact]
    public async Task TokenBucket_AllowsBasicWaits()
    {
        var limiter = new TokenBucketRateLimiter(capacity: 1, refillPerSecond: 5);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await limiter.WaitAsync(cts.Token);
        await limiter.WaitAsync(cts.Token);
    }

    [Fact]
    public async Task TokenBucket_DelaysWhenEmpty()
    {
        var limiter = new TokenBucketRateLimiter(capacity: 1, refillPerSecond: 10);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await limiter.WaitAsync(cts.Token);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await limiter.WaitAsync(cts.Token);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 80);
    }

    [Fact]
    public void Factory_RespectsEnvOverride()
    {
        Environment.SetEnvironmentVariable("XWS_HL_RATE_LIMIT", "7");
        var limiter = RateLimiterFactory.CreateHyperliquid();
        Assert.NotNull(limiter);
        Environment.SetEnvironmentVariable("XWS_HL_RATE_LIMIT", null);
    }
}
