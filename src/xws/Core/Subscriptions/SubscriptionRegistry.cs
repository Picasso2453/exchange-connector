using System.Collections.Concurrent;

namespace xws.Core.Subscriptions;

public sealed class SubscriptionRegistry
{
    private readonly ConcurrentDictionary<SubscriptionKey, SubscriptionRequest> _subscriptions = new();

    public bool Add(SubscriptionRequest request)
    {
        return _subscriptions.TryAdd(request.Key, request);
    }

    public IReadOnlyCollection<SubscriptionRequest> GetAll()
    {
        return _subscriptions.Values.ToArray();
    }
}
