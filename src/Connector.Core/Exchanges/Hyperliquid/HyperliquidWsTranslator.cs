using System.Globalization;
using System.Text.Json;
using Connector.Core.Abstractions;
using Connector.Core.Contracts;
using Connector.Core.Transport;
using Microsoft.Extensions.Logging;

namespace Connector.Core.Exchanges.Hyperliquid;

/// <summary>
/// Translates unified WS requests to Hyperliquid protocol and HL messages to unified events.
/// Supports: Trades, L2Book, Candles (public), OrderUpdates, UserFills (private).
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

    public IEnumerable<TransportWsMessage> ToExchangeSubscribe(UnifiedWsSubscribeRequest request)
    {
        foreach (var symbol in request.Symbols)
        {
            var sub = request.Channel switch
            {
                UnifiedWsChannel.Trades => new { method = "subscribe", subscription = (object)new { type = "trades", coin = symbol } },
                UnifiedWsChannel.OrderBookL2 => new { method = "subscribe", subscription = (object)new { type = "l2Book", coin = symbol } },
                UnifiedWsChannel.Candles => new { method = "subscribe", subscription = (object)new { type = "candle", coin = symbol, interval = request.Interval ?? "1m" } },
                UnifiedWsChannel.UserOrders => new { method = "subscribe", subscription = (object)new { type = "orderUpdates", user = _userAddress ?? throw new InvalidOperationException("userAddress required for orderUpdates") } },
                UnifiedWsChannel.Fills => new { method = "subscribe", subscription = (object)new { type = "userFills", user = _userAddress ?? throw new InvalidOperationException("userAddress required for userFills") } },
                _ => throw new NotSupportedException($"Channel {request.Channel} not supported for Hyperliquid")
            };

            yield return new TransportWsMessage { Payload = JsonSerializer.Serialize(sub) };

            // For user-scoped channels, only one subscription needed regardless of symbols
            if (request.Channel is UnifiedWsChannel.UserOrders or UnifiedWsChannel.Fills)
                yield break;
        }
    }

    public IEnumerable<TransportWsMessage> ToExchangeUnsubscribe(UnifiedWsUnsubscribeRequest request)
    {
        foreach (var symbol in request.Symbols)
        {
            var sub = request.Channel switch
            {
                UnifiedWsChannel.Trades => new { method = "unsubscribe", subscription = (object)new { type = "trades", coin = symbol } },
                UnifiedWsChannel.OrderBookL2 => new { method = "unsubscribe", subscription = (object)new { type = "l2Book", coin = symbol } },
                UnifiedWsChannel.Candles => new { method = "unsubscribe", subscription = (object)new { type = "candle", coin = symbol, interval = "1m" } },
                UnifiedWsChannel.UserOrders => new { method = "unsubscribe", subscription = (object)new { type = "orderUpdates", user = _userAddress } },
                UnifiedWsChannel.Fills => new { method = "unsubscribe", subscription = (object)new { type = "userFills", user = _userAddress } },
                _ => throw new NotSupportedException($"Channel {request.Channel} not supported for Hyperliquid")
            };

            yield return new TransportWsMessage { Payload = JsonSerializer.Serialize(sub) };

            if (request.Channel is UnifiedWsChannel.UserOrders or UnifiedWsChannel.Fills)
                yield break;
        }
    }

    public IEnumerable<UnifiedWsEvent> FromExchangeMessage(TransportWsInbound inbound)
    {
        using var doc = JsonDocument.Parse(inbound.Payload);
        var root = doc.RootElement;

        if (!root.TryGetProperty("channel", out var channelProp))
        {
            // Subscription ack or pong - skip
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
                foreach (var evt in ParseTrades(data, inbound.ReceivedAt, raw))
                    yield return evt;
                break;

            case "l2Book":
                foreach (var evt in ParseL2Book(data, inbound.ReceivedAt, raw))
                    yield return evt;
                break;

            case "candle":
                foreach (var evt in ParseCandle(data, inbound.ReceivedAt, raw))
                    yield return evt;
                break;

            case "orderUpdates":
                foreach (var evt in ParseOrderUpdates(data, inbound.ReceivedAt, raw))
                    yield return evt;
                break;

            case "userFills":
                foreach (var evt in ParseUserFills(data, inbound.ReceivedAt, raw))
                    yield return evt;
                break;

            case "subscriptionResponse":
                _logger.LogDebug("Subscription confirmed");
                break;

            default:
                _logger.LogDebug("Unknown channel: {Channel}", channel);
                break;
        }
    }

    private IEnumerable<TradesEvent> ParseTrades(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        if (data.ValueKind != JsonValueKind.Array) yield break;

        // Group by coin for the event
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
                Exchange = UnifiedExchange.Hyperliquid,
                Channel = UnifiedWsChannel.Trades,
                Symbol = coin,
                ReceivedAt = receivedAt,
                Trades = trades.ToArray(),
                Raw = raw
            };
        }
    }

    private IEnumerable<OrderBookL2Event> ParseL2Book(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        var coin = data.GetProperty("coin").GetString()!;
        var levels = data.GetProperty("levels");

        var bids = ParseLevels(levels[0]);
        var asks = ParseLevels(levels[1]);

        yield return new OrderBookL2Event
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.OrderBookL2,
            Symbol = coin,
            ReceivedAt = receivedAt,
            IsSnapshot = true,
            Bids = bids,
            Asks = asks,
            Raw = raw
        };
    }

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

    private IEnumerable<CandleEvent> ParseCandle(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        // HL sends candle data as a single object or array
        var items = data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray().ToList()
            : [data];

        foreach (var c in items)
        {
            var coin = c.GetProperty("s").GetString()!;
            yield return new CandleEvent
            {
                Exchange = UnifiedExchange.Hyperliquid,
                Channel = UnifiedWsChannel.Candles,
                Symbol = coin,
                ReceivedAt = receivedAt,
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
                FilledSize = decimal.Parse(order.GetProperty("origSz").GetString()!, CultureInfo.InvariantCulture) - decimal.Parse(order.GetProperty("sz").GetString()!, CultureInfo.InvariantCulture),
                Status = item.GetProperty("status").GetString()!,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(item.GetProperty("statusTimestamp").GetInt64())
            });
        }

        if (orders.Count > 0)
        {
            yield return new UserOrderEvent
            {
                Exchange = UnifiedExchange.Hyperliquid,
                Channel = UnifiedWsChannel.UserOrders,
                Symbol = firstCoin!,
                ReceivedAt = receivedAt,
                Orders = orders.ToArray(),
                Raw = raw
            };
        }
    }

    private IEnumerable<FillEvent> ParseUserFills(JsonElement data, DateTimeOffset receivedAt, RawPayload? raw)
    {
        // data has { user, fills, isSnapshot? }
        if (!data.TryGetProperty("fills", out var fillsArr)) yield break;

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
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(f.GetProperty("time").GetInt64())
            });
        }

        if (fills.Count > 0)
        {
            yield return new FillEvent
            {
                Exchange = UnifiedExchange.Hyperliquid,
                Channel = UnifiedWsChannel.Fills,
                Symbol = firstCoin!,
                ReceivedAt = receivedAt,
                Fills = fills.ToArray(),
                Raw = raw
            };
        }
    }

    /// <summary>
    /// Parses a decimal from a JSON element that may be a number or a string.
    /// HL API sends some fields (e.g. candle OHLCV) as strings, others as numbers.
    /// </summary>
    private static decimal ParseDecimalFlexible(JsonElement el) =>
        el.ValueKind == JsonValueKind.String
            ? decimal.Parse(el.GetString()!, CultureInfo.InvariantCulture)
            : el.GetDecimal();
}
