using System.CommandLine;
using System.Text.Json;
using Xws.Exec;

var root = new RootCommand("xws.exec CLI");

var modeOption = new Option<string>("--mode", () => "paper", "Execution mode: paper|testnet|mainnet");
var armLiveOption = new Option<bool>("--arm-live", "Arm live trading (mainnet only)");
var exchangeOption = new Option<string>("--exchange", () => "hl", "Execution exchange: hl|okx|bybit|mexc");

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

        if (!TryParseMode(mode, out var execMode))
        {
            Console.Error.WriteLine("invalid --mode (expected: paper|testnet|mainnet)");
            Environment.ExitCode = 1;
            return;
        }

        if (!TryParseExchange(exchange, out var execExchange))
        {
            Console.Error.WriteLine("invalid --exchange (expected: hl|okx|bybit|mexc)");
            Environment.ExitCode = 1;
            return;
        }

        var config = BuildConfig(execMode, armLive);
        var arming = ExecutionSafety.ValidateArming(config);
        if (!arming.Ok)
        {
            Console.Error.WriteLine(arming.Error);
            Environment.ExitCode = 1;
            return;
        }

        if (!TryParseSide(side, out var orderSide))
        {
            Console.Error.WriteLine("invalid --side (expected: buy|sell)");
            Environment.ExitCode = 1;
            return;
        }

        if (!TryParseType(type, out var orderType))
        {
            Console.Error.WriteLine("invalid --type (expected: market|limit)");
            Environment.ExitCode = 1;
            return;
        }

        if (orderType == OrderType.Limit && price is null)
        {
            Console.Error.WriteLine("--price is required for limit orders");
            Environment.ExitCode = 1;
            return;
        }

        if (execMode != ExecutionMode.Paper)
        {
            Console.Error.WriteLine("only paper mode is implemented in the CLI");
            Environment.ExitCode = 1;
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
            Console.Error.WriteLine(idempotency.Error);
            Environment.ExitCode = 1;
            return;
        }

        var client = ExecutionClientFactory.Create(config, execExchange);
        var result = await client.PlaceAsync(request, CancellationToken.None);

        WriteJson(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"place failed: {ex.Message}");
        Environment.ExitCode = 2;
    }
});

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

        if (!TryParseMode(mode, out var execMode))
        {
            Console.Error.WriteLine("invalid --mode (expected: paper|testnet|mainnet)");
            Environment.ExitCode = 1;
            return;
        }

        if (!TryParseExchange(exchange, out var execExchange))
        {
            Console.Error.WriteLine("invalid --exchange (expected: hl|okx|bybit|mexc)");
            Environment.ExitCode = 1;
            return;
        }

        var config = BuildConfig(execMode, armLive);
        var arming = ExecutionSafety.ValidateArming(config);
        if (!arming.Ok)
        {
            Console.Error.WriteLine(arming.Error);
            Environment.ExitCode = 1;
            return;
        }

        if (execMode != ExecutionMode.Paper)
        {
            Console.Error.WriteLine("only paper mode is implemented in the CLI");
            Environment.ExitCode = 1;
            return;
        }

        var client = ExecutionClientFactory.Create(config, execExchange);
        var result = await client.CancelAsync(new CancelOrderRequest(OrderId: orderId), CancellationToken.None);

        WriteJson(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"cancel failed: {ex.Message}");
        Environment.ExitCode = 2;
    }
});

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

        if (!TryParseMode(mode, out var execMode))
        {
            Console.Error.WriteLine("invalid --mode (expected: paper|testnet|mainnet)");
            Environment.ExitCode = 1;
            return;
        }

        if (!TryParseExchange(exchange, out var execExchange))
        {
            Console.Error.WriteLine("invalid --exchange (expected: hl|okx|bybit|mexc)");
            Environment.ExitCode = 1;
            return;
        }

        var config = BuildConfig(execMode, armLive);
        var arming = ExecutionSafety.ValidateArming(config);
        if (!arming.Ok)
        {
            Console.Error.WriteLine(arming.Error);
            Environment.ExitCode = 1;
            return;
        }

        if (execMode != ExecutionMode.Paper)
        {
            Console.Error.WriteLine("only paper mode is implemented in the CLI");
            Environment.ExitCode = 1;
            return;
        }

        var client = ExecutionClientFactory.Create(config, execExchange);
        var result = await client.CancelAllAsync(new CancelAllRequest(symbol), CancellationToken.None);

        WriteJson(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"cancel-all failed: {ex.Message}");
        Environment.ExitCode = 2;
    }
});

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

        if (!TryParseMode(mode, out var execMode))
        {
            Console.Error.WriteLine("invalid --mode (expected: paper|testnet|mainnet)");
            Environment.ExitCode = 1;
            return;
        }

        if (!TryParseExchange(exchange, out var execExchange))
        {
            Console.Error.WriteLine("invalid --exchange (expected: hl|okx|bybit|mexc)");
            Environment.ExitCode = 1;
            return;
        }

        var config = BuildConfig(execMode, armLive);
        var arming = ExecutionSafety.ValidateArming(config);
        if (!arming.Ok)
        {
            Console.Error.WriteLine(arming.Error);
            Environment.ExitCode = 1;
            return;
        }

        if (execMode != ExecutionMode.Paper)
        {
            Console.Error.WriteLine("only paper mode is implemented in the CLI");
            Environment.ExitCode = 1;
            return;
        }

        if (price is null && size is null)
        {
            Console.Error.WriteLine("amend requires --price, --size, or both");
            Environment.ExitCode = 1;
            return;
        }

        var request = new AmendOrderRequest(
            orderId,
            clientOrderId,
            price,
            size);

        var client = ExecutionClientFactory.Create(config, execExchange);
        var result = await client.AmendAsync(request, CancellationToken.None);

        WriteJson(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"amend failed: {ex.Message}");
        Environment.ExitCode = 2;
    }
});

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

        if (!TryParseMode(mode, out var execMode))
        {
            Console.Error.WriteLine("invalid --mode (expected: paper|testnet|mainnet)");
            Environment.ExitCode = 1;
            return;
        }

        if (!TryParseExchange(exchange, out var execExchange))
        {
            Console.Error.WriteLine("invalid --exchange (expected: hl|okx|bybit|mexc)");
            Environment.ExitCode = 1;
            return;
        }

        if (!TryParseOrderStatus(status, out var queryStatus))
        {
            Console.Error.WriteLine("invalid --status (expected: open|closed|all)");
            Environment.ExitCode = 1;
            return;
        }

        var config = BuildConfig(execMode, armLive);
        var arming = ExecutionSafety.ValidateArming(config);
        if (!arming.Ok)
        {
            Console.Error.WriteLine(arming.Error);
            Environment.ExitCode = 1;
            return;
        }

        if (execMode != ExecutionMode.Paper)
        {
            Console.Error.WriteLine("only paper mode is implemented in the CLI");
            Environment.ExitCode = 1;
            return;
        }

        var request = new QueryOrdersRequest(queryStatus, orderId, symbol);
        var client = ExecutionClientFactory.Create(config, execExchange);
        var result = await client.QueryOrdersAsync(request, CancellationToken.None);

        WriteJson(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"query orders failed: {ex.Message}");
        Environment.ExitCode = 2;
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

        if (!TryParseMode(mode, out var execMode))
        {
            Console.Error.WriteLine("invalid --mode (expected: paper|testnet|mainnet)");
            Environment.ExitCode = 1;
            return;
        }

        if (!TryParseExchange(exchange, out var execExchange))
        {
            Console.Error.WriteLine("invalid --exchange (expected: hl|okx|bybit|mexc)");
            Environment.ExitCode = 1;
            return;
        }

        var config = BuildConfig(execMode, armLive);
        var arming = ExecutionSafety.ValidateArming(config);
        if (!arming.Ok)
        {
            Console.Error.WriteLine(arming.Error);
            Environment.ExitCode = 1;
            return;
        }

        if (execMode != ExecutionMode.Paper)
        {
            Console.Error.WriteLine("only paper mode is implemented in the CLI");
            Environment.ExitCode = 1;
            return;
        }

        var client = ExecutionClientFactory.Create(config, execExchange);
        var result = await client.QueryPositionsAsync(CancellationToken.None);

        WriteJson(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"query positions failed: {ex.Message}");
        Environment.ExitCode = 2;
    }
});

queryCommand.AddCommand(queryOrdersCommand);
queryCommand.AddCommand(queryPositionsCommand);

root.AddCommand(placeCommand);
root.AddCommand(cancelCommand);
root.AddCommand(cancelAllCommand);
root.AddCommand(amendCommand);
root.AddCommand(queryCommand);

try
{
    var exitCode = await root.InvokeAsync(args);
    return Environment.ExitCode != 0 ? Environment.ExitCode : exitCode;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"fatal: {ex.Message}");
    return 2;
}

static bool TryParseMode(string value, out ExecutionMode mode)
{
    if (string.Equals(value, "paper", StringComparison.OrdinalIgnoreCase))
    {
        mode = ExecutionMode.Paper;
        return true;
    }

    if (string.Equals(value, "testnet", StringComparison.OrdinalIgnoreCase))
    {
        mode = ExecutionMode.Testnet;
        return true;
    }

    if (string.Equals(value, "mainnet", StringComparison.OrdinalIgnoreCase))
    {
        mode = ExecutionMode.Mainnet;
        return true;
    }

    mode = ExecutionMode.Paper;
    return false;
}

static bool TryParseExchange(string value, out string exchange)
{
    if (string.Equals(value, "hl", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "okx", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "bybit", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "mexc", StringComparison.OrdinalIgnoreCase))
    {
        exchange = value.ToLowerInvariant();
        return true;
    }

    exchange = "hl";
    return false;
}

static bool TryParseOrderStatus(string value, out OrderQueryStatus status)
{
    if (string.Equals(value, "open", StringComparison.OrdinalIgnoreCase))
    {
        status = OrderQueryStatus.Open;
        return true;
    }

    if (string.Equals(value, "closed", StringComparison.OrdinalIgnoreCase))
    {
        status = OrderQueryStatus.Closed;
        return true;
    }

    if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase))
    {
        status = OrderQueryStatus.All;
        return true;
    }

    status = OrderQueryStatus.Open;
    return false;
}

static bool TryParseSide(string value, out OrderSide side)
{
    if (string.Equals(value, "buy", StringComparison.OrdinalIgnoreCase))
    {
        side = OrderSide.Buy;
        return true;
    }

    if (string.Equals(value, "sell", StringComparison.OrdinalIgnoreCase))
    {
        side = OrderSide.Sell;
        return true;
    }

    side = OrderSide.Buy;
    return false;
}

static bool TryParseType(string value, out OrderType type)
{
    if (string.Equals(value, "market", StringComparison.OrdinalIgnoreCase))
    {
        type = OrderType.Market;
        return true;
    }

    if (string.Equals(value, "limit", StringComparison.OrdinalIgnoreCase))
    {
        type = OrderType.Limit;
        return true;
    }

    type = OrderType.Market;
    return false;
}

static void WriteJson<T>(T payload)
{
    var line = JsonSerializer.Serialize(payload);
    Console.Out.WriteLine(line);
}

static ExecutionConfig BuildConfig(ExecutionMode mode, bool armLiveFlag)
{
    var armEnv = Environment.GetEnvironmentVariable("XWS_EXEC_ARM");
    var paperStatePath = mode == ExecutionMode.Paper
        ? Path.Combine(Environment.CurrentDirectory, "artifacts", "paper", "state.json")
        : null;
    return new ExecutionConfig(mode, armLiveFlag, armEnv, PaperStatePath: paperStatePath);
}
