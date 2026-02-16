using System.CommandLine;

using xws.Commands;
using xws.Config;
using xws.Core.Shared.Logging;

Logger.Configure(Console.Error.WriteLine, Console.Error.WriteLine);

var root = new RootCommand("xws CLI");
var dotenvOption = new Option<string?>("--dotenv", "Load environment variables from a .env file");
var noDotenvOption = new Option<bool>("--no-dotenv", "Disable loading .env files");
root.AddGlobalOption(dotenvOption);
root.AddGlobalOption(noDotenvOption);

root.AddCommand(DevCommands.Build());
root.AddCommand(MexcCommands.Build());
root.AddCommand(MuxCommands.Build());
root.AddCommand(HyperliquidCommands.Build());

var parseResult = root.Parse(args);
var noDotenv = parseResult.GetValueForOption(noDotenvOption);
var dotenvPath = parseResult.GetValueForOption(dotenvOption);

if (!noDotenv)
{
    if (!string.IsNullOrWhiteSpace(dotenvPath))
    {
        var result = DotEnvLoader.Load(dotenvPath, required: true);
        if (!result.Loaded)
        {
            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                Logger.Error(result.Error);
            }
            return 1;
        }

        Logger.Info($"Loaded .env from {dotenvPath}");
    }
    else
    {
        var defaultPath = Path.Combine(Environment.CurrentDirectory, ".env");
        var result = DotEnvLoader.Load(defaultPath, required: false);
        if (result.Loaded)
        {
            Logger.Info($"Loaded .env from {defaultPath}");
        }
    }
}

try
{
    var exitCode = await root.InvokeAsync(args);
    return Environment.ExitCode != 0 ? Environment.ExitCode : exitCode;
}
catch (Exception ex)
{
    Logger.Error($"Fatal error. Unhandled exception occurred. {ex.Message}. Re-run with --help or file an issue.");
    return 2;
}
