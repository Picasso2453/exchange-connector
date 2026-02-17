using System.Globalization;
using System.Text.Json;
using Connector.Core.Abstractions;
using Connector.Core.Contracts;
using Connector.Core.Transport;

namespace Connector.Core.Exchanges.Hyperliquid;

/// <summary>
/// Translates unified REST requests to Hyperliquid info API.
/// </summary>
public sealed class HyperliquidRestTranslator : IRestTranslator
{
    public TransportRestRequest ToExchangeRequest<TResponse>(UnifiedRestRequest<TResponse> request)
    {
        return request switch
        {
            GetCandlesRequest r => BuildCandlesRequest(r),
            GetOpenOrdersRequest r => BuildOpenOrdersRequest(r),
            GetPositionsRequest r => BuildPositionsRequest(r),
            _ => throw new NotSupportedException($"Operation {request.Operation} not supported for Hyperliquid REST")
        };
    }

    public TResponse FromExchangeResponse<TResponse>(UnifiedRestRequest<TResponse> request, TransportRestResponse response)
    {
        return request switch
        {
            GetCandlesRequest => (TResponse)(object)ParseCandlesResponse(response.Body),
            GetOpenOrdersRequest => (TResponse)(object)ParseOpenOrdersResponse(response.Body),
            GetPositionsRequest => (TResponse)(object)ParsePositionsResponse(response.Body),
            _ => throw new NotSupportedException($"Response parsing not supported for {request.Operation}")
        };
    }

    private static TransportRestRequest BuildCandlesRequest(GetCandlesRequest r)
    {
        var body = JsonSerializer.Serialize(new
        {
            type = "candleSnapshot",
            req = new { coin = r.Symbol, interval = r.Interval, startTime = 0, endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
        });

        return new TransportRestRequest
        {
            Method = HttpMethod.Post,
            Path = "/info",
            Body = body,
            ContentType = "application/json"
        };
    }

    private static TransportRestRequest BuildOpenOrdersRequest(GetOpenOrdersRequest r)
    {
        var user = r.Params?.GetValueOrDefault("user") ?? throw new InvalidOperationException("user param required");
        var body = JsonSerializer.Serialize(new { type = "openOrders", user });

        return new TransportRestRequest
        {
            Method = HttpMethod.Post,
            Path = "/info",
            Body = body,
            ContentType = "application/json"
        };
    }

    private static TransportRestRequest BuildPositionsRequest(GetPositionsRequest r)
    {
        var user = r.Params?.GetValueOrDefault("user") ?? throw new InvalidOperationException("user param required");
        var body = JsonSerializer.Serialize(new { type = "clearinghouseState", user });

        return new TransportRestRequest
        {
            Method = HttpMethod.Post,
            Path = "/info",
            Body = body,
            ContentType = "application/json"
        };
    }

    private static GetCandlesResponse ParseCandlesResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var candles = doc.RootElement.EnumerateArray().Select(c => new CandleEntry
        {
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(c.GetProperty("t").GetInt64()),
            Open = c.GetProperty("o").GetDecimal(),
            High = c.GetProperty("h").GetDecimal(),
            Low = c.GetProperty("l").GetDecimal(),
            Close = c.GetProperty("c").GetDecimal(),
            Volume = c.GetProperty("v").GetDecimal()
        }).ToArray();

        return new GetCandlesResponse { Candles = candles };
    }

    private static GetOpenOrdersResponse ParseOpenOrdersResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var orders = doc.RootElement.EnumerateArray().Select(o => new UserOrderEntry
        {
            OrderId = o.GetProperty("oid").GetInt64().ToString(),
            ClientOrderId = o.TryGetProperty("cloid", out var c) ? c.GetString() : null,
            Symbol = o.GetProperty("coin").GetString()!,
            Side = o.GetProperty("side").GetString()!.ToLowerInvariant(),
            OrderType = "limit",
            Price = decimal.Parse(o.GetProperty("limitPx").GetString()!, CultureInfo.InvariantCulture),
            Size = decimal.Parse(o.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture),
            FilledSize = 0m,
            Status = "open",
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(o.GetProperty("timestamp").GetInt64())
        }).ToArray();

        return new GetOpenOrdersResponse { Orders = orders };
    }

    private static GetPositionsResponse ParsePositionsResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var assetPositions = doc.RootElement.GetProperty("assetPositions");

        var positions = assetPositions.EnumerateArray().Select(ap =>
        {
            var pos = ap.GetProperty("position");
            return new PositionEntry
            {
                Symbol = pos.GetProperty("coin").GetString()!,
                Side = decimal.Parse(pos.GetProperty("szi").GetString()!, CultureInfo.InvariantCulture) >= 0 ? "long" : "short",
                Size = Math.Abs(decimal.Parse(pos.GetProperty("szi").GetString()!, CultureInfo.InvariantCulture)),
                EntryPrice = decimal.Parse(pos.GetProperty("entryPx").GetString() ?? "0", CultureInfo.InvariantCulture),
                UnrealizedPnl = decimal.Parse(pos.GetProperty("unrealizedPnl").GetString() ?? "0", CultureInfo.InvariantCulture),
                LiquidationPrice = pos.TryGetProperty("liquidationPx", out var lp) && lp.GetString() is not null
                    ? decimal.Parse(lp.GetString()!, CultureInfo.InvariantCulture) : null,
                Leverage = pos.TryGetProperty("leverage", out var lev)
                    ? lev.GetProperty("value").GetDecimal() : null
            };
        }).ToArray();

        return new GetPositionsResponse { Positions = positions };
    }
}
