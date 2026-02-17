using Connector.Core.Contracts;
using Connector.Core.Transport;

namespace Connector.Core.Abstractions;

/// <summary>
/// Translates between unified WS requests/events and exchange-native WS messages.
/// </summary>
public interface IWsTranslator
{
    IEnumerable<TransportWsMessage> ToExchangeSubscribe(UnifiedWsSubscribeRequest request);
    IEnumerable<TransportWsMessage> ToExchangeUnsubscribe(UnifiedWsUnsubscribeRequest request);
    IEnumerable<UnifiedWsEvent> FromExchangeMessage(TransportWsInbound inbound);
}
