namespace Xws.Abstractions;

public interface IMessageParser<T>
{
    bool TryParse(string message, out T result);
}
