using System.Globalization;
using System.Text.Json;
using Connector.Core.Abstractions;
using Connector.Core.Contracts;
using Connector.Core.Transport;

namespace Connector.Core.Exchanges.Hyperliquid;

/// <summary>
/// Translates unified REST requests to Hyperliquid info/exchange API.
/// Supports all HL info endpoints and exchange endpoints.
/// </summary>
public sealed class HyperliquidRestTranslator : IRestTranslator
{
    public TransportRestRequest ToExchangeRequest<TResponse>(UnifiedRestRequest<TResponse> request)
    {
        return request switch
        {
            // Info endpoints (POST /info)
            GetCandlesRequest r => BuildInfoRequest(new { type = "candleSnapshot", req = new { coin = r.Symbol, interval = r.Interval, startTime = r.StartTime ?? 0, endTime = r.EndTime ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } }),
            GetL2BookRequest r => BuildL2BookRequest(r),
            GetAllMidsRequest => BuildInfoRequest(new { type = "allMids" }),
            GetMetaRequest r => BuildInfoRequest(r.IncludeAssetCtxs ? new { type = "metaAndAssetCtxs" } : (object)new { type = "meta" }),
            GetFundingHistoryRequest r => BuildInfoRequest(r.EndTime.HasValue ? new { type = "fundingHistory", coin = r.Symbol, startTime = r.StartTime, endTime = r.EndTime.Value } : (object)new { type = "fundingHistory", coin = r.Symbol, startTime = r.StartTime }),
            GetPredictedFundingsRequest => BuildInfoRequest(new { type = "predictedFundings" }),
            GetOpenOrdersRequest r => BuildInfoRequest(new { type = "openOrders", user = GetUser(r) }),
            GetFrontendOpenOrdersRequest r => BuildInfoRequest(new { type = "frontendOpenOrders", user = GetUser(r) }),
            GetPositionsRequest r => BuildInfoRequest(new { type = "clearinghouseState", user = GetUser(r) }),
            GetBalancesRequest r => BuildInfoRequest(new { type = "clearinghouseState", user = GetUser(r) }),
            GetFillsRequest r => BuildInfoRequest(new { type = "userFills", user = GetUser(r) }),
            GetFillsByTimeRequest r => BuildInfoRequest(r.EndTime.HasValue
                ? new { type = "userFillsByTime", user = GetUser(r), startTime = r.StartTime, endTime = r.EndTime.Value, aggregateByTime = r.AggregateByTime }
                : (object)new { type = "userFillsByTime", user = GetUser(r), startTime = r.StartTime, aggregateByTime = r.AggregateByTime }),
            GetOrderStatusRequest r => BuildInfoRequest(new { type = "orderStatus", user = GetUser(r), oid = long.Parse(r.OrderId) }),
            GetHistoricalOrdersRequest r => BuildInfoRequest(new { type = "historicalOrders", user = GetUser(r) }),
            GetUserFundingRequest r => BuildInfoRequest(r.EndTime.HasValue
                ? new { type = "userFunding", user = GetUser(r), startTime = r.StartTime, endTime = r.EndTime.Value }
                : (object)new { type = "userFunding", user = GetUser(r), startTime = r.StartTime }),
            GetUserRateLimitRequest r => BuildInfoRequest(new { type = "userRateLimit", user = GetUser(r) }),
            GetUserFeesRequest r => BuildInfoRequest(new { type = "userFees", user = GetUser(r) }),
            GetSubAccountsRequest r => BuildInfoRequest(new { type = "subAccounts", user = GetUser(r) }),
            GetActiveAssetDataRequest r => BuildInfoRequest(new { type = "activeAssetData", user = GetUser(r), coin = r.Symbol }),
            GetPortfolioRequest r => BuildInfoRequest(new { type = "portfolio", user = GetUser(r) }),
            GetSpotMetaRequest r => BuildInfoRequest(r.IncludeAssetCtxs ? new { type = "spotMetaAndAssetCtxs" } : (object)new { type = "spotMeta" }),
            GetSpotBalancesRequest r => BuildInfoRequest(new { type = "spotClearinghouseState", user = GetUser(r) }),
            _ => throw new NotSupportedException($"Operation {request.Operation} not supported for Hyperliquid REST")
        };
    }

    public TResponse FromExchangeResponse<TResponse>(UnifiedRestRequest<TResponse> request, TransportRestResponse response)
    {
        return request switch
        {
            GetCandlesRequest => (TResponse)(object)ParseCandlesResponse(response.Body),
            GetL2BookRequest r => (TResponse)(object)ParseL2BookResponse(response.Body, r.Symbol),
            GetAllMidsRequest => (TResponse)(object)ParseAllMidsResponse(response.Body),
            GetMetaRequest r => (TResponse)(object)ParseMetaResponse(response.Body, r.IncludeAssetCtxs),
            GetFundingHistoryRequest => (TResponse)(object)ParseFundingHistoryResponse(response.Body),
            GetPredictedFundingsRequest => (TResponse)(object)ParsePredictedFundingsResponse(response.Body),
            GetOpenOrdersRequest => (TResponse)(object)ParseOpenOrdersResponse(response.Body),
            GetFrontendOpenOrdersRequest => (TResponse)(object)ParseFrontendOpenOrdersResponse(response.Body),
            GetPositionsRequest => (TResponse)(object)ParsePositionsResponse(response.Body),
            GetBalancesRequest => (TResponse)(object)ParseBalancesResponse(response.Body),
            GetFillsRequest => (TResponse)(object)ParseFillsResponse(response.Body),
            GetFillsByTimeRequest => (TResponse)(object)ParseFillsResponse(response.Body),
            GetOrderStatusRequest => (TResponse)(object)ParseOrderStatusResponse(response.Body),
            GetHistoricalOrdersRequest => (TResponse)(object)ParseHistoricalOrdersResponse(response.Body),
            GetUserFundingRequest => (TResponse)(object)ParseUserFundingResponse(response.Body),
            GetUserRateLimitRequest => (TResponse)(object)ParseUserRateLimitResponse(response.Body),
            GetUserFeesRequest => (TResponse)(object)ParseUserFeesResponse(response.Body),
            GetSubAccountsRequest => (TResponse)(object)ParseSubAccountsResponse(response.Body),
            GetActiveAssetDataRequest => (TResponse)(object)ParseActiveAssetDataResponse(response.Body),
            GetPortfolioRequest => (TResponse)(object)ParsePortfolioResponse(response.Body),
            GetSpotMetaRequest r => (TResponse)(object)ParseSpotMetaResponse(response.Body, r.IncludeAssetCtxs),
            GetSpotBalancesRequest => (TResponse)(object)ParseSpotBalancesResponse(response.Body),
            _ => throw new NotSupportedException($"Response parsing not supported for {request.Operation}")
        };
    }

    // ─── Request builders ────────────────────────────────────

    private static TransportRestRequest BuildInfoRequest(object body) => new()
    {
        Method = HttpMethod.Post, Path = "/info",
        Body = JsonSerializer.Serialize(body), ContentType = "application/json"
    };

    private static TransportRestRequest BuildL2BookRequest(GetL2BookRequest r)
    {
        object body = (r.SignificantFigures, r.Mantissa) switch
        {
            (not null, not null) => new { type = "l2Book", coin = r.Symbol, nSigFigs = r.SignificantFigures, mantissa = r.Mantissa },
            (not null, _) => new { type = "l2Book", coin = r.Symbol, nSigFigs = r.SignificantFigures },
            _ => new { type = "l2Book", coin = r.Symbol }
        };
        return BuildInfoRequest(body);
    }

    private static string GetUser<T>(UnifiedRestRequest<T> r) =>
        r.Params?.GetValueOrDefault("user") ?? throw new InvalidOperationException("user param required");

    // ─── Response parsers ────────────────────────────────────

    private static GetCandlesResponse ParseCandlesResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var candles = doc.RootElement.EnumerateArray().Select(c => new CandleEntry
        {
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(c.GetProperty("t").GetInt64()),
            Open = Flexi(c.GetProperty("o")),
            High = Flexi(c.GetProperty("h")),
            Low = Flexi(c.GetProperty("l")),
            Close = Flexi(c.GetProperty("c")),
            Volume = Flexi(c.GetProperty("v"))
        }).ToArray();
        return new GetCandlesResponse { Candles = candles };
    }

    private static GetL2BookResponse ParseL2BookResponse(string body, string symbol)
    {
        using var doc = JsonDocument.Parse(body);
        var levels = doc.RootElement.GetProperty("levels");
        return new GetL2BookResponse
        {
            Symbol = symbol,
            Bids = ParseLevels(levels[0]),
            Asks = ParseLevels(levels[1])
        };
    }

    private static GetAllMidsResponse ParseAllMidsResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var mids = doc.RootElement.EnumerateObject().Select(p => new MidPrice
        {
            Symbol = p.Name,
            Mid = decimal.Parse(p.Value.GetString()!, CultureInfo.InvariantCulture)
        }).ToArray();
        return new GetAllMidsResponse { Mids = mids };
    }

    private static GetMetaResponse ParseMetaResponse(string body, bool includeCtx)
    {
        using var doc = JsonDocument.Parse(body);

        var metaRoot = includeCtx ? doc.RootElement[0] : doc.RootElement;
        var universe = metaRoot.GetProperty("universe");
        var assets = universe.EnumerateArray().Select(a => new AssetMeta
        {
            Name = a.GetProperty("name").GetString()!,
            SzDecimals = a.GetProperty("szDecimals").GetInt32(),
            MaxLeverage = a.TryGetProperty("maxLeverage", out var ml) ? ml.GetInt32() : null
        }).ToArray();

        AssetContext[]? ctxs = null;
        if (includeCtx && doc.RootElement.GetArrayLength() > 1)
        {
            ctxs = doc.RootElement[1].EnumerateArray().Select((ctx, i) => new AssetContext
            {
                Symbol = i < assets.Length ? assets[i].Name : $"ASSET_{i}",
                MarkPrice = decimal.Parse(ctx.GetProperty("markPx").GetString()!, CultureInfo.InvariantCulture),
                FundingRate = decimal.Parse(ctx.GetProperty("funding").GetString()!, CultureInfo.InvariantCulture),
                OpenInterest = decimal.Parse(ctx.GetProperty("openInterest").GetString()!, CultureInfo.InvariantCulture),
                PrevDayPrice = ctx.TryGetProperty("prevDayPx", out var p) ? decimal.Parse(p.GetString()!, CultureInfo.InvariantCulture) : null,
                DayNotionalVolume = ctx.TryGetProperty("dayNtlVlm", out var d) ? decimal.Parse(d.GetString()!, CultureInfo.InvariantCulture) : null
            }).ToArray();
        }

        return new GetMetaResponse { Assets = assets, AssetContexts = ctxs };
    }

    private static GetFundingHistoryResponse ParseFundingHistoryResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var history = doc.RootElement.EnumerateArray().Select(e => new FundingHistoryEntry
        {
            Symbol = e.GetProperty("coin").GetString()!,
            FundingRate = decimal.Parse(e.GetProperty("fundingRate").GetString()!, CultureInfo.InvariantCulture),
            Premium = decimal.Parse(e.GetProperty("premium").GetString()!, CultureInfo.InvariantCulture),
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(e.GetProperty("time").GetInt64())
        }).ToArray();
        return new GetFundingHistoryResponse { History = history };
    }

    private static GetPredictedFundingsResponse ParsePredictedFundingsResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var predictions = new List<PredictedFundingEntry>();
        foreach (var outer in doc.RootElement.EnumerateArray())
        {
            // Each entry is [coin, [[venue, fundingRate, nextFundingTime?]]]
            if (outer.GetArrayLength() < 2) continue;
            var coin = outer[0].GetString()!;
            foreach (var inner in outer[1].EnumerateArray())
            {
                predictions.Add(new PredictedFundingEntry
                {
                    Symbol = coin,
                    Venue = inner[0].GetString()!,
                    FundingRate = decimal.Parse(inner[1].GetString()!, CultureInfo.InvariantCulture),
                    NextFundingTime = inner.GetArrayLength() > 2 ? DateTimeOffset.FromUnixTimeMilliseconds(inner[2].GetInt64()) : null
                });
            }
        }
        return new GetPredictedFundingsResponse { Predictions = predictions.ToArray() };
    }

    private static GetOpenOrdersResponse ParseOpenOrdersResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var orders = doc.RootElement.EnumerateArray().Select(ParseBasicOrder).ToArray();
        return new GetOpenOrdersResponse { Orders = orders };
    }

    private static GetFrontendOpenOrdersResponse ParseFrontendOpenOrdersResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var orders = doc.RootElement.EnumerateArray().Select(o =>
        {
            var entry = ParseBasicOrder(o);
            return new UserOrderEntry
            {
                OrderId = entry.OrderId, ClientOrderId = entry.ClientOrderId, Symbol = entry.Symbol,
                Side = entry.Side, OrderType = entry.OrderType, Price = entry.Price, Size = entry.Size,
                FilledSize = entry.FilledSize, Status = entry.Status, Timestamp = entry.Timestamp,
                ReduceOnly = o.TryGetProperty("reduceOnly", out var ro) && ro.GetBoolean(),
                IsTrigger = o.TryGetProperty("isTrigger", out var it) && it.GetBoolean(),
                TriggerPrice = o.TryGetProperty("triggerPx", out var tp) && tp.ValueKind == JsonValueKind.String
                    ? decimal.Parse(tp.GetString()!, CultureInfo.InvariantCulture) : null,
                TriggerCondition = o.TryGetProperty("triggerCondition", out var tc) ? tc.GetString() : null,
                OriginalSize = o.TryGetProperty("origSz", out var os)
                    ? decimal.Parse(os.GetString()!, CultureInfo.InvariantCulture) : null
            };
        }).ToArray();
        return new GetFrontendOpenOrdersResponse { Orders = orders };
    }

    private static GetPositionsResponse ParsePositionsResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var assetPositions = doc.RootElement.GetProperty("assetPositions");
        var positions = assetPositions.EnumerateArray().Select(ap =>
        {
            var pos = ap.GetProperty("position");
            var szi = decimal.Parse(pos.GetProperty("szi").GetString()!, CultureInfo.InvariantCulture);
            return new PositionEntry
            {
                Symbol = pos.GetProperty("coin").GetString()!,
                Side = szi >= 0 ? "long" : "short",
                Size = Math.Abs(szi),
                EntryPrice = decimal.Parse(pos.GetProperty("entryPx").GetString() ?? "0", CultureInfo.InvariantCulture),
                UnrealizedPnl = decimal.Parse(pos.GetProperty("unrealizedPnl").GetString() ?? "0", CultureInfo.InvariantCulture),
                LiquidationPrice = pos.TryGetProperty("liquidationPx", out var lp) && lp.GetString() is not null
                    ? decimal.Parse(lp.GetString()!, CultureInfo.InvariantCulture) : null,
                Leverage = pos.TryGetProperty("leverage", out var lev) ? lev.GetProperty("value").GetDecimal() : null,
                MarginType = pos.TryGetProperty("leverage", out var lt) ? lt.GetProperty("type").GetString() : null,
                ReturnOnEquity = pos.TryGetProperty("returnOnEquity", out var roe) ? decimal.Parse(roe.GetString()!, CultureInfo.InvariantCulture) : null
            };
        }).ToArray();
        return new GetPositionsResponse { Positions = positions };
    }

    private static GetBalancesResponse ParseBalancesResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var margin = doc.RootElement.TryGetProperty("crossMarginSummary", out var cms) ? cms
            : doc.RootElement.GetProperty("marginSummary");

        var accountValue = decimal.Parse(margin.GetProperty("accountValue").GetString()!, CultureInfo.InvariantCulture);
        var totalMarginUsed = decimal.Parse(margin.GetProperty("totalMarginUsed").GetString()!, CultureInfo.InvariantCulture);
        var withdrawable = doc.RootElement.TryGetProperty("withdrawable", out var w)
            ? decimal.Parse(w.GetString()!, CultureInfo.InvariantCulture) : accountValue - totalMarginUsed;

        return new GetBalancesResponse
        {
            Balances = [new BalanceEntry { Asset = "USDC", Total = accountValue, Available = withdrawable }],
            AccountValue = accountValue,
            Withdrawable = withdrawable
        };
    }

    private static GetFillsResponse ParseFillsResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var fills = doc.RootElement.EnumerateArray().Select(f => new FillEntry
        {
            TradeId = f.TryGetProperty("tid", out var tid) ? tid.GetInt64().ToString() : "",
            OrderId = f.GetProperty("oid").GetInt64().ToString(),
            Symbol = f.GetProperty("coin").GetString()!,
            Side = f.GetProperty("side").GetString()!.ToLowerInvariant(),
            Price = decimal.Parse(f.GetProperty("px").GetString()!, CultureInfo.InvariantCulture),
            Size = decimal.Parse(f.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture),
            Fee = decimal.Parse(f.GetProperty("fee").GetString()!, CultureInfo.InvariantCulture),
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(f.GetProperty("time").GetInt64()),
            Direction = f.TryGetProperty("dir", out var dir) ? dir.GetString() : null,
            ClosedPnl = f.TryGetProperty("closedPnl", out var cp) ? decimal.Parse(cp.GetString()!, CultureInfo.InvariantCulture) : null,
            FeeToken = f.TryGetProperty("feeToken", out var ft) ? ft.GetString() : null
        }).ToArray();
        return new GetFillsResponse { Fills = fills };
    }

    private static GetOrderStatusResponse ParseOrderStatusResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var status = doc.RootElement.GetProperty("status").GetString()!;
        UserOrderEntry? order = null;
        if (doc.RootElement.TryGetProperty("order", out var orderEl) && orderEl.ValueKind == JsonValueKind.Object)
        {
            var o = orderEl.GetProperty("order");
            order = new UserOrderEntry
            {
                OrderId = o.GetProperty("oid").GetInt64().ToString(),
                ClientOrderId = o.TryGetProperty("cloid", out var c) ? c.GetString() : null,
                Symbol = o.GetProperty("coin").GetString()!,
                Side = o.GetProperty("side").GetString()!.ToLowerInvariant(),
                OrderType = "limit",
                Price = decimal.Parse(o.GetProperty("limitPx").GetString()!, CultureInfo.InvariantCulture),
                Size = decimal.Parse(o.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture),
                FilledSize = 0m,
                Status = orderEl.GetProperty("status").GetString()!,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(orderEl.GetProperty("statusTimestamp").GetInt64())
            };
        }
        return new GetOrderStatusResponse { Status = status, Order = order };
    }

    private static GetHistoricalOrdersResponse ParseHistoricalOrdersResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var orders = doc.RootElement.EnumerateArray().Select(item =>
        {
            var o = item.GetProperty("order");
            return new UserOrderEntry
            {
                OrderId = o.GetProperty("oid").GetInt64().ToString(),
                ClientOrderId = o.TryGetProperty("cloid", out var c) ? c.GetString() : null,
                Symbol = o.GetProperty("coin").GetString()!,
                Side = o.GetProperty("side").GetString()!.ToLowerInvariant(),
                OrderType = "limit",
                Price = decimal.Parse(o.GetProperty("limitPx").GetString()!, CultureInfo.InvariantCulture),
                Size = decimal.Parse(o.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture),
                FilledSize = 0m,
                Status = item.GetProperty("status").GetString()!,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(item.GetProperty("statusTimestamp").GetInt64())
            };
        }).ToArray();
        return new GetHistoricalOrdersResponse { Orders = orders };
    }

    private static GetUserFundingResponse ParseUserFundingResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var fundings = doc.RootElement.EnumerateArray().Select(f =>
        {
            var delta = f.GetProperty("delta");
            return new UserFundingEntry
            {
                Symbol = delta.GetProperty("coin").GetString()!,
                FundingRate = decimal.Parse(delta.GetProperty("fundingRate").GetString()!, CultureInfo.InvariantCulture),
                Payment = decimal.Parse(delta.GetProperty("usdc").GetString()!, CultureInfo.InvariantCulture),
                PositionSize = decimal.Parse(delta.GetProperty("szi").GetString()!, CultureInfo.InvariantCulture),
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(f.GetProperty("time").GetInt64()),
                Hash = f.TryGetProperty("hash", out var h) ? h.GetString() : null
            };
        }).ToArray();
        return new GetUserFundingResponse { Fundings = fundings };
    }

    private static GetUserRateLimitResponse ParseUserRateLimitResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var r = doc.RootElement;
        return new GetUserRateLimitResponse
        {
            CumulativeVolume = decimal.Parse(r.GetProperty("cumVlm").GetString()!, CultureInfo.InvariantCulture),
            RequestsUsed = r.GetProperty("nRequestsUsed").GetInt32(),
            RequestsCap = r.GetProperty("nRequestsCap").GetInt32()
        };
    }

    private static GetUserFeesResponse ParseUserFeesResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var r = doc.RootElement;
        return new GetUserFeesResponse
        {
            DailyVolume = decimal.Parse(r.GetProperty("dailyUserVlm").GetString()!, CultureInfo.InvariantCulture),
            MakerRate = decimal.Parse(r.GetProperty("userAddRate").GetString()!, CultureInfo.InvariantCulture),
            TakerRate = decimal.Parse(r.GetProperty("userCrossRate").GetString()!, CultureInfo.InvariantCulture)
        };
    }

    private static GetSubAccountsResponse ParseSubAccountsResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var subs = doc.RootElement.EnumerateArray().Select(s => new SubAccountEntry
        {
            Name = s.GetProperty("name").GetString()!,
            Address = s.GetProperty("subAccountUser").GetString()!
        }).ToArray();
        return new GetSubAccountsResponse { SubAccounts = subs };
    }

    private static GetActiveAssetDataResponse ParseActiveAssetDataResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var r = doc.RootElement;
        return new GetActiveAssetDataResponse
        {
            Leverage = Flexi(r.GetProperty("leverage")),
            MarkPrice = decimal.Parse(r.GetProperty("markPx").GetString()!, CultureInfo.InvariantCulture),
            MaxTradeSizeLong = r.TryGetProperty("maxTradeSzs", out var mts) && mts.GetArrayLength() > 0
                ? decimal.Parse(mts[0].GetString()!, CultureInfo.InvariantCulture) : null,
            MaxTradeSizeShort = r.TryGetProperty("maxTradeSzs", out _) && mts.GetArrayLength() > 1
                ? decimal.Parse(mts[1].GetString()!, CultureInfo.InvariantCulture) : null
        };
    }

    private static GetPortfolioResponse ParsePortfolioResponse(string body)
    {
        // Portfolio response is complex; return a simplified version
        using var doc = JsonDocument.Parse(body);
        return new GetPortfolioResponse();
    }

    private static GetSpotMetaResponse ParseSpotMetaResponse(string body, bool includeCtx)
    {
        using var doc = JsonDocument.Parse(body);
        var metaRoot = includeCtx ? doc.RootElement[0] : doc.RootElement;
        var tokens = metaRoot.GetProperty("tokens").EnumerateArray().Select(t => new SpotTokenMeta
        {
            Name = t.GetProperty("name").GetString()!,
            TokenId = t.GetProperty("index").GetInt32(),
            Decimals = t.TryGetProperty("weiDecimals", out var d) ? d.GetInt32() : 8
        }).ToArray();

        SpotAssetContext[]? ctxs = null;
        if (includeCtx && doc.RootElement.GetArrayLength() > 1)
        {
            ctxs = doc.RootElement[1].EnumerateArray().Select((ctx, i) => new SpotAssetContext
            {
                Symbol = i < tokens.Length ? tokens[i].Name : $"TOKEN_{i}",
                MarkPrice = decimal.Parse(ctx.GetProperty("markPx").GetString()!, CultureInfo.InvariantCulture),
                PrevDayPrice = ctx.TryGetProperty("prevDayPx", out var p) ? decimal.Parse(p.GetString()!, CultureInfo.InvariantCulture) : null,
                DayNotionalVolume = ctx.TryGetProperty("dayNtlVlm", out var d) ? decimal.Parse(d.GetString()!, CultureInfo.InvariantCulture) : null
            }).ToArray();
        }

        return new GetSpotMetaResponse { Tokens = tokens, AssetContexts = ctxs };
    }

    private static GetSpotBalancesResponse ParseSpotBalancesResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var balances = doc.RootElement.GetProperty("balances").EnumerateArray().Select(b => new SpotBalanceEntry
        {
            Asset = b.GetProperty("coin").GetString()!,
            TokenId = b.GetProperty("token").GetInt32(),
            Total = decimal.Parse(b.GetProperty("total").GetString()!, CultureInfo.InvariantCulture),
            Hold = decimal.Parse(b.GetProperty("hold").GetString()!, CultureInfo.InvariantCulture),
            EntryNotional = b.TryGetProperty("entryNtl", out var en) ? decimal.Parse(en.GetString()!, CultureInfo.InvariantCulture) : null
        }).ToArray();
        return new GetSpotBalancesResponse { Balances = balances };
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static UserOrderEntry ParseBasicOrder(JsonElement o) => new()
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
    };

    private static PriceLevel[] ParseLevels(JsonElement arr) =>
        arr.EnumerateArray().Select(level => new PriceLevel
        {
            Price = decimal.Parse(level.GetProperty("px").GetString()!, CultureInfo.InvariantCulture),
            Size = decimal.Parse(level.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture)
        }).ToArray();

    private static decimal Flexi(JsonElement el) =>
        el.ValueKind == JsonValueKind.String
            ? decimal.Parse(el.GetString()!, CultureInfo.InvariantCulture)
            : el.GetDecimal();
}
