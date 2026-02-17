namespace Connector.Core.Transport;

/// <summary>
/// Outbound WebSocket message (exchange-native format).
/// </summary>
public sealed class TransportWsMessage
{
    public required string Payload { get; init; }
}

/// <summary>
/// Inbound WebSocket message (exchange-native format).
/// </summary>
public sealed class TransportWsInbound
{
    public required string Payload { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }
    public byte[]? RawBytes { get; init; }
}

/// <summary>
/// Outbound REST request (exchange-native format).
/// </summary>
public sealed class TransportRestRequest
{
    public required HttpMethod Method { get; init; }
    public required string Path { get; init; }
    public string? Body { get; init; }
    public string? ContentType { get; init; }
    public Dictionary<string, string>? Headers { get; set; }
    public Dictionary<string, string>? QueryParams { get; init; }
}

/// <summary>
/// Inbound REST response (exchange-native format).
/// </summary>
public sealed class TransportRestResponse
{
    public required int StatusCode { get; init; }
    public required string Body { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
}

/// <summary>
/// Transport-level error.
/// </summary>
public sealed class TransportError
{
    public required int StatusCode { get; init; }
    public required string Body { get; init; }
    public string? Code { get; init; }
}
