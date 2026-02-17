using System.Text.Json;
using System.Text.Json.Serialization;

namespace Connector.Core.Contracts;

/// <summary>
/// Shared JSON serialization options for all unified contract types.
/// camelCase property naming, enums as strings.
/// </summary>
public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = false
    };
}
