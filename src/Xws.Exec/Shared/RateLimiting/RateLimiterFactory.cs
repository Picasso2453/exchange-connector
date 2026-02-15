namespace Xws.Exec;

public static class RateLimiterFactory
{
    public static IRateLimiter CreateHyperliquid()
    {
        var rate = ReadRate("XWS_HL_RATE_LIMIT", 20);
        return new TokenBucketRateLimiter(rate, rate);
    }

    public static IRateLimiter CreateOkx()
    {
        var rate = ReadRate("XWS_OKX_RATE_LIMIT", 10);
        return new TokenBucketRateLimiter(rate, rate);
    }

    public static IRateLimiter CreateBybit()
    {
        var rate = ReadRate("XWS_BYBIT_RATE_LIMIT", 10);
        return new TokenBucketRateLimiter(rate, rate);
    }

    public static IRateLimiter CreateMexc()
    {
        var rate = ReadRate("XWS_MEXC_RATE_LIMIT", 10);
        return new TokenBucketRateLimiter(rate, rate);
    }

    private static int ReadRate(string envVar, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        if (int.TryParse(value, out var parsed) && parsed > 0)
        {
            return parsed;
        }

        return fallback;
    }
}
