using System.CommandLine;
using Xws.Exec.Cli;

var root = new RootCommand("xws.exec CLI");
ExecutionCommands.Configure(root);

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
