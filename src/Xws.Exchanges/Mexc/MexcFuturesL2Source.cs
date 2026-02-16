using System.Threading.Channels;
using Xws.Data.Output;
using Xws.Exchanges.Mexc.WebSocket;
using Xws.Data.Shared.Logging;

namespace Xws.Exchanges.Mexc;

public static class MexcFuturesL2Source
{
    private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(20);

    public static async Task RunL2Async(
        string[] symbols,
        ChannelWriter<EnvelopeV1> writer,
        CancellationToken cancellationToken)
    {
        if (symbols.Length == 0)
        {
            throw new InvalidOperationException("at least one symbol is required");
        }

        var config = MexcConfig.Load();
        var subscribePayloads = symbols.Select(symbol => System.Text.Json.JsonSerializer.Serialize(new
        {
            method = "sub.depth",
            param = new { symbol }
        }));

        await MEXCWebSocketClient.RunAsync(
            config.FuturesWsUri,
            subscribePayloads,
            PingInterval,
            async (text, token) =>
            {
                if (MexcFuturesL2Decoder.TryBuildEnvelope(text, symbols, out var envelope))
                {
                    await writer.WriteAsync(envelope, token);
                }
                else
                {
                    Logger.Info($"mexc fut text frame: {text}");
                }
            },
            cancellationToken);
    }
}

