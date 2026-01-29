namespace xws.Core.Output;

public static class Logger
{
    public static void Info(string message)
    {
        Console.Error.WriteLine(message);
    }

    public static void Error(string message)
    {
        Console.Error.WriteLine(message);
    }
}
