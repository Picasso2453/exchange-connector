using Connector.Core.Contracts;
using Connector.Core.Exchanges.Hyperliquid;
using Connector.Core.Transport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Connector.Tests;

public class HyperliquidWsTranslatorTests
{
    private readonly HyperliquidWsTranslator _translator = new(
        NullLogger.Instance, includeRaw: false, userAddress: "0xTestUser");

    private readonly HyperliquidWsTranslator _translatorWithRaw = new(
        NullLogger.Instance, includeRaw: true, userAddress: "0xTestUser");

    // --- Subscribe tests ---

    [Fact]
    public void Subscribe_Trades_ProducesCorrectJson()
    {
        var req = new UnifiedWsSubscribeRequest
        {
            CorrelationId = "c1",
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Trades,
            Symbols = ["BTC"]
        };

        var messages = _translator.ToExchangeSubscribe(req).ToList();
        Assert.Single(messages);
        Assert.Contains("\"method\":\"subscribe\"", messages[0].Payload);
        Assert.Contains("\"type\":\"trades\"", messages[0].Payload);
        Assert.Contains("\"coin\":\"BTC\"", messages[0].Payload);
    }

    [Fact]
    public void Subscribe_MultipleSymbols_ProducesMultipleMessages()
    {
        var req = new UnifiedWsSubscribeRequest
        {
            CorrelationId = "c1",
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Trades,
            Symbols = ["BTC", "ETH", "SOL"]
        };

        var messages = _translator.ToExchangeSubscribe(req).ToList();
        Assert.Equal(3, messages.Count);
    }

    [Fact]
    public void Subscribe_L2Book_ProducesCorrectJson()
    {
        var req = new UnifiedWsSubscribeRequest
        {
            CorrelationId = "c1",
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.OrderBookL2,
            Symbols = ["ETH"]
        };

        var messages = _translator.ToExchangeSubscribe(req).ToList();
        Assert.Single(messages);
        Assert.Contains("\"type\":\"l2Book\"", messages[0].Payload);
    }

    [Fact]
    public void Subscribe_Candles_IncludesInterval()
    {
        var req = new UnifiedWsSubscribeRequest
        {
            CorrelationId = "c1",
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.Candles,
            Symbols = ["SOL"],
            Interval = "5m"
        };

        var messages = _translator.ToExchangeSubscribe(req).ToList();
        Assert.Single(messages);
        Assert.Contains("\"interval\":\"5m\"", messages[0].Payload);
    }

    [Fact]
    public void Subscribe_UserOrders_OnlyOneMessage()
    {
        var req = new UnifiedWsSubscribeRequest
        {
            CorrelationId = "c1",
            Exchange = UnifiedExchange.Hyperliquid,
            Channel = UnifiedWsChannel.UserOrders,
            Symbols = ["BTC", "ETH"]
        };

        var messages = _translator.ToExchangeSubscribe(req).ToList();
        Assert.Single(messages);
        Assert.Contains("\"type\":\"orderUpdates\"", messages[0].Payload);
        Assert.Contains("\"user\":\"0xTestUser\"", messages[0].Payload);
    }

    // --- Parse tests (golden fixtures) ---

    [Fact]
    public void Parse_Trades_ReturnsTradesEvent()
    {
        var json = """
        {
          "channel": "trades",
          "data": [
            {"coin":"BTC","side":"B","px":"50123.45","sz":"1.5","hash":"0xabc","time":1704067200000,"tid":12345,"users":["0xa","0xb"]},
            {"coin":"BTC","side":"A","px":"50125.00","sz":"0.3","hash":"0xdef","time":1704067200100,"tid":12346,"users":["0xc","0xd"]}
          ]
        }
        """;

        var events = Parse(json);
        Assert.Single(events);
        var te = Assert.IsType<TradesEvent>(events[0]);
        Assert.Equal("BTC", te.Symbol);
        Assert.Equal(UnifiedExchange.Hyperliquid, te.Exchange);
        Assert.Equal(2, te.Trades.Length);
        Assert.Equal(50123.45m, te.Trades[0].Price);
        Assert.Equal(1.5m, te.Trades[0].Size);
        Assert.Equal("b", te.Trades[0].Side);
        Assert.Equal("12345", te.Trades[0].TradeId);
    }

    [Fact]
    public void Parse_L2Book_ReturnsOrderBookL2Event()
    {
        var json = """
        {
          "channel": "l2Book",
          "data": {
            "coin": "ETH",
            "levels": [
              [{"px":"3000.50","sz":"10.0","n":5},{"px":"2999.00","sz":"20.0","n":3}],
              [{"px":"3001.00","sz":"8.0","n":4},{"px":"3002.50","sz":"15.0","n":2}]
            ],
            "time": 1704067200000
          }
        }
        """;

        var events = Parse(json);
        Assert.Single(events);
        var ob = Assert.IsType<OrderBookL2Event>(events[0]);
        Assert.Equal("ETH", ob.Symbol);
        Assert.True(ob.IsSnapshot);
        Assert.Equal(2, ob.Bids.Length);
        Assert.Equal(2, ob.Asks.Length);
        Assert.Equal(3000.50m, ob.Bids[0].Price);
        Assert.Equal(3001.00m, ob.Asks[0].Price);
    }

    [Fact]
    public void Parse_Candle_StringValues_ReturnsCandleEvent()
    {
        // Real HL API sends OHLCV as strings
        var json = """
        {
          "channel": "candle",
          "data": {
            "t": 1704067200000,
            "T": 1704067260000,
            "s": "SOL",
            "i": "1m",
            "o": "100.5",
            "c": "101.0",
            "h": "102.0",
            "l": "99.5",
            "v": "5000.0",
            "n": 150
          }
        }
        """;

        var events = Parse(json);
        Assert.Single(events);
        var ce = Assert.IsType<CandleEvent>(events[0]);
        Assert.Equal("SOL", ce.Symbol);
        Assert.Equal(100.5m, ce.Candle.Open);
        Assert.Equal(101.0m, ce.Candle.Close);
        Assert.Equal(102.0m, ce.Candle.High);
        Assert.Equal(99.5m, ce.Candle.Low);
        Assert.Equal(5000.0m, ce.Candle.Volume);
    }

    [Fact]
    public void Parse_Candle_NumericValues_ReturnsCandleEvent()
    {
        // Also handle numeric values for robustness
        var json = """
        {
          "channel": "candle",
          "data": {
            "t": 1704067200000,
            "T": 1704067260000,
            "s": "BTC",
            "i": "1m",
            "o": 50000.0,
            "c": 50100.0,
            "h": 50200.0,
            "l": 49900.0,
            "v": 1234.5,
            "n": 80
          }
        }
        """;

        var events = Parse(json);
        Assert.Single(events);
        var ce = Assert.IsType<CandleEvent>(events[0]);
        Assert.Equal("BTC", ce.Symbol);
        Assert.Equal(50000.0m, ce.Candle.Open);
        Assert.Equal(50100.0m, ce.Candle.Close);
    }

    [Fact]
    public void Parse_OrderUpdates_ReturnsUserOrderEvent()
    {
        var json = """
        {
          "channel": "orderUpdates",
          "data": [
            {
              "order": {
                "coin": "BTC",
                "side": "B",
                "limitPx": "50000.00",
                "sz": "0.5",
                "oid": 999,
                "timestamp": 1704067200000,
                "origSz": "1.0",
                "cloid": "my-order-1"
              },
              "status": "open",
              "statusTimestamp": 1704067200100
            }
          ]
        }
        """;

        var events = Parse(json);
        Assert.Single(events);
        var oe = Assert.IsType<UserOrderEvent>(events[0]);
        Assert.Single(oe.Orders);
        Assert.Equal("999", oe.Orders[0].OrderId);
        Assert.Equal("my-order-1", oe.Orders[0].ClientOrderId);
        Assert.Equal(50000.00m, oe.Orders[0].Price);
        Assert.Equal(0.5m, oe.Orders[0].FilledSize); // origSz(1.0) - sz(0.5)
    }

    [Fact]
    public void Parse_UserFills_ReturnsFillEvent()
    {
        var json = """
        {
          "channel": "userFills",
          "data": {
            "isSnapshot": true,
            "user": "0xTestUser",
            "fills": [
              {
                "coin": "BTC",
                "px": "50100.00",
                "sz": "0.5",
                "side": "B",
                "time": 1704067200000,
                "startPosition": "0.0",
                "dir": "Open Long",
                "closedPnl": "0.0",
                "hash": "0xfill1",
                "oid": 999,
                "crossed": true,
                "fee": "12.525",
                "tid": 54321,
                "feeToken": "USDC"
              }
            ]
          }
        }
        """;

        var events = Parse(json);
        Assert.Single(events);
        var fe = Assert.IsType<FillEvent>(events[0]);
        Assert.Single(fe.Fills);
        Assert.Equal("BTC", fe.Fills[0].Symbol);
        Assert.Equal(50100.00m, fe.Fills[0].Price);
        Assert.Equal(12.525m, fe.Fills[0].Fee);
        Assert.Equal("54321", fe.Fills[0].TradeId);
    }

    [Fact]
    public void Parse_SubscriptionResponse_NoEvents()
    {
        var json = """{"channel":"subscriptionResponse","data":{"method":"subscribe","subscription":{"type":"trades","coin":"BTC"}}}""";
        var events = Parse(json);
        Assert.Empty(events);
    }

    [Fact]
    public void Parse_UnknownChannel_NoEvents()
    {
        var json = """{"channel":"unknown","data":{}}""";
        var events = Parse(json);
        Assert.Empty(events);
    }

    [Fact]
    public void Parse_NoChannelField_NoEvents()
    {
        var json = """{"method":"pong"}""";
        var events = Parse(json);
        Assert.Empty(events);
    }

    [Fact]
    public void Parse_WithRawPayload_IncludesRaw()
    {
        var json = """
        {
          "channel": "trades",
          "data": [
            {"coin":"BTC","side":"B","px":"50000","sz":"1","hash":"0x","time":1704067200000,"tid":1,"users":["0xa","0xb"]}
          ]
        }
        """;

        var inbound = new TransportWsInbound { Payload = json, ReceivedAt = DateTimeOffset.UtcNow };
        var events = _translatorWithRaw.FromExchangeMessage(inbound).ToList();
        Assert.Single(events);
        Assert.NotNull(events[0].Raw);
        Assert.Contains("trades", events[0].Raw!.RawJson!);
    }

    private List<UnifiedWsEvent> Parse(string json)
    {
        var inbound = new TransportWsInbound
        {
            Payload = json,
            ReceivedAt = DateTimeOffset.UtcNow
        };
        return _translator.FromExchangeMessage(inbound).ToList();
    }
}
