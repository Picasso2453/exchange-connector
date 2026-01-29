namespace xws.Core.Output;

public interface IJsonlWriter
{
    void WriteLine(string message);
}

public sealed class JsonlWriter : IJsonlWriter
{
    public void WriteLine(string message)
    {
        if (message is null)
        {
            return;
        }

        var cleaned = message.Replace("\r", string.Empty).Replace("\n", string.Empty);
        Console.Out.WriteLine(cleaned);
    }
}
