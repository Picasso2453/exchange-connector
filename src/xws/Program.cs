using System.CommandLine;
using xws.Core.Output;

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
tradesCommand.SetHandler((string symbol) =>
{
    Logger.Info($"hl subscribe trades: not implemented (symbol={symbol})");
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
