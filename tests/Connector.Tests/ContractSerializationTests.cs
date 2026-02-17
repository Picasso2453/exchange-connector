using System.Text.Json;
using Connector.Core.Contracts;

namespace Connector.Tests;

public class ContractSerializationTests
{
    private static T Roundtrip<T>(T obj) where T : class
    {
        var json = JsonSerializer.Serialize(obj, JsonOptions.Default);
        var result = JsonSerializer.Deserialize<T>(json, JsonOptions.Default);
        Assert.NotNull(result);
        return result;
    }

    [Fact]
    public void TradesEvent_Roundtrip()
    {
        var evt = new TradesEvent
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Trades,
            Symbol = "BTC",
            ReceivedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Sequence = 42,
            Trades = [
                new TradeEntry
                {
                    TradeId = "t1",
                    Price = 50000.5m,
                    Size = 1.25m,
                    Side = "buy",
                    Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z")
                }
            ]
        };

        var result = Roundtrip(evt);
        Assert.Equal(UnifiedExchange.Hyperliquid, result.Exchange);
        Assert.Equal(UnifiedWsChannel.Trades, result.Channel);
        Assert.Equal("BTC", result.Symbol);
        Assert.Equal(42L, result.Sequence);
        Assert.Single(result.Trades);
        Assert.Equal(50000.5m, result.Trades[0].Price);
        Assert.Equal("buy", result.Trades[0].Side);
    }

    [Fact]
    public void OrderBookL1Event_Roundtrip()
    {
        var evt = new OrderBookL1Event
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.OrderBookL1,
            Symbol = "ETH",
            ReceivedAt = DateTimeOffset.UtcNow,
            BestBid = new PriceLevel { Price = 3000m, Size = 10m },
            BestAsk = new PriceLevel { Price = 3001m, Size = 5m }
        };

        var result = Roundtrip(evt);
        Assert.Equal(3000m, result.BestBid.Price);
        Assert.Equal(3001m, result.BestAsk.Price);
    }

    [Fact]
    public void OrderBookL2Event_Roundtrip()
    {
        var evt = new OrderBookL2Event
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.OrderBookL2,
            Symbol = "SOL",
            ReceivedAt = DateTimeOffset.UtcNow,
            IsSnapshot = true,
            Bids = [new PriceLevel { Price = 100m, Size = 50m }],
            Asks = [new PriceLevel { Price = 101m, Size = 30m }]
        };

        var result = Roundtrip(evt);
        Assert.True(result.IsSnapshot);
        Assert.Single(result.Bids);
        Assert.Single(result.Asks);
    }

    [Fact]
    public void CandleEvent_Roundtrip()
    {
        var evt = new CandleEvent
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Candles,
            Symbol = "BTC",
            ReceivedAt = DateTimeOffset.UtcNow,
            Candle = new CandleEntry
            {
                OpenTime = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                Open = 50000m, High = 51000m, Low = 49000m, Close = 50500m,
                Volume = 1000m
            }
        };

        var result = Roundtrip(evt);
        Assert.Equal(50000m, result.Candle.Open);
        Assert.Equal(51000m, result.Candle.High);
    }

    [Fact]
    public void UserOrderEvent_Roundtrip()
    {
        var evt = new UserOrderEvent
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.UserOrders,
            Symbol = "BTC",
            ReceivedAt = DateTimeOffset.UtcNow,
            Orders = [
                new UserOrderEntry
                {
                    OrderId = "o1", Symbol = "BTC", Side = "buy", OrderType = "limit",
                    Price = 50000m, Size = 1m, FilledSize = 0m, Status = "open",
                    Timestamp = DateTimeOffset.UtcNow
                }
            ]
        };

        var result = Roundtrip(evt);
        Assert.Single(result.Orders);
        Assert.Equal("o1", result.Orders[0].OrderId);
    }

    [Fact]
    public void FillEvent_Roundtrip()
    {
        var evt = new FillEvent
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Fills,
            Symbol = "BTC",
            ReceivedAt = DateTimeOffset.UtcNow,
            Fills = [
                new FillEntry
                {
                    TradeId = "t1", OrderId = "o1", Symbol = "BTC", Side = "buy",
                    Price = 50000m, Size = 1m, Fee = 0.5m,
                    Timestamp = DateTimeOffset.UtcNow
                }
            ]
        };

        var result = Roundtrip(evt);
        Assert.Single(result.Fills);
        Assert.Equal(0.5m, result.Fills[0].Fee);
    }

    [Fact]
    public void PositionEvent_Roundtrip()
    {
        var evt = new PositionEvent
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Positions,
            Symbol = "BTC",
            ReceivedAt = DateTimeOffset.UtcNow,
            Positions = [
                new PositionEntry
                {
                    Symbol = "BTC", Side = "long", Size = 1m,
                    EntryPrice = 50000m, UnrealizedPnl = 500m,
                    LiquidationPrice = 45000m, Leverage = 10m
                }
            ]
        };

        var result = Roundtrip(evt);
        Assert.Equal(50000m, result.Positions[0].EntryPrice);
        Assert.Equal(45000m, result.Positions[0].LiquidationPrice);
    }

    [Fact]
    public void BalanceEvent_Roundtrip()
    {
        var evt = new BalanceEvent
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Balances,
            Symbol = "USDC",
            ReceivedAt = DateTimeOffset.UtcNow,
            Balances = [
                new BalanceEntry { Asset = "USDC", Total = 10000m, Available = 8000m }
            ]
        };

        var result = Roundtrip(evt);
        Assert.Equal(10000m, result.Balances[0].Total);
    }

    [Fact]
    public void WsSubscribeRequest_Roundtrip()
    {
        var req = new UnifiedWsSubscribeRequest
        {
            CorrelationId = "c1",
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Trades,
            Symbols = ["BTC", "ETH"],
            Interval = "1m"
        };

        var result = Roundtrip(req);
        Assert.Equal("c1", result.CorrelationId);
        Assert.Equal(2, result.Symbols.Length);
        Assert.Equal("1m", result.Interval);
    }

    [Fact]
    public void PlaceOrderRequest_Roundtrip()
    {
        var req = new PlaceOrderRequest
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Symbol = "BTC",
            Side = "buy",
            OrderType = "limit",
            Size = 1m,
            Price = 50000m,
            ClientOrderId = "co1"
        };

        var result = Roundtrip(req);
        Assert.Equal("buy", result.Side);
        Assert.Equal(50000m, result.Price);
        Assert.True(result.AuthRequired);
    }

    [Fact]
    public void PlaceOrderResponse_Roundtrip()
    {
        var resp = new PlaceOrderResponse
        {
            OrderId = "o1",
            ClientOrderId = "co1",
            Status = "accepted"
        };

        var result = Roundtrip(resp);
        Assert.Equal("o1", result.OrderId);
    }

    [Fact]
    public void RawPayload_PopulatedWhenSet()
    {
        var evt = new TradesEvent
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Trades,
            Symbol = "BTC",
            ReceivedAt = DateTimeOffset.UtcNow,
            Trades = [],
            Raw = new RawPayload
            {
                RawJson = "{\"test\":true}",
                ExchangeMessageType = "trades"
            }
        };

        var json = JsonSerializer.Serialize(evt, JsonOptions.Default);
        Assert.Contains("rawJson", json);
        Assert.Contains("exchangeMessageType", json);

        var result = Roundtrip(evt);
        Assert.NotNull(result.Raw);
        Assert.Equal("{\"test\":true}", result.Raw.RawJson);
    }

    [Fact]
    public void RawPayload_OmittedWhenNull()
    {
        var evt = new TradesEvent
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Trades,
            Symbol = "BTC",
            ReceivedAt = DateTimeOffset.UtcNow,
            Trades = []
        };

        var json = JsonSerializer.Serialize(evt, JsonOptions.Default);
        Assert.DoesNotContain("raw", json);
    }

    [Fact]
    public void Enums_SerializeAsStrings()
    {
        var evt = new TradesEvent
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Trades,
            Symbol = "BTC",
            ReceivedAt = DateTimeOffset.UtcNow,
            Trades = []
        };

        var json = JsonSerializer.Serialize(evt, JsonOptions.Default);
        Assert.Contains("\"hyperliquid\"", json);
        Assert.Contains("\"trades\"", json);
        Assert.DoesNotContain("\"0\"", json);
    }

    [Fact]
    public void GetCandlesRequest_PublicByDefault()
    {
        var req = new GetCandlesRequest
        {
            Exchange = UnifiedExchange.Hyperliquid,
            Symbol = "BTC",
            Interval = "1h",
            Limit = 100
        };

        Assert.False(req.AuthRequired);
    }

    [Fact]
    public void GetBalancesRequest_RequiresAuth()
    {
        var req = new GetBalancesRequest
        {
            Exchange = UnifiedExchange.Hyperliquid
        };

        Assert.True(req.AuthRequired);
    }
}
