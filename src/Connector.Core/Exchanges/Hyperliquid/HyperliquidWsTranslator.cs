using System.Globalization;
using System.Text.Json;
using Connector.Core.Abstractions;
using Connector.Core.Contracts;
using Connector.Core.Transport;
using Microsoft.Extensions.Logging;

namespace Connector.Core.Exchanges.Hyperliquid;

/// <summary>
/// Translates unified WS requests to Hyperliquid protocol and HL messages to unified events.
/// Supports all 19 HL WebSocket subscription types.
/// </summary>
public sealed class HyperliquidWsTranslator : IWsTranslator
{
    private readonly ILogger _logger;
    private readonly bool _includeRaw;
    private readonly string? _userAddress;

    public HyperliquidWsTranslator(ILogger logger, bool includeRaw = false, string? userAddress = null)
    {
        _logger = logger;
        _includeRaw = includeRaw;
        _userAddress = userAddress;
    }

    private string RequireUser(string channel) =>
        _userAddress ?? throw new InvalidOperationException($"userAddress required for {channel}");

    // ─── Subscribe ───────────────────────────────────────────

    public IEnumerable<TransportWsMessage> ToExchangeSubscribe(UnifiedWsSubscribeRequest request)
    {
        foreach (var symbol in request.Symbols)
        {
            var sub = BuildSubscription("subscribe", request.Channel, symbol, request.Interval, request.Options);
            yield return new TransportWsMessage { Payload = JsonSerializer.Serialize(sub) };

            if (IsUserScoped(request.Channel))
                yield break;
        }
    }

    public IEnumerable<TransportWsMessage> ToExchangeUnsubscribe(UnifiedWsUnsubscribeRequest request)
    {
        foreach (var symbol in request.Symbols)
        {
            var sub = BuildSubscription("unsubscribe", request.Channel, symbol, null, null);
            yield return new TransportWsMessage { Payload = JsonSerializer.Serialize(sub) };

            if (IsUserScoped(request.Channel))
                yield break;
        }
    }

    private object BuildSubscription(string method, UnifiedWsChannel channel, string symbol, string? interval, Dictionary<string, string>? options)
    {
        var dex = options?.GetValueOrDefault("dex");
        object subscription = channel switch
        {
            UnifiedWsChannel.Trades => new { type = "trades", coin = symbol },
            UnifiedWsChannel.OrderBookL1 => new { type = "bbo", coin = symbol },
            UnifiedWsChannel.OrderBookL2 => new { type = "l2Book", coin = symbol },
            UnifiedWsChannel.Candles => new { type = "candle", coin = symbol, interval = interval ?? "1m" },
            UnifiedWsChannel.AllMids => (object)new { type = "allMids" },
            UnifiedWsChannel.ActiveAssetCtx => new { type = "activeAssetCtx", coin = symbol },
            UnifiedWsChannel.UserOrders => new { type = "orderUpdates", user = RequireUser("orderUpdates") },
            UnifiedWsChannel.Fills => new { type = "userFills", user = RequireUser("userFills") },
            UnifiedWsChannel.Positions => new { type = "clearinghouseState", user = RequireUser("clearinghouseState") },
            UnifiedWsChannel.Balances => new { type = "clearinghouseState", user = RequireUser("clearinghouseState") },
            UnifiedWsChannel.UserFundings => new { type = "userFundings", user = RequireUser("userFundings") },
            UnifiedWsChannel.Ledger => new { type = "userNonFundingLedgerUpdates", user = RequireUser("userNonFundingLedgerUpdates") },
            UnifiedWsChannel.Notifications => new { type = "notification", user = RequireUser("notification") },
            UnifiedWsChannel.OpenOrders => new { type = "openOrders", user = RequireUser("openOrders") },
            UnifiedWsChannel.TwapState => new { type = "twapStates", user = RequireUser("twapStates") },
            UnifiedWsChannel.TwapSliceFills => new { type = "userTwapSliceFills", user = RequireUser("userTwapSliceFills") },
            UnifiedWsChannel.TwapHistory => new { type = "userTwapHistory", user = RequireUser("userTwapHistory") },
            UnifiedWsChannel.ActiveAssetData => new { type = "activeAssetData", user = RequireUser("activeAssetData"), coin = symbol },
            UnifiedWsChannel.WebData => new { type = "webData3", user = RequireUser("webData3") },
            _ => throw new NotSupportedException($"Channel {channel} not supported for Hyperliquid")
        };
        return new { method, subscription };
    }

    private static bool IsUserScoped(UnifiedWsChannel ch) => ch is
        UnifiedWsChannel.UserOrders or UnifiedWsChannel.Fills or
        UnifiedWsChannel.Positions or UnifiedWsChannel.Balances or
        UnifiedWsChannel.UserFundings or UnifiedWsChannel.Ledger or
        UnifiedWsChannel.Notifications or UnifiedWsChannel.OpenOrders or
        UnifiedWsChannel.TwapState or UnifiedWsChannel.TwapSliceFills or
        UnifiedWsChannel.TwapHistory or UnifiedWsChannel.WebData;

    // ─── Parse inbound ───────────────────────────────────────

    public IEnumerable<UnifiedWsEvent> FromExchangeMessage(TransportWsInbound inbound)
    {
        using var doc = JsonDocument.Parse(inbound.Payload);
        var root = doc.RootElement;

        if (!root.TryGetProperty("channel", out var channelProp))
        {
            _logger.LogDebug("Skipping non-channel message");
            yield break;
        }

        var channel = channelProp.GetString();
        if (!root.TryGetProperty("data", out var data))
            yield break;

        var raw = _includeRaw ? new RawPayload { RawJson = inbound.Payload, ExchangeMessageType = channel } : null;

        switch (channel)
        {
            case "trades":
                foreach (var evt in ParseTrades(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "bbo":
                foreach (var evt in ParseBbo(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "l2Book":
                foreach (var evt in ParseL2Book(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "candle":
                foreach (var evt in ParseCandle(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "allMids":
                foreach (var evt in ParseAllMids(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "activeAssetCtx":
                foreach (var evt in ParseActiveAssetCtx(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "orderUpdates":
                foreach (var evt in ParseOrderUpdates(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "userFills":
                foreach (var evt in ParseUserFills(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "clearinghouseState":
                foreach (var evt in ParseClearinghouseState(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "userFundings":
                foreach (var evt in ParseUserFundings(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "userNonFundingLedgerUpdates":
                foreach (var evt in ParseLedger(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "notification":
                foreach (var evt in ParseNotification(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "openOrders":
                foreach (var evt in ParseOpenOrders(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "twapStates":
                foreach (var evt in ParseTwapStates(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "userTwapSliceFills":
                foreach (var evt in ParseTwapSliceFills(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "userTwapHistory":
                foreach (var evt in ParseTwapHistory(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "activeAssetData":
                foreach (var evt in ParseActiveAssetData(data, inbound.ReceivedAt, raw)) yield return evt;
                break;
            case "webData3":
                yield return new WebDataEvent
                {
                    Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.WebData,
                    Symbol = "*", ReceivedAt = inbound.ReceivedAt, Data = data.GetRawText(), Raw = raw
                };
                break;
            case "subscriptionResponse":
                _logger.LogDebug("Subscription confirmed");
                break;
            default:
                _logger.LogDebug("Unknown channel: {Channel}", channel);
                break;
        }
    }

    // ─── Parsers ─────────────────────────────────────────────

    private IEnumerable<TradesEvent> ParseTrades(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        if (data.ValueKind != JsonValueKind.Array) yield break;

        var trades = new List<TradeEntry>();
        string? coin = null;

        foreach (var t in data.EnumerateArray())
        {
            coin ??= t.GetProperty("coin").GetString();
            trades.Add(new TradeEntry
            {
                TradeId = t.GetProperty("tid").GetInt64().ToString(),
                Price = decimal.Parse(t.GetProperty("px").GetString()!, CultureInfo.InvariantCulture),
                Size = decimal.Parse(t.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture),
                Side = t.GetProperty("side").GetString()!.ToLowerInvariant(),
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(t.GetProperty("time").GetInt64())
            });
        }

        if (coin is not null && trades.Count > 0)
        {
            yield return new TradesEvent
            {
                Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.Trades,
                Symbol = coin, ReceivedAt = receivedAt, Trades = trades.ToArray(), Raw = raw
            };
        }
    }

    private IEnumerable<OrderBookL1Event> ParseBbo(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        var coin = data.GetProperty("coin").GetString()!;
        var bid = data.GetProperty("bid");
        var ask = data.GetProperty("ask");

        yield return new OrderBookL1Event
        {
            Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.OrderBookL1,
            Symbol = coin, ReceivedAt = receivedAt,
            BestBid = new PriceLevel
            {
                Price = decimal.Parse(bid.GetProperty("px").GetString()!, CultureInfo.InvariantCulture),
                Size = decimal.Parse(bid.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture)
            },
            BestAsk = new PriceLevel
            {
                Price = decimal.Parse(ask.GetProperty("px").GetString()!, CultureInfo.InvariantCulture),
                Size = decimal.Parse(ask.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture)
            },
            Raw = raw
        };
    }

    private IEnumerable<OrderBookL2Event> ParseL2Book(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        var coin = data.GetProperty("coin").GetString()!;
        var levels = data.GetProperty("levels");

        yield return new OrderBookL2Event
        {
            Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.OrderBookL2,
            Symbol = coin, ReceivedAt = receivedAt, IsSnapshot = true,
            Bids = ParseLevels(levels[0]), Asks = ParseLevels(levels[1]), Raw = raw
        };
    }

    private IEnumerable<CandleEvent> ParseCandle(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        var items = data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray().ToList()
            : [data];

        foreach (var c in items)
        {
            var coin = c.GetProperty("s").GetString()!;
            yield return new CandleEvent
            {
                Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.Candles,
                Symbol = coin, ReceivedAt = receivedAt,
                Candle = new CandleEntry
                {
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(c.GetProperty("t").GetInt64()),
                    Open = ParseDecimalFlexible(c.GetProperty("o")),
                    High = ParseDecimalFlexible(c.GetProperty("h")),
                    Low = ParseDecimalFlexible(c.GetProperty("l")),
                    Close = ParseDecimalFlexible(c.GetProperty("c")),
                    Volume = ParseDecimalFlexible(c.GetProperty("v"))
                },
                Raw = raw
            };
        }
    }

    private IEnumerable<AllMidsEvent> ParseAllMids(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        // data = { "mids": { "BTC": "50000.0", "ETH": "3000.0", ... } }
        if (!data.TryGetProperty("mids", out var mids)) yield break;

        var entries = new List<AllMidsEntry>();
        foreach (var prop in mids.EnumerateObject())
        {
            entries.Add(new AllMidsEntry
            {
                Symbol = prop.Name,
                Mid = decimal.Parse(prop.Value.GetString()!, CultureInfo.InvariantCulture)
            });
        }

        yield return new AllMidsEvent
        {
            Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.AllMids,
            Symbol = "*", ReceivedAt = receivedAt, Mids = entries.ToArray(), Raw = raw
        };
    }

    private IEnumerable<ActiveAssetCtxEvent> ParseActiveAssetCtx(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        // data = { "coin": "BTC", "ctx": { "funding": "0.0001", "markPx": "50000", "openInterest": "1000", ... } }
        var coin = data.GetProperty("coin").GetString()!;
        var ctx = data.GetProperty("ctx");

        yield return new ActiveAssetCtxEvent
        {
            Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.ActiveAssetCtx,
            Symbol = coin, ReceivedAt = receivedAt,
            FundingRate = decimal.Parse(ctx.GetProperty("funding").GetString()!, CultureInfo.InvariantCulture),
            MarkPrice = decimal.Parse(ctx.GetProperty("markPx").GetString()!, CultureInfo.InvariantCulture),
            OpenInterest = decimal.Parse(ctx.GetProperty("openInterest").GetString()!, CultureInfo.InvariantCulture),
            OraclePrice = ctx.TryGetProperty("oraclePx", out var op) && op.ValueKind == JsonValueKind.String
                ? decimal.Parse(op.GetString()!, CultureInfo.InvariantCulture) : null,
            PrevDayPrice = ctx.TryGetProperty("prevDayPx", out var pdp) && pdp.ValueKind == JsonValueKind.String
                ? decimal.Parse(pdp.GetString()!, CultureInfo.InvariantCulture) : null,
            DayNotionalVolume = ctx.TryGetProperty("dayNtlVlm", out var dnv) && dnv.ValueKind == JsonValueKind.String
                ? decimal.Parse(dnv.GetString()!, CultureInfo.InvariantCulture) : null,
            Raw = raw
        };
    }

    private IEnumerable<UserOrderEvent> ParseOrderUpdates(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        if (data.ValueKind != JsonValueKind.Array) yield break;

        var orders = new List<UserOrderEntry>();
        string? firstCoin = null;

        foreach (var item in data.EnumerateArray())
        {
            var order = item.GetProperty("order");
            var coin = order.GetProperty("coin").GetString()!;
            firstCoin ??= coin;

            orders.Add(new UserOrderEntry
            {
                OrderId = order.GetProperty("oid").GetInt64().ToString(),
                ClientOrderId = order.TryGetProperty("cloid", out var cloid) ? cloid.GetString() : null,
                Symbol = coin,
                Side = order.GetProperty("side").GetString()!.ToLowerInvariant(),
                OrderType = "limit",
                Price = decimal.Parse(order.GetProperty("limitPx").GetString()!, CultureInfo.InvariantCulture),
                Size = decimal.Parse(order.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture),
                OriginalSize = decimal.Parse(order.GetProperty("origSz").GetString()!, CultureInfo.InvariantCulture),
                FilledSize = decimal.Parse(order.GetProperty("origSz").GetString()!, CultureInfo.InvariantCulture) - decimal.Parse(order.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture),
                Status = item.GetProperty("status").GetString()!,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(item.GetProperty("statusTimestamp").GetInt64())
            });
        }

        if (orders.Count > 0)
        {
            yield return new UserOrderEvent
            {
                Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.UserOrders,
                Symbol = firstCoin!, ReceivedAt = receivedAt, Orders = orders.ToArray(), Raw = raw
            };
        }
    }

    private IEnumerable<FillEvent> ParseUserFills(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        if (!data.TryGetProperty("fills", out var fillsArr)) yield break;
        var isSnapshot = data.TryGetProperty("isSnapshot", out var snap) && snap.GetBoolean();

        var fills = new List<FillEntry>();
        string? firstCoin = null;

        foreach (var f in fillsArr.EnumerateArray())
        {
            var coin = f.GetProperty("coin").GetString()!;
            firstCoin ??= coin;

            fills.Add(new FillEntry
            {
                TradeId = f.TryGetProperty("tid", out var tid) ? tid.GetInt64().ToString() : "",
                OrderId = f.GetProperty("oid").GetInt64().ToString(),
                Symbol = coin,
                Side = f.GetProperty("side").GetString()!.ToLowerInvariant(),
                Price = decimal.Parse(f.GetProperty("px").GetString()!, CultureInfo.InvariantCulture),
                Size = decimal.Parse(f.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture),
                Fee = decimal.Parse(f.GetProperty("fee").GetString()!, CultureInfo.InvariantCulture),
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(f.GetProperty("time").GetInt64()),
                Direction = f.TryGetProperty("dir", out var dir) ? dir.GetString() : null,
                ClosedPnl = f.TryGetProperty("closedPnl", out var cp) ? decimal.Parse(cp.GetString()!, CultureInfo.InvariantCulture) : null,
                StartPosition = f.TryGetProperty("startPosition", out var sp) ? decimal.Parse(sp.GetString()!, CultureInfo.InvariantCulture) : null,
                FeeToken = f.TryGetProperty("feeToken", out var ft) ? ft.GetString() : null
            });
        }

        if (fills.Count > 0)
        {
            yield return new FillEvent
            {
                Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.Fills,
                Symbol = firstCoin!, ReceivedAt = receivedAt, Fills = fills.ToArray(),
                IsSnapshot = isSnapshot, Raw = raw
            };
        }
    }

    private IEnumerable<UnifiedWsEvent> ParseClearinghouseState(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        // Emits both PositionEvent and BalanceEvent from clearinghouseState
        // Positions
        if (data.TryGetProperty("assetPositions", out var assetPositions))
        {
            var positions = new List<PositionEntry>();
            foreach (var ap in assetPositions.EnumerateArray())
            {
                var pos = ap.GetProperty("position");
                var szi = decimal.Parse(pos.GetProperty("szi").GetString()!, CultureInfo.InvariantCulture);
                positions.Add(new PositionEntry
                {
                    Symbol = pos.GetProperty("coin").GetString()!,
                    Side = szi >= 0 ? "long" : "short",
                    Size = Math.Abs(szi),
                    EntryPrice = decimal.Parse(pos.GetProperty("entryPx").GetString() ?? "0", CultureInfo.InvariantCulture),
                    UnrealizedPnl = decimal.Parse(pos.GetProperty("unrealizedPnl").GetString() ?? "0", CultureInfo.InvariantCulture),
                    LiquidationPrice = pos.TryGetProperty("liquidationPx", out var lp) && lp.GetString() is not null
                        ? decimal.Parse(lp.GetString()!, CultureInfo.InvariantCulture) : null,
                    Leverage = pos.TryGetProperty("leverage", out var lev) ? lev.GetProperty("value").GetDecimal() : null,
                    MarginType = pos.TryGetProperty("leverage", out var lt) ? (lt.GetProperty("type").GetString()) : null,
                    ReturnOnEquity = pos.TryGetProperty("returnOnEquity", out var roe) ? decimal.Parse(roe.GetString()!, CultureInfo.InvariantCulture) : null
                });
            }

            if (positions.Count > 0)
            {
                yield return new PositionEvent
                {
                    Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.Positions,
                    Symbol = "*", ReceivedAt = receivedAt, Positions = positions.ToArray(), Raw = raw
                };
            }
        }

        // Balance
        if (data.TryGetProperty("marginSummary", out var margin) || data.TryGetProperty("crossMarginSummary", out margin))
        {
            var accountValue = decimal.Parse(margin.GetProperty("accountValue").GetString()!, CultureInfo.InvariantCulture);
            var totalMarginUsed = decimal.Parse(margin.GetProperty("totalMarginUsed").GetString()!, CultureInfo.InvariantCulture);
            var totalRawUsd = margin.TryGetProperty("totalRawUsd", out var tru) ? decimal.Parse(tru.GetString()!, CultureInfo.InvariantCulture) : accountValue;
            var withdrawable = data.TryGetProperty("withdrawable", out var w) ? decimal.Parse(w.GetString()!, CultureInfo.InvariantCulture) : accountValue - totalMarginUsed;

            yield return new BalanceEvent
            {
                Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.Balances,
                Symbol = "*", ReceivedAt = receivedAt,
                Balances = [new BalanceEntry { Asset = "USDC", Total = accountValue, Available = withdrawable }],
                AccountValue = accountValue, TotalMarginUsed = totalMarginUsed,
                TotalRawUsd = totalRawUsd, Withdrawable = withdrawable, Raw = raw
            };
        }
    }

    private IEnumerable<UserFundingEvent> ParseUserFundings(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        var isSnapshot = data.TryGetProperty("isSnapshot", out var snap) && snap.GetBoolean();

        JsonElement fundingsArr;
        if (data.ValueKind == JsonValueKind.Array)
            fundingsArr = data;
        else if (!data.TryGetProperty("fundings", out fundingsArr))
            yield break;

        var fundings = new List<FundingEntry>();
        foreach (var f in fundingsArr.EnumerateArray())
        {
            var delta = f.GetProperty("delta");
            fundings.Add(new FundingEntry
            {
                Symbol = delta.GetProperty("coin").GetString()!,
                FundingRate = decimal.Parse(delta.GetProperty("fundingRate").GetString()!, CultureInfo.InvariantCulture),
                Payment = decimal.Parse(delta.GetProperty("usdc").GetString()!, CultureInfo.InvariantCulture),
                PositionSize = decimal.Parse(delta.GetProperty("szi").GetString()!, CultureInfo.InvariantCulture),
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(f.GetProperty("time").GetInt64())
            });
        }

        if (fundings.Count > 0)
        {
            yield return new UserFundingEvent
            {
                Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.UserFundings,
                Symbol = "*", ReceivedAt = receivedAt, Fundings = fundings.ToArray(),
                IsSnapshot = isSnapshot, Raw = raw
            };
        }
    }

    private IEnumerable<LedgerEvent> ParseLedger(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        var isSnapshot = data.TryGetProperty("isSnapshot", out var snap) && snap.GetBoolean();

        JsonElement ledgerArr;
        if (data.ValueKind == JsonValueKind.Array)
            ledgerArr = data;
        else if (!data.TryGetProperty("ledgerUpdates", out ledgerArr))
            yield break;

        var entries = new List<LedgerEntry>();
        foreach (var item in ledgerArr.EnumerateArray())
        {
            var delta = item.TryGetProperty("delta", out var d) ? d : item;
            entries.Add(new LedgerEntry
            {
                Type = delta.TryGetProperty("type", out var t) ? t.GetString()! : "unknown",
                Amount = delta.TryGetProperty("usdc", out var u) ? decimal.Parse(u.GetString()!, CultureInfo.InvariantCulture) : 0m,
                Timestamp = item.TryGetProperty("time", out var tm) ? DateTimeOffset.FromUnixTimeMilliseconds(tm.GetInt64()) : receivedAt,
                Hash = item.TryGetProperty("hash", out var h) ? h.GetString() : null
            });
        }

        if (entries.Count > 0)
        {
            yield return new LedgerEvent
            {
                Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.Ledger,
                Symbol = "*", ReceivedAt = receivedAt, Entries = entries.ToArray(),
                IsSnapshot = isSnapshot, Raw = raw
            };
        }
    }

    private IEnumerable<NotificationEvent> ParseNotification(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        var msg = data.ValueKind == JsonValueKind.String ? data.GetString()! : data.GetRawText();
        yield return new NotificationEvent
        {
            Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.Notifications,
            Symbol = "*", ReceivedAt = receivedAt, Message = msg, Raw = raw
        };
    }

    private IEnumerable<OpenOrdersEvent> ParseOpenOrders(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        if (data.ValueKind != JsonValueKind.Array) yield break;

        var orders = new List<UserOrderEntry>();
        foreach (var o in data.EnumerateArray())
        {
            orders.Add(new UserOrderEntry
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
            });
        }

        yield return new OpenOrdersEvent
        {
            Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.OpenOrders,
            Symbol = "*", ReceivedAt = receivedAt, Orders = orders.ToArray(), Raw = raw
        };
    }

    private IEnumerable<TwapStateEvent> ParseTwapStates(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        if (data.ValueKind != JsonValueKind.Array) yield break;

        var entries = new List<TwapStateEntry>();
        foreach (var t in data.EnumerateArray())
        {
            entries.Add(new TwapStateEntry
            {
                TwapId = t.GetProperty("twapId").GetInt64().ToString(),
                Symbol = t.GetProperty("coin").GetString()!,
                Side = t.GetProperty("isBuy").GetBoolean() ? "buy" : "sell",
                Size = ParseDecimalFlexible(t.GetProperty("sz")),
                FilledSize = ParseDecimalFlexible(t.GetProperty("executedSz")),
                DurationMinutes = t.GetProperty("minutes").GetInt32(),
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(t.GetProperty("createdTime").GetInt64())
            });
        }

        yield return new TwapStateEvent
        {
            Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.TwapState,
            Symbol = "*", ReceivedAt = receivedAt, Twaps = entries.ToArray(), Raw = raw
        };
    }

    private IEnumerable<TwapSliceFillEvent> ParseTwapSliceFills(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        var isSnapshot = data.TryGetProperty("isSnapshot", out var snap) && snap.GetBoolean();

        JsonElement fillsArr;
        if (data.ValueKind == JsonValueKind.Array)
            fillsArr = data;
        else if (!data.TryGetProperty("fills", out fillsArr))
            yield break;

        var fills = new List<TwapSliceFillEntry>();
        foreach (var f in fillsArr.EnumerateArray())
        {
            fills.Add(new TwapSliceFillEntry
            {
                TwapId = f.TryGetProperty("twapId", out var ti) ? ti.GetInt64().ToString() : "",
                Symbol = f.GetProperty("coin").GetString()!,
                Price = decimal.Parse(f.GetProperty("px").GetString()!, CultureInfo.InvariantCulture),
                Size = decimal.Parse(f.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture),
                Side = f.GetProperty("side").GetString()!.ToLowerInvariant(),
                Fee = decimal.Parse(f.GetProperty("fee").GetString()!, CultureInfo.InvariantCulture),
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(f.GetProperty("time").GetInt64())
            });
        }

        if (fills.Count > 0)
        {
            yield return new TwapSliceFillEvent
            {
                Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.TwapSliceFills,
                Symbol = "*", ReceivedAt = receivedAt, Fills = fills.ToArray(),
                IsSnapshot = isSnapshot, Raw = raw
            };
        }
    }

    private IEnumerable<TwapHistoryEvent> ParseTwapHistory(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        if (data.ValueKind != JsonValueKind.Array) yield break;

        var entries = new List<TwapHistoryEntry>();
        foreach (var t in data.EnumerateArray())
        {
            entries.Add(new TwapHistoryEntry
            {
                TwapId = t.GetProperty("twapId").GetInt64().ToString(),
                Symbol = t.GetProperty("coin").GetString()!,
                Side = t.GetProperty("isBuy").GetBoolean() ? "buy" : "sell",
                Size = ParseDecimalFlexible(t.GetProperty("sz")),
                FilledSize = ParseDecimalFlexible(t.GetProperty("executedSz")),
                Status = t.GetProperty("state").GetString()!,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(t.GetProperty("createdTime").GetInt64())
            });
        }

        yield return new TwapHistoryEvent
        {
            Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.TwapHistory,
            Symbol = "*", ReceivedAt = receivedAt, History = entries.ToArray(), Raw = raw
        };
    }

    private IEnumerable<ActiveAssetDataEvent> ParseActiveAssetData(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        var coin = data.TryGetProperty("coin", out var c) ? c.GetString()! : "*";

        yield return new ActiveAssetDataEvent
        {
            Exchange = UnifiedExchange.Hyperliquid, Channel = UnifiedWsChannel.ActiveAssetData,
            Symbol = coin, ReceivedAt = receivedAt,
            Leverage = data.TryGetProperty("leverage", out var lev) ? ParseDecimalFlexible(lev) : 0m,
            MarkPrice = data.TryGetProperty("markPx", out var mp) ? decimal.Parse(mp.GetString()!, CultureInfo.InvariantCulture) : 0m,
            MaxTradeSizeLong = data.TryGetProperty("maxTradeSzs", out var mts) && mts.GetArrayLength() > 0
                ? decimal.Parse(mts[0].GetString()!, CultureInfo.InvariantCulture) : null,
            MaxTradeSizeShort = data.TryGetProperty("maxTradeSzs", out _) && mts.GetArrayLength() > 1
                ? decimal.Parse(mts[1].GetString()!, CultureInfo.InvariantCulture) : null,
            AvailableToTradeLong = data.TryGetProperty("availableToTrade", out var att) && att.GetArrayLength() > 0
                ? decimal.Parse(att[0].GetString()!, CultureInfo.InvariantCulture) : null,
            AvailableToTradeShort = data.TryGetProperty("availableToTrade", out _) && att.GetArrayLength() > 1
                ? decimal.Parse(att[1].GetString()!, CultureInfo.InvariantCulture) : null,
            Raw = raw
        };
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static PriceLevel[] ParseLevels(JsonElement arr)
    {
        var result = new List<PriceLevel>();
        foreach (var level in arr.EnumerateArray())
        {
            result.Add(new PriceLevel
            {
                Price = decimal.Parse(level.GetProperty("px").GetString()!, CultureInfo.InvariantCulture),
                Size = decimal.Parse(level.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture)
            });
        }
        return result.ToArray();
    }

    private static decimal ParseDecimalFlexible(JsonElement el) =>
        el.ValueKind == JsonValueKind.String
            ? decimal.Parse(el.GetString()!, CultureInfo.InvariantCulture)
            : el.GetDecimal();
}
