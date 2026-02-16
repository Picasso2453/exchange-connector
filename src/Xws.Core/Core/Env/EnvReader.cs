namespace Xws.Data.Env;

public static class EnvReader
{
    public static string? GetOptional(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string GetRequired(string name)
    {
        var value = GetOptional(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required env var: {name}");
        }

        return value;
    }
}
