using System.Text.Json;

namespace xws.Core.Output;

public sealed class EnvelopeWriter : IJsonlWriter
{
    private const string EnvelopeType = "xws.envelope.v1";
    private readonly string _exchange;
    private readonly string _stream;
    private readonly string? _market;
    private readonly string[]? _symbols;
    private readonly Action<string> _emit;

    public EnvelopeWriter(string exchange, string stream, string? market, string[]? symbols, Action<string> emit)
    {
        _exchange = exchange;
        _stream = stream;
        _market = market;
        _symbols = symbols;
        _emit = emit ?? throw new ArgumentNullException(nameof(emit));
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
        _emit(line);
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
        _emit(line);
    }
}
