using Connector.Core.Transport;

namespace Connector.Tests;

public class TransportTests
{
    [Fact]
    public async Task TokenBucketRateLimiter_AllowsBurstUpToMax()
    {
        var limiter = new TokenBucketRateLimiter(3, TimeSpan.FromSeconds(1));

        // Should allow 3 without waiting
        await limiter.WaitAsync(CancellationToken.None);
        await limiter.WaitAsync(CancellationToken.None);
        await limiter.WaitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TokenBucketRateLimiter_BlocksAfterBurstExhausted()
    {
        var limiter = new TokenBucketRateLimiter(1, TimeSpan.FromSeconds(10));

        // First call succeeds
        await limiter.WaitAsync(CancellationToken.None);

        // Second call should block; cancel after short timeout
        using var cts = new CancellationTokenSource(100);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await limiter.WaitAsync(cts.Token);
        });
    }

    [Fact]
    public void TransportRestRequest_HasAllProperties()
    {
        var req = new TransportRestRequest
        {
            Method = HttpMethod.Post,
            Path = "/api/v1/orders",
            Body = "{\"symbol\":\"BTC\"}",
            ContentType = "application/json",
            Headers = new() { ["X-Api-Key"] = "test" },
            QueryParams = new() { ["limit"] = "10" }
        };

        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("/api/v1/orders", req.Path);
        Assert.NotNull(req.Body);
        Assert.NotNull(req.Headers);
        Assert.NotNull(req.QueryParams);
    }

    [Fact]
    public void TransportWsInbound_HasTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var inbound = new TransportWsInbound
        {
            Payload = "{\"test\":true}",
            ReceivedAt = now
        };

        Assert.Equal(now, inbound.ReceivedAt);
        Assert.Null(inbound.RawBytes);
    }
}
