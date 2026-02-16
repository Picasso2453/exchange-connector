using System.Text.Json.Serialization;

namespace Xws.Data.Output;

public sealed record EnvelopeV1(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("exchange")] string Exchange,
    [property: JsonPropertyName("market")] string? Market,
    [property: JsonPropertyName("stream")] string Stream,
    [property: JsonPropertyName("symbols")] string[]? Symbols,
    [property: JsonPropertyName("receivedAt")] string ReceivedAt,
    [property: JsonPropertyName("raw")] object Raw,
    [property: JsonPropertyName("rawEncoding")] string RawEncoding);
