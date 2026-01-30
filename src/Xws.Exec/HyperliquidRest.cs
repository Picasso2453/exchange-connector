using System.Collections;
using CryptoExchange.Net.Authentication;
using HyperLiquid.Net;
using HyperLiquid.Net.Clients;
using HyperLiquid.Net.Interfaces.Clients;
using HyperLiquid.Net.Enums;
using HyperLiquid.Net.Objects.Models;

namespace Xws.Exec;

public sealed class HyperliquidRest : IHyperliquidRest
{
    public async Task<HyperliquidPlaceResult> PlaceOrderAsync(PlaceOrderRequest request, ExecutionConfig config, CancellationToken cancellationToken)
    {
        var client = BuildClient(config);
        var order = BuildOrderRequest(request);
        var result = await client.FuturesApi.Trading.PlaceMultipleOrdersAsync(
            new[] { order },
            null,
            null,
            null,
            cancellationToken);

        if (!result.Success)
        {
            return new HyperliquidPlaceResult(null, "error", result.Error);
        }

        var first = GetFirstItem(result.Data);
        var orderId = GetStringProperty(first, "OrderId");
        var status = GetStringProperty(first, "Status");

        return new HyperliquidPlaceResult(orderId, status, result.Data);
    }

    public async Task<HyperliquidCancelResult> CancelOrderAsync(string orderId, string symbol, ExecutionConfig config, CancellationToken cancellationToken)
    {
        var client = BuildClient(config);
        var parsed = long.Parse(orderId);
        var request = new HyperLiquidCancelRequest(symbol, parsed);
        var result = await client.FuturesApi.Trading.CancelOrdersAsync(
            new[] { request },
            null,
            null,
            cancellationToken);

        if (!result.Success)
        {
            return new HyperliquidCancelResult("error", result.Error);
        }

        var first = GetFirstItem(result.Data);
        var status = GetStringProperty(first, "Status") ?? "success";

        return new HyperliquidCancelResult(status, result.Data);
    }

    public async Task<IReadOnlyList<HyperliquidOpenOrder>> GetOpenOrdersAsync(string address, ExecutionConfig config, CancellationToken cancellationToken)
    {
        var client = BuildClient(config);
        var result = await client.FuturesApi.Trading.GetOpenOrdersAsync(address, string.Empty, cancellationToken);

        if (!result.Success || result.Data is null)
        {
            return Array.Empty<HyperliquidOpenOrder>();
        }

        var list = new List<HyperliquidOpenOrder>();
        foreach (var item in (IEnumerable)result.Data)
        {
            var orderId = GetStringProperty(item, "OrderId") ?? string.Empty;
            var clientOrderId = GetStringProperty(item, "ClientOrderId");
            var symbol = GetStringProperty(item, "Symbol");
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                list.Add(new HyperliquidOpenOrder(orderId, clientOrderId, symbol));
            }
        }

        return list;
    }

    public async Task<HyperliquidCancelResult> CancelManyAsync(IReadOnlyList<string> orderIds, ExecutionConfig config, CancellationToken cancellationToken)
    {
        var client = BuildClient(config);
        var requests = orderIds.Select(id => new HyperLiquidCancelRequest(string.Empty, long.Parse(id))).ToArray();
        var result = await client.FuturesApi.Trading.CancelOrdersAsync(requests, null, null, cancellationToken);

        return result.Success
            ? new HyperliquidCancelResult("success", result.Data)
            : new HyperliquidCancelResult("error", result.Error);
    }

    private static IHyperLiquidRestClient BuildClient(ExecutionConfig config)
    {
        if (config.HyperliquidCredentials is null)
        {
            throw new InvalidOperationException("hyperliquid credentials are required");
        }

        var env = config.Mode == ExecutionMode.Testnet
            ? HyperLiquidEnvironment.Testnet
            : HyperLiquidEnvironment.Live;

        var credentials = new ApiCredentials(
            config.HyperliquidCredentials.AccountAddress,
            config.HyperliquidCredentials.PrivateKey);

        var provider = new HyperLiquidUserClientProvider();
        return provider.GetRestClient(
            config.HyperliquidCredentials.AccountAddress,
            credentials,
            env);
    }

    private static HyperLiquidOrderRequest BuildOrderRequest(PlaceOrderRequest request)
    {
        var side = request.Side == OrderSide.Buy ? HyperLiquid.Net.Enums.OrderSide.Buy : HyperLiquid.Net.Enums.OrderSide.Sell;
        var type = request.Type == OrderType.Market ? HyperLiquid.Net.Enums.OrderType.Market : HyperLiquid.Net.Enums.OrderType.Limit;
        var price = request.Price ?? 0m;

        return new HyperLiquidOrderRequest(
            request.Symbol,
            side,
            type,
            request.Size,
            price,
            null,
            request.ReduceOnly,
            null,
            null,
            request.ClientOrderId ?? string.Empty,
            null);
    }

    private static object? GetFirstItem(object? data)
    {
        if (data is null)
        {
            return null;
        }

        if (data is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                return item;
            }
        }

        return data;
    }

    private static string? GetStringProperty(object? obj, string name)
    {
        if (obj is null)
        {
            return null;
        }

        var prop = obj.GetType().GetProperty(name);
        return prop?.GetValue(obj)?.ToString();
    }
}
