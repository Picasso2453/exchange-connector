using System.CommandLine;
using xws.Core.Output;
using xws.Core.Subscriptions;
using xws.Core.WebSocket;
using xws.Exchanges.Hyperliquid;

var root = new RootCommand("xws CLI");

var hlCommand = new Command("hl", "Hyperliquid adapter");

var hlSymbolsCommand = new Command("symbols", "List available symbols/instruments");
hlSymbolsCommand.SetHandler(() =>
{
    Logger.Info("hl symbols: not implemented");
});

var hlSubscribeCommand = new Command("subscribe", "Subscribe to Hyperliquid streams");

var tradesCommand = new Command("trades", "Subscribe to trades stream");
var tradesSymbolOption = new Option<string>("--symbol", "Native coin symbol")
{
    IsRequired = true
};
tradesCommand.AddOption(tradesSymbolOption);
tradesCommand.SetHandler(async (string symbol) =>
{
    using var cts = new CancellationTokenSource();
    var cancelLogged = 0;
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        if (Interlocked.Exchange(ref cancelLogged, 1) == 0)
        {
            Logger.Info("shutdown requested");
        }
        cts.Cancel();
    };

    var subscription = HyperliquidWs.BuildTradesSubscription(symbol);
    var writer = new JsonlWriter();
    var registry = new SubscriptionRegistry();
    registry.Add(subscription);
    var runner = new WebSocketRunner(writer, registry);

    var exitCode = await runner.RunAsync(new Uri(HyperliquidWs.MainnetUrl), cts.Token);
    Environment.ExitCode = exitCode;
}, tradesSymbolOption);

var positionsCommand = new Command("positions", "Subscribe to positions/account stream");
positionsCommand.SetHandler(() =>
{
    Logger.Info("hl subscribe positions: not implemented");
});

hlSubscribeCommand.AddCommand(tradesCommand);
hlSubscribeCommand.AddCommand(positionsCommand);

hlCommand.AddCommand(hlSymbolsCommand);
hlCommand.AddCommand(hlSubscribeCommand);

root.AddCommand(hlCommand);

return await root.InvokeAsync(args);
