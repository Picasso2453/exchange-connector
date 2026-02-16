using System.CommandLine;

using Xws.Exec;

namespace Xws.Exec.Cli;

public static class ExecutionCommands
{
    public static void Configure(RootCommand root)
    {
        var modeOption = new Option<string>("--mode", () => "paper", "Execution mode: paper|testnet|mainnet");
        var armLiveOption = new Option<bool>("--arm-live", "Arm live trading (mainnet only)");
        var exchangeOption = new Option<string>("--exchange", () => "hl", "Execution exchange: hl|okx|bybit|mexc");

        var placeCommand = BuildPlaceCommand(modeOption, armLiveOption, exchangeOption);
        var cancelCommand = BuildCancelCommand(modeOption, armLiveOption, exchangeOption);
        var cancelAllCommand = BuildCancelAllCommand(modeOption, armLiveOption, exchangeOption);
        var amendCommand = BuildAmendCommand(modeOption, armLiveOption, exchangeOption);
        var queryCommand = BuildQueryCommand(modeOption, armLiveOption, exchangeOption);

        root.AddCommand(placeCommand);
        root.AddCommand(cancelCommand);
        root.AddCommand(cancelAllCommand);
        root.AddCommand(amendCommand);
        root.AddCommand(queryCommand);
    }

    private static Command BuildPlaceCommand(
        Option<string> modeOption,
        Option<bool> armLiveOption,
        Option<string> exchangeOption)
    {
        var placeCommand = new Command("place", "Place an order");
        var symbolOption = new Option<string>("--symbol") { IsRequired = true };
        var sideOption = new Option<string>("--side") { IsRequired = true };
        var typeOption = new Option<string>("--type") { IsRequired = true };
        var sizeOption = new Option<decimal>("--size") { IsRequired = true };
        var priceOption = new Option<decimal?>("--price");
        var clientOrderIdOption = new Option<string?>("--client-order-id");
        var reduceOnlyOption = new Option<bool>("--reduce-only", "Reduce only (close or reduce existing position)");

        placeCommand.AddOption(modeOption);
        placeCommand.AddOption(armLiveOption);
        placeCommand.AddOption(exchangeOption);
        placeCommand.AddOption(symbolOption);
        placeCommand.AddOption(sideOption);
        placeCommand.AddOption(typeOption);
        placeCommand.AddOption(sizeOption);
        placeCommand.AddOption(priceOption);
        placeCommand.AddOption(clientOrderIdOption);
        placeCommand.AddOption(reduceOnlyOption);

        placeCommand.SetHandler(async context =>
        {
            try
            {
                var mode = context.ParseResult.GetValueForOption(modeOption) ?? string.Empty;
                var armLive = context.ParseResult.GetValueForOption(armLiveOption);
                var exchange = context.ParseResult.GetValueForOption(exchangeOption) ?? string.Empty;
                var symbol = context.ParseResult.GetValueForOption(symbolOption) ?? string.Empty;
                var side = context.ParseResult.GetValueForOption(sideOption) ?? string.Empty;
                var type = context.ParseResult.GetValueForOption(typeOption) ?? string.Empty;
                var size = context.ParseResult.GetValueForOption(sizeOption);
                var price = context.ParseResult.GetValueForOption(priceOption);
                var clientOrderId = context.ParseResult.GetValueForOption(clientOrderIdOption);
                var reduceOnly = context.ParseResult.GetValueForOption(reduceOnlyOption);

                if (!ExecutionCommandHelpers.TryParseMode(mode, out var execMode))
                {
                    ExecutionCommandHelpers.Fail("Invalid --mode. Supported values are paper, testnet, mainnet. Use --mode paper|testnet|mainnet.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.TryParseExchange(exchange, out var execExchange))
                {
                    ExecutionCommandHelpers.Fail("Invalid --exchange. Supported values are hl, okx, bybit, mexc. Use --exchange hl|okx|bybit|mexc.", 1);
                    return;
                }

                var config = ExecutionCommandHelpers.BuildConfig(execMode, armLive);
                var arming = ExecutionSafety.ValidateArming(config);
                if (!arming.Ok)
                {
                    ExecutionCommandHelpers.Fail(arming.Error, 1);
                    return;
                }

                if (!ExecutionCommandHelpers.TryParseSide(side, out var orderSide))
                {
                    ExecutionCommandHelpers.Fail("Invalid --side. Supported values are buy or sell. Use --side buy|sell.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.TryParseType(type, out var orderType))
                {
                    ExecutionCommandHelpers.Fail("Invalid --type. Supported values are market or limit. Use --type market|limit.", 1);
                    return;
                }

                if (execMode != ExecutionMode.Paper)
                {
                    ExecutionCommandHelpers.Fail("Unsupported --mode in CLI. xws.exec.cli only supports paper mode. Use --mode paper or use Xws.Exec for testnet/mainnet.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.IsValidSymbol(execExchange, symbol, out var symbolError))
                {
                    ExecutionCommandHelpers.Fail($"Invalid --symbol. {symbolError}. Provide exchange-native symbols.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.ValidatePlaceInputs(orderType, size, price, out var inputError))
                {
                    ExecutionCommandHelpers.Fail(inputError, 1);
                    return;
                }

                var request = new PlaceOrderRequest(
                    symbol,
                    orderSide,
                    orderType,
                    size,
                    price,
                    clientOrderId,
                    reduceOnly);

                var idempotency = ExecutionSafety.ValidateIdempotency(config, request);
                if (!idempotency.Ok)
                {
                    ExecutionCommandHelpers.Fail(idempotency.Error, 1);
                    return;
                }

                var client = ExecutionClientFactory.Create(config, execExchange);
                var result = await client.PlaceAsync(request, CancellationToken.None);

                ExecutionCommandHelpers.WriteJson(result);
            }
            catch (Exception ex)
            {
                ExecutionCommandHelpers.Fail($"Place failed. {ex.Message}. Check input values and retry.", 2);
            }
        });

        return placeCommand;
    }

    private static Command BuildCancelCommand(
        Option<string> modeOption,
        Option<bool> armLiveOption,
        Option<string> exchangeOption)
    {
        var cancelCommand = new Command("cancel", "Cancel an order by orderId");
        var orderIdOption = new Option<string>("--order-id") { IsRequired = true };

        cancelCommand.AddOption(modeOption);
        cancelCommand.AddOption(armLiveOption);
        cancelCommand.AddOption(exchangeOption);
        cancelCommand.AddOption(orderIdOption);

        cancelCommand.SetHandler(async context =>
        {
            try
            {
                var mode = context.ParseResult.GetValueForOption(modeOption) ?? string.Empty;
                var armLive = context.ParseResult.GetValueForOption(armLiveOption);
                var exchange = context.ParseResult.GetValueForOption(exchangeOption) ?? string.Empty;
                var orderId = context.ParseResult.GetValueForOption(orderIdOption) ?? string.Empty;

                if (!ExecutionCommandHelpers.TryParseMode(mode, out var execMode))
                {
                    ExecutionCommandHelpers.Fail("Invalid --mode. Supported values are paper, testnet, mainnet. Use --mode paper|testnet|mainnet.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.TryParseExchange(exchange, out var execExchange))
                {
                    ExecutionCommandHelpers.Fail("Invalid --exchange. Supported values are hl, okx, bybit, mexc. Use --exchange hl|okx|bybit|mexc.", 1);
                    return;
                }

                if (string.IsNullOrWhiteSpace(orderId))
                {
                    ExecutionCommandHelpers.Fail("Invalid --order-id. Value is required. Provide a non-empty order id.", 1);
                    return;
                }

                var config = ExecutionCommandHelpers.BuildConfig(execMode, armLive);
                var arming = ExecutionSafety.ValidateArming(config);
                if (!arming.Ok)
                {
                    ExecutionCommandHelpers.Fail(arming.Error, 1);
                    return;
                }

                if (execMode != ExecutionMode.Paper)
                {
                    ExecutionCommandHelpers.Fail("Unsupported --mode in CLI. xws.exec.cli only supports paper mode. Use --mode paper or use Xws.Exec for testnet/mainnet.", 1);
                    return;
                }

                var client = ExecutionClientFactory.Create(config, execExchange);
                var result = await client.CancelAsync(new CancelOrderRequest(OrderId: orderId), CancellationToken.None);

                ExecutionCommandHelpers.WriteJson(result);
            }
            catch (Exception ex)
            {
                ExecutionCommandHelpers.Fail($"Cancel failed. {ex.Message}. Check the order id and retry.", 2);
            }
        });

        return cancelCommand;
    }

    private static Command BuildCancelAllCommand(
        Option<string> modeOption,
        Option<bool> armLiveOption,
        Option<string> exchangeOption)
    {
        var cancelAllCommand = new Command("cancel-all", "Cancel all open orders");
        var symbolFilterOption = new Option<string?>("--symbol");

        cancelAllCommand.AddOption(modeOption);
        cancelAllCommand.AddOption(armLiveOption);
        cancelAllCommand.AddOption(exchangeOption);
        cancelAllCommand.AddOption(symbolFilterOption);

        cancelAllCommand.SetHandler(async context =>
        {
            try
            {
                var mode = context.ParseResult.GetValueForOption(modeOption) ?? string.Empty;
                var armLive = context.ParseResult.GetValueForOption(armLiveOption);
                var exchange = context.ParseResult.GetValueForOption(exchangeOption) ?? string.Empty;
                var symbol = context.ParseResult.GetValueForOption(symbolFilterOption);

                if (!ExecutionCommandHelpers.TryParseMode(mode, out var execMode))
                {
                    ExecutionCommandHelpers.Fail("Invalid --mode. Supported values are paper, testnet, mainnet. Use --mode paper|testnet|mainnet.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.TryParseExchange(exchange, out var execExchange))
                {
                    ExecutionCommandHelpers.Fail("Invalid --exchange. Supported values are hl, okx, bybit, mexc. Use --exchange hl|okx|bybit|mexc.", 1);
                    return;
                }

                var config = ExecutionCommandHelpers.BuildConfig(execMode, armLive);
                var arming = ExecutionSafety.ValidateArming(config);
                if (!arming.Ok)
                {
                    ExecutionCommandHelpers.Fail(arming.Error, 1);
                    return;
                }

                if (execMode != ExecutionMode.Paper)
                {
                    ExecutionCommandHelpers.Fail("Unsupported --mode in CLI. xws.exec.cli only supports paper mode. Use --mode paper or use Xws.Exec for testnet/mainnet.", 1);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(symbol) && !ExecutionCommandHelpers.IsValidSymbol(execExchange, symbol, out var symbolError))
                {
                    ExecutionCommandHelpers.Fail($"Invalid --symbol. {symbolError}. Provide exchange-native symbols.", 1);
                    return;
                }

                var client = ExecutionClientFactory.Create(config, execExchange);
                var result = await client.CancelAllAsync(new CancelAllRequest(symbol), CancellationToken.None);

                ExecutionCommandHelpers.WriteJson(result);
            }
            catch (Exception ex)
            {
                ExecutionCommandHelpers.Fail($"Cancel-all failed. {ex.Message}. Check inputs and retry.", 2);
            }
        });

        return cancelAllCommand;
    }

    private static Command BuildAmendCommand(
        Option<string> modeOption,
        Option<bool> armLiveOption,
        Option<string> exchangeOption)
    {
        var amendCommand = new Command("amend", "Amend an open order");
        var amendOrderIdOption = new Option<string>("--order-id") { IsRequired = true };
        var amendClientOrderIdOption = new Option<string?>("--client-order-id");
        var amendPriceOption = new Option<decimal?>("--price");
        var amendSizeOption = new Option<decimal?>("--size");

        amendCommand.AddOption(modeOption);
        amendCommand.AddOption(armLiveOption);
        amendCommand.AddOption(exchangeOption);
        amendCommand.AddOption(amendOrderIdOption);
        amendCommand.AddOption(amendClientOrderIdOption);
        amendCommand.AddOption(amendPriceOption);
        amendCommand.AddOption(amendSizeOption);

        amendCommand.SetHandler(async context =>
        {
            try
            {
                var mode = context.ParseResult.GetValueForOption(modeOption) ?? string.Empty;
                var armLive = context.ParseResult.GetValueForOption(armLiveOption);
                var exchange = context.ParseResult.GetValueForOption(exchangeOption) ?? string.Empty;
                var orderId = context.ParseResult.GetValueForOption(amendOrderIdOption) ?? string.Empty;
                var clientOrderId = context.ParseResult.GetValueForOption(amendClientOrderIdOption);
                var price = context.ParseResult.GetValueForOption(amendPriceOption);
                var size = context.ParseResult.GetValueForOption(amendSizeOption);

                if (!ExecutionCommandHelpers.TryParseMode(mode, out var execMode))
                {
                    ExecutionCommandHelpers.Fail("Invalid --mode. Supported values are paper, testnet, mainnet. Use --mode paper|testnet|mainnet.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.TryParseExchange(exchange, out var execExchange))
                {
                    ExecutionCommandHelpers.Fail("Invalid --exchange. Supported values are hl, okx, bybit, mexc. Use --exchange hl|okx|bybit|mexc.", 1);
                    return;
                }

                if (string.IsNullOrWhiteSpace(orderId))
                {
                    ExecutionCommandHelpers.Fail("Invalid --order-id. Value is required. Provide a non-empty order id.", 1);
                    return;
                }

                var config = ExecutionCommandHelpers.BuildConfig(execMode, armLive);
                var arming = ExecutionSafety.ValidateArming(config);
                if (!arming.Ok)
                {
                    ExecutionCommandHelpers.Fail(arming.Error, 1);
                    return;
                }

                if (execMode != ExecutionMode.Paper)
                {
                    ExecutionCommandHelpers.Fail("Unsupported --mode in CLI. xws.exec.cli only supports paper mode. Use --mode paper or use Xws.Exec for testnet/mainnet.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.ValidateAmendInputs(size, price, out var amendError))
                {
                    ExecutionCommandHelpers.Fail(amendError, 1);
                    return;
                }

                var request = new AmendOrderRequest(
                    orderId,
                    clientOrderId,
                    price,
                    size);

                var client = ExecutionClientFactory.Create(config, execExchange);
                var result = await client.AmendAsync(request, CancellationToken.None);

                ExecutionCommandHelpers.WriteJson(result);
            }
            catch (Exception ex)
            {
                ExecutionCommandHelpers.Fail($"Amend failed. {ex.Message}. Check inputs and retry.", 2);
            }
        });

        return amendCommand;
    }

    private static Command BuildQueryCommand(
        Option<string> modeOption,
        Option<bool> armLiveOption,
        Option<string> exchangeOption)
    {
        var queryCommand = new Command("query", "Query orders and positions");
        var queryOrdersCommand = new Command("orders", "Query orders");
        var queryPositionsCommand = new Command("positions", "Query positions");
        var queryStatusOption = new Option<string>("--status", () => "open", "Order status: open|closed|all");
        var queryOrderIdOption = new Option<string?>("--order-id");
        var querySymbolOption = new Option<string?>("--symbol");

        queryOrdersCommand.AddOption(modeOption);
        queryOrdersCommand.AddOption(armLiveOption);
        queryOrdersCommand.AddOption(exchangeOption);
        queryOrdersCommand.AddOption(queryStatusOption);
        queryOrdersCommand.AddOption(queryOrderIdOption);
        queryOrdersCommand.AddOption(querySymbolOption);

        queryOrdersCommand.SetHandler(async context =>
        {
            try
            {
                var mode = context.ParseResult.GetValueForOption(modeOption) ?? string.Empty;
                var armLive = context.ParseResult.GetValueForOption(armLiveOption);
                var exchange = context.ParseResult.GetValueForOption(exchangeOption) ?? string.Empty;
                var status = context.ParseResult.GetValueForOption(queryStatusOption) ?? string.Empty;
                var orderId = context.ParseResult.GetValueForOption(queryOrderIdOption);
                var symbol = context.ParseResult.GetValueForOption(querySymbolOption);

                if (!ExecutionCommandHelpers.TryParseMode(mode, out var execMode))
                {
                    ExecutionCommandHelpers.Fail("Invalid --mode. Supported values are paper, testnet, mainnet. Use --mode paper|testnet|mainnet.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.TryParseExchange(exchange, out var execExchange))
                {
                    ExecutionCommandHelpers.Fail("Invalid --exchange. Supported values are hl, okx, bybit, mexc. Use --exchange hl|okx|bybit|mexc.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.TryParseOrderStatus(status, out var queryStatus))
                {
                    ExecutionCommandHelpers.Fail("Invalid --status. Supported values are open, closed, all. Use --status open|closed|all.", 1);
                    return;
                }

                if (orderId is not null && string.IsNullOrWhiteSpace(orderId))
                {
                    ExecutionCommandHelpers.Fail("Invalid --order-id. Value cannot be empty when provided. Remove --order-id or provide a value.", 1);
                    return;
                }

                var config = ExecutionCommandHelpers.BuildConfig(execMode, armLive);
                var arming = ExecutionSafety.ValidateArming(config);
                if (!arming.Ok)
                {
                    ExecutionCommandHelpers.Fail(arming.Error, 1);
                    return;
                }

                if (execMode != ExecutionMode.Paper)
                {
                    ExecutionCommandHelpers.Fail("Unsupported --mode in CLI. xws.exec.cli only supports paper mode. Use --mode paper or use Xws.Exec for testnet/mainnet.", 1);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(symbol) && !ExecutionCommandHelpers.IsValidSymbol(execExchange, symbol, out var querySymbolError))
                {
                    ExecutionCommandHelpers.Fail($"Invalid --symbol. {querySymbolError}. Provide exchange-native symbols.", 1);
                    return;
                }

                var request = new QueryOrdersRequest(queryStatus, orderId, symbol);
                var client = ExecutionClientFactory.Create(config, execExchange);
                var result = await client.QueryOrdersAsync(request, CancellationToken.None);

                ExecutionCommandHelpers.WriteJson(result);
            }
            catch (Exception ex)
            {
                ExecutionCommandHelpers.Fail($"Query orders failed. {ex.Message}. Check inputs and retry.", 2);
            }
        });

        queryPositionsCommand.AddOption(modeOption);
        queryPositionsCommand.AddOption(armLiveOption);
        queryPositionsCommand.AddOption(exchangeOption);

        queryPositionsCommand.SetHandler(async context =>
        {
            try
            {
                var mode = context.ParseResult.GetValueForOption(modeOption) ?? string.Empty;
                var armLive = context.ParseResult.GetValueForOption(armLiveOption);
                var exchange = context.ParseResult.GetValueForOption(exchangeOption) ?? string.Empty;

                if (!ExecutionCommandHelpers.TryParseMode(mode, out var execMode))
                {
                    ExecutionCommandHelpers.Fail("Invalid --mode. Supported values are paper, testnet, mainnet. Use --mode paper|testnet|mainnet.", 1);
                    return;
                }

                if (!ExecutionCommandHelpers.TryParseExchange(exchange, out var execExchange))
                {
                    ExecutionCommandHelpers.Fail("Invalid --exchange. Supported values are hl, okx, bybit, mexc. Use --exchange hl|okx|bybit|mexc.", 1);
                    return;
                }

                var config = ExecutionCommandHelpers.BuildConfig(execMode, armLive);
                var arming = ExecutionSafety.ValidateArming(config);
                if (!arming.Ok)
                {
                    ExecutionCommandHelpers.Fail(arming.Error, 1);
                    return;
                }

                if (execMode != ExecutionMode.Paper)
                {
                    ExecutionCommandHelpers.Fail("Unsupported --mode in CLI. xws.exec.cli only supports paper mode. Use --mode paper or use Xws.Exec for testnet/mainnet.", 1);
                    return;
                }

                var client = ExecutionClientFactory.Create(config, execExchange);
                var result = await client.QueryPositionsAsync(CancellationToken.None);

                ExecutionCommandHelpers.WriteJson(result);
            }
            catch (Exception ex)
            {
                ExecutionCommandHelpers.Fail($"Query positions failed. {ex.Message}. Check inputs and retry.", 2);
            }
        });

        queryCommand.AddCommand(queryOrdersCommand);
        queryCommand.AddCommand(queryPositionsCommand);

        return queryCommand;
    }
}
