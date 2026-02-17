using Connector.Core.Transport;

namespace Connector.Core.Abstractions;

/// <summary>
/// Low-level REST transport: send HTTP requests, return responses.
/// </summary>
public interface IRestTransport
{
    Task<TransportRestResponse> SendAsync(TransportRestRequest request, CancellationToken ct);
}
