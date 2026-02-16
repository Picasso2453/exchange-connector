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
    ExecutionCommandHelpers.Fail($"Fatal error. Unhandled exception occurred. {ex.Message}. Re-run with --help or file an issue.", 2);
    return 2;
}
