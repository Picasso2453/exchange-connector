using System.CommandLine;

var root = new RootCommand("xws CLI");

return await root.InvokeAsync(args);
