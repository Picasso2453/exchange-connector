namespace Connector.Core;

/// <summary>
/// Exception thrown by connector operations (transport errors, auth failures, etc.).
/// </summary>
public sealed class ConnectorException : Exception
{
    public int? StatusCode { get; }
    public string? ResponseBody { get; }

    public ConnectorException(string message) : base(message) { }

    public ConnectorException(string message, int statusCode, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public ConnectorException(string message, Exception innerException)
        : base(message, innerException) { }
}
