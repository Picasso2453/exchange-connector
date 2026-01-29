using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace xws.Exchanges.Mexc;

public sealed class MexcSpotTradeSubscriber
{
    private const int MaxSubscriptions = 30;

    public async Task<int> RunAsync(
        Uri wsUri,
        string[] symbols,
        int? maxMessages,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        if (symbols.Length == 0)
        {
            throw new InvalidOperationException("at least one symbol is required");
        }

        if (symbols.Length > MaxSubscriptions)
        {
            throw new InvalidOperationException($"mexc spot supports max {MaxSubscriptions} subscriptions per connection");
        }

        using var timeoutCts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : null;
        using var linkedCts = timeoutCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;
        var runToken = linkedCts?.Token ?? cancellationToken;

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(wsUri, runToken);

        var channels = symbols.Select(symbol => $"spot@public.aggre.deals.v3.api.pb@100ms@{symbol}").ToArray();
        var payload = new
        {
            method = "SUBSCRIPTION",
            @params = channels
        };

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, runToken);

        var buffer = new byte[8192];
        var messageCount = 0;

        while (!runToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            var segment = new ArraySegment<byte>(buffer);
            using var messageBuffer = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(segment, runToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return 0;
                }

                messageBuffer.Write(segment.Array!, segment.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Binary || result.MessageType == WebSocketMessageType.Text)
            {
                messageCount++;
                if (maxMessages.HasValue && messageCount >= maxMessages.Value)
                {
                    return 0;
                }
            }
        }

        if (timeoutCts?.IsCancellationRequested == true)
        {
            return 1;
        }

        return 0;
    }
}
