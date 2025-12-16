using System.Text.Json;

namespace ToonFormat.SpecGenerator;

internal static class SpecSerializer
{
    public static T Deserialize<T>(string input)
    {
        return JsonSerializer.Deserialize<T>(input, jsonSerializerOptions) ?? throw new InvalidOperationException("Deserialization resulted in null");
    }

    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, jsonSerializerOptions);
    }

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
