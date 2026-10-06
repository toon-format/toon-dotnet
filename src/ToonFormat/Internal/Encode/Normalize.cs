using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Encode;

/// <summary>
/// Maps .NET values onto the JSON data model and classifies the resulting nodes.
/// </summary>
internal static class Normalize
{
    #region Normalization (object → JsonNode)

    /// <summary>
    /// Normalizes a .NET value to the JSON data model: primitives, dates, <c>System.Text.Json</c> nodes and elements,
    /// dictionaries, enumerables, and the public properties of other objects. Unsupported values become null.
    /// </summary>
    public static JsonNode? NormalizeValue(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case string s:
                return JsonValue.Create(RequireScalarValues(s, "string value"));
            case bool b:
                return JsonValue.Create(b);
            case double d:
                return NumericUtils.IsFinite(d) ? JsonValue.Create(d) : null;
            // Through the shortest digits, so 0.1f encodes as 0.1 rather than 0.10000000149011612.
            case float f:
                return NumericUtils.IsFinite(f) ? JsonValue.Create(ParseDouble(f.ToString("R", CultureInfo.InvariantCulture))) : null;
            case decimal dec:
                return JsonValue.Create(ParseDouble(dec.ToString(CultureInfo.InvariantCulture)));
            case ulong ul when ul > long.MaxValue:
                return JsonValue.Create((double)ul);
            case sbyte or byte or short or ushort or int or uint or long or ulong:
                return JsonValue.Create(Convert.ToInt64(value, CultureInfo.InvariantCulture));
            case DateTime dt:
                return JsonValue.Create(dt.ToString("O"));
            case DateTimeOffset dto:
                return JsonValue.Create(dto.ToString("O"));
            case JsonObject jsonObject:
                return NormalizeObject(jsonObject.Select(property => (property.Key, (object?)property.Value)));
            case JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text):
                return NormalizeValue(text);
            // System.Text.Json refuses to write NaN and ±Infinity, which the data model maps to null.
            case JsonValue jsonValue when jsonValue.TryGetValue<double>(out var d) && !NumericUtils.IsFinite(d):
                return null;
            case JsonValue jsonValue when jsonValue.TryGetValue<float>(out var f) && !NumericUtils.IsFinite(f):
                return null;
            // Other values may wrap any .NET type, so read them back as the JSON they serialize to.
            case JsonValue jsonValue:
                using (var document = JsonDocument.Parse(jsonValue.ToJsonString()))
                    return NormalizeValue(document.RootElement);
            case JsonElement element:
                return element.ValueKind switch
                {
                    JsonValueKind.Object => NormalizeObject(element.EnumerateObject().Select(property => (property.Name, (object?)property.Value))),
                    JsonValueKind.Array => NormalizeValue(element.EnumerateArray()),
                    JsonValueKind.String => NormalizeValue(element.GetString()),
                    JsonValueKind.Number when element.TryGetInt64(out var integer) => JsonValue.Create(integer),
                    JsonValueKind.Number when element.TryGetDouble(out var number) => NormalizeValue(number),
                    JsonValueKind.True or JsonValueKind.False => JsonValue.Create(element.GetBoolean()),
                    _ => null,
                };
            case IDictionary dict:
                return NormalizeObject(dict.Keys.Cast<object>().Select(key => (key.ToString() ?? string.Empty, (object?)dict[key])));
            case IEnumerable enumerable:
                var jsonArray = new JsonArray();
                foreach (var item in enumerable)
                    jsonArray.Add(NormalizeValue(item));
                return jsonArray;
        }

        // The primitives left here, char and the native integers, have no JSON form.
        if (value.GetType().IsPrimitive)
            return null;

        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanRead);
        return NormalizeObject(properties.Select(prop => (prop.Name, (object?)prop.GetValue(value))));
    }

    private static JsonObject NormalizeObject(IEnumerable<(string Key, object? Value)> entries)
    {
        var jsonObject = new JsonObject();
        foreach (var (key, value) in entries)
            jsonObject[RequireScalarValues(key, "object key")] = NormalizeValue(value);
        return jsonObject;
    }

    private static double ParseDouble(string number) => double.Parse(number, CultureInfo.InvariantCulture);

    // A lone surrogate has no UTF-8 form, so emitting it would silently substitute U+FFFD.
    private static string RequireScalarValues(string value, string context)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                i++;
            else if (char.IsSurrogate(value[i]))
                throw ToonFormatException.Validation($"Cannot encode {context} containing an unpaired surrogate U+{(int)value[i]:X4} at index {i}");
        }

        return value;
    }

    #endregion

    #region Type guards

    /// <summary>
    /// Whether the node is null, a string, a number, or a boolean.
    /// </summary>
    public static bool IsJsonPrimitive(JsonNode? value) => value is null or JsonValue;

    #endregion

    #region Array type detection

    public static bool IsArrayOfPrimitives(JsonArray array) => array.All(IsJsonPrimitive);

    public static bool IsArrayOfObjects(JsonArray array) => array.All(item => item is JsonObject);

    #endregion
}
