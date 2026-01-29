using System.Text.Json;

namespace xws.Core.Output;

public sealed class EnvelopeWriter : IJsonlWriter
{
    private const string EnvelopeType = "xws.envelope.v1";
    private readonly string _exchange;
    private readonly string _stream;
    private readonly string? _market;
    private readonly string[]? _symbols;

    public EnvelopeWriter(string exchange, string stream, string? market, string[]? symbols)
    {
        _exchange = exchange;
        _stream = stream;
        _market = market;
        _symbols = symbols;
    }

    public void WriteLine(string message)
    {
        WriteRawJson(message);
    }

    public void WriteRawJson(string rawJson)
    {
        if (rawJson is null)
        {
            return;
        }

        object rawPayload;
        var rawEncoding = "json";

        try
        {
            rawPayload = JsonSerializer.Deserialize<JsonElement>(rawJson);
        }
        catch
        {
            rawPayload = rawJson;
            rawEncoding = "text";
        }

        var envelope = new EnvelopeV1(
            EnvelopeType,
            _exchange,
            _market,
            _stream,
            _symbols,
            DateTimeOffset.UtcNow.ToString("O"),
            rawPayload,
            rawEncoding);

        var line = JsonSerializer.Serialize(envelope);
        Console.Out.WriteLine(line);
    }

    public void WriteRawObject(object rawObject)
    {
        var rawPayload = JsonSerializer.SerializeToElement(rawObject);
        var envelope = new EnvelopeV1(
            EnvelopeType,
            _exchange,
            _market,
            _stream,
            _symbols,
            DateTimeOffset.UtcNow.ToString("O"),
            rawPayload,
            "json");

        var line = JsonSerializer.Serialize(envelope);
        Console.Out.WriteLine(line);
    }
}
