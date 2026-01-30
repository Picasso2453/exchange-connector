using System.CommandLine;
using System.Text.Json;
using Xws.Exec;

var root = new RootCommand("xws.exec CLI");

var modeOption = new Option<string>("--mode", () => "paper", "Execution mode: paper|testnet|mainnet");
var armLiveOption = new Option<bool>("--arm-live", "Arm live trading (mainnet only)");

var placeCommand = new Command("place", "Place an order");
var symbolOption = new Option<string>("--symbol") { IsRequired = true };
var sideOption = new Option<string>("--side") { IsRequired = true };
var typeOption = new Option<string>("--type") { IsRequired = true };
var sizeOption = new Option<decimal>("--size") { IsRequired = true };
var priceOption = new Option<decimal?>("--price");
var clientOrderIdOption = new Option<string?>("--client-order-id");

placeCommand.AddOption(modeOption);
placeCommand.AddOption(armLiveOption);
placeCommand.AddOption(symbolOption);
placeCommand.AddOption(sideOption);
placeCommand.AddOption(typeOption);
placeCommand.AddOption(sizeOption);
placeCommand.AddOption(priceOption);
placeCommand.AddOption(clientOrderIdOption);

placeCommand.SetHandler(async (string mode, bool armLive, string symbol, string side, string type, decimal size, decimal? price, string? clientOrderId) =>
{
    if (!TryParseMode(mode, out var execMode))
    {
        Console.Error.WriteLine("invalid --mode");
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
        Console.Error.WriteLine("invalid --side");
        Environment.ExitCode = 1;
        return;
    }

    if (!TryParseType(type, out var orderType))
    {
        Console.Error.WriteLine("invalid --type");
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
        Console.Error.WriteLine("mode not implemented yet");
        Environment.ExitCode = 1;
        return;
    }

    var request = new PlaceOrderRequest(
        symbol,
        orderSide,
        orderType,
        size,
        price,
        clientOrderId);

    var idempotency = ExecutionSafety.ValidateIdempotency(config, request);
    if (!idempotency.Ok)
    {
        Console.Error.WriteLine(idempotency.Error);
        Environment.ExitCode = 1;
        return;
    }

    var client = new PaperExecutionClient();
    var result = await client.PlaceAsync(request, CancellationToken.None);

    WriteJson(result);
}, modeOption, armLiveOption, symbolOption, sideOption, typeOption, sizeOption, priceOption, clientOrderIdOption);

var cancelCommand = new Command("cancel", "Cancel an order by orderId");
var orderIdOption = new Option<string>("--order-id") { IsRequired = true };

cancelCommand.AddOption(modeOption);
cancelCommand.AddOption(armLiveOption);
cancelCommand.AddOption(orderIdOption);

cancelCommand.SetHandler(async (string mode, bool armLive, string orderId) =>
{
    if (!TryParseMode(mode, out var execMode))
    {
        Console.Error.WriteLine("invalid --mode");
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
        Console.Error.WriteLine("mode not implemented yet");
        Environment.ExitCode = 1;
        return;
    }

    var client = new PaperExecutionClient();
    var result = await client.CancelAsync(new CancelOrderRequest(OrderId: orderId), CancellationToken.None);

    WriteJson(result);
}, modeOption, armLiveOption, orderIdOption);

var cancelAllCommand = new Command("cancel-all", "Cancel all open orders");
var symbolFilterOption = new Option<string?>("--symbol");

cancelAllCommand.AddOption(modeOption);
cancelAllCommand.AddOption(armLiveOption);
cancelAllCommand.AddOption(symbolFilterOption);

cancelAllCommand.SetHandler(async (string mode, bool armLive, string? symbol) =>
{
    if (!TryParseMode(mode, out var execMode))
    {
        Console.Error.WriteLine("invalid --mode");
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
        Console.Error.WriteLine("mode not implemented yet");
        Environment.ExitCode = 1;
        return;
    }

    var client = new PaperExecutionClient();
    var result = await client.CancelAllAsync(new CancelAllRequest(symbol), CancellationToken.None);

    WriteJson(result);
}, modeOption, armLiveOption, symbolFilterOption);

root.AddCommand(placeCommand);
root.AddCommand(cancelCommand);
root.AddCommand(cancelAllCommand);

var exitCode = await root.InvokeAsync(args);
return Environment.ExitCode != 0 ? Environment.ExitCode : exitCode;

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
    return new ExecutionConfig(mode, armLiveFlag, armEnv);
}
