using Connector.Core.Contracts;
using Connector.Core.Transport;

namespace Connector.Core.Abstractions;

/// <summary>
/// Translates between unified REST requests/responses and exchange-native HTTP requests/responses.
/// </summary>
public interface IRestTranslator
{
    TransportRestRequest ToExchangeRequest<TResponse>(UnifiedRestRequest<TResponse> request);
    TResponse FromExchangeResponse<TResponse>(UnifiedRestRequest<TResponse> request, TransportRestResponse response);
}
