using System.Collections.Concurrent;
using System.Text.Json;
using Xws.Data.Output;

namespace Xws.Data.Tests;

public sealed class OutputChannelTests
{
    [Fact]
    public async Task OutputChannel_PreservesWholeJsonLines()
    {
        var output = new OutputChannel();
        var expectedCounts = new ConcurrentDictionary<string, int>();
        var producers = Enumerable.Range(0, 4)
            .Select(producerId => Task.Run(async () =>
            {
                for (var seq = 0; seq < 25; seq++)
                {
                    var payload = JsonSerializer.Serialize(new { producer = producerId, seq });
                    expectedCounts.AddOrUpdate(payload, 1, (_, count) => count + 1);
                    await output.Writer.WriteAsync(payload);
                }
            }))
            .ToArray();

        var readTask = Task.Run(async () =>
        {
            var lines = new List<string>();
            await foreach (var line in output.Reader.ReadAllAsync())
            {
                lines.Add(line);
            }

            return lines;
        });

        await Task.WhenAll(producers);
        output.Complete();

        var linesRead = await readTask;
        Assert.Equal(expectedCounts.Sum(kv => kv.Value), linesRead.Count);

        foreach (var line in linesRead)
        {
            JsonDocument.Parse(line);
            Assert.True(expectedCounts.TryGetValue(line, out var remaining));
            Assert.True(remaining > 0);
            expectedCounts[line] = remaining - 1;
        }
    }
}
