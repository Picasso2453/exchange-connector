namespace Xws.Data.Shared.Logging;

public static class Logger
{
    private static Action<string> _infoSink = _ => { };
    private static Action<string> _errorSink = _ => { };

    public static void Configure(Action<string> infoSink, Action<string> errorSink)
    {
        _infoSink = infoSink ?? (_ => { });
        _errorSink = errorSink ?? (_ => { });
    }

    public static void Info(string message)
    {
        _infoSink(message);
    }

    public static void Error(string message)
    {
        _errorSink(message);
    }
}
