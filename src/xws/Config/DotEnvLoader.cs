namespace xws.Config;

public static class DotEnvLoader
{
    public static DotEnvLoadResult Load(string path, bool required)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new DotEnvLoadResult(false, "dotenv path is required");
        }

        if (!File.Exists(path))
        {
            return required
                ? new DotEnvLoadResult(false, $"dotenv file not found: {path}")
                : new DotEnvLoadResult(false, null);
        }

        foreach (var line in File.ReadAllLines(path))
        {
            if (!TryParseLine(line, out var key, out var value))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var existing = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(existing))
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, value);
        }

        return new DotEnvLoadResult(true, null);
    }

    public static bool TryParseLine(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var trimmed = line.Trim();
        if (trimmed.StartsWith('#'))
        {
            return false;
        }

        var separator = trimmed.IndexOf('=');
        if (separator <= 0)
        {
            return false;
        }

        key = trimmed[..separator].Trim();
        value = trimmed[(separator + 1)..].Trim();

        if (value.Length >= 2 && value.StartsWith('\"') && value.EndsWith('\"'))
        {
            value = value[1..^1];
        }

        return true;
    }
}

public sealed record DotEnvLoadResult(bool Loaded, string? Error);
