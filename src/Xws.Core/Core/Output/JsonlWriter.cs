namespace xws.Core.Output;

public interface IJsonlWriter
{
    void WriteLine(string message);
}

public sealed class JsonlWriter : IJsonlWriter
{
    private readonly Action<string> _emit;

    public JsonlWriter(Action<string> emit)
    {
        _emit = emit ?? throw new ArgumentNullException(nameof(emit));
    }

    public void WriteLine(string message)
    {
        if (message is null)
        {
            return;
        }

        var cleaned = message.Replace("\r", string.Empty).Replace("\n", string.Empty);
        _emit(cleaned);
    }
}
