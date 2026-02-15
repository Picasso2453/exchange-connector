namespace xws.Core.Shared.Interfaces;

public interface IMessageParser<T>
{
    bool TryParse(string message, out T result);
}
