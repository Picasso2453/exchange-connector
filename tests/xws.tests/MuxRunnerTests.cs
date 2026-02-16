using System.Threading.Channels;
using Xws.Data.Mux;
using Xws.Data.Output;

namespace xws.tests;

public sealed class MuxRunnerTests
{
    [Fact]
    public async Task MuxRunner_CombinesMultipleProducers()
    {
        var lines = new List<string>();
        var writer = new JsonlWriter(line => lines.Add(line));
        var runner = new MuxRunner(writer);

        var producers = new List<Func<ChannelWriter<EnvelopeV1>, CancellationToken, Task>>
        {
            async (channel, token) =>
            {
                await channel.WriteAsync(BuildEnvelope("hl", "trades", "SOL"), token);
            },
            async (channel, token) =>
            {
                await channel.WriteAsync(BuildEnvelope("okx", "l2", "BTC-USDT-SWAP"), token);
            }
        };

        var options = new MuxRunnerOptions { MaxMessages = 2, Timeout = TimeSpan.FromSeconds(2) };
        var exitCode = await runner.RunAsync(producers, options, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(2, lines.Count);
    }

    private static EnvelopeV1 BuildEnvelope(string exchange, string type, string symbol)
    {
        return new EnvelopeV1(
            "xws.envelope.v1",
            exchange,
            "fut",
            type,
            new[] { symbol },
            DateTimeOffset.UtcNow.ToString("O"),
            new { symbol },
            "json");
    }
}
