using System.Text.Json;

namespace Xws.Data.Dev;

public static class DevEmitter
{
    public static IEnumerable<string> BuildLines(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            var payload = new
            {
                type = "xws.dev",
                seq = i,
                ts = DateTimeOffset.UtcNow.ToString("O")
            };
            yield return JsonSerializer.Serialize(payload);
        }
    }
}
