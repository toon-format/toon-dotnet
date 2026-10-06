#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Encode
{
    /// <summary>
    /// Normalization utilities for converting arbitrary .NET objects to JsonNode representations
    /// and type guards for JSON value classification.
    /// </summary>
    internal static class Normalize
    {
        #region Normalization (object → JsonNode)

        /// <summary>
        /// Normalizes a .NET value to the JSON data model: primitives, dates, dictionaries, enumerables,
        /// and the public properties of other objects. Unsupported values become null.
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
                    return NumericUtils.IsFinite(d) ? JsonValue.Create(d == 0 ? 0.0 : d) : null;
                case float f:
                    return NumericUtils.IsFinite(f) ? JsonValue.Create(f == 0 ? 0.0f : f) : null;
                case int i:
                    return JsonValue.Create(i);
                case long l:
                    return JsonValue.Create(l);
                case decimal dec:
                    return JsonValue.Create(dec);
                case byte by:
                    return JsonValue.Create(by);
                case sbyte sb:
                    return JsonValue.Create(sb);
                case short sh:
                    return JsonValue.Create(sh);
                case ushort us:
                    return JsonValue.Create(us);
                case uint ui:
                    return JsonValue.Create(ui);
                case ulong ul:
                    return JsonValue.Create(ul);
                case DateTime dt:
                    return JsonValue.Create(dt.ToString("O"));
                case DateTimeOffset dto:
                    return JsonValue.Create(dto.ToString("O"));
                case IDictionary dict:
                    var jsonObject = new JsonObject();
                    foreach (DictionaryEntry entry in dict)
                        jsonObject[RequireScalarValues(entry.Key?.ToString() ?? string.Empty, "object key")] = NormalizeValue(entry.Value);
                    return jsonObject;
                case IEnumerable enumerable:
                    var jsonArray = new JsonArray();
                    foreach (var item in enumerable)
                        jsonArray.Add(NormalizeValue(item));
                    return jsonArray;
            }

            // The primitives left here, char and the native integers, have no JSON form.
            if (value.GetType().IsPrimitive)
                return null;

            var properties = new JsonObject();
            foreach (var prop in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanRead))
                properties[prop.Name] = NormalizeValue(prop.GetValue(value));
            return properties;
        }

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
        /// Checks if a JsonNode is a primitive value (null, string, number, or boolean).
        /// </summary>
        public static bool IsJsonPrimitive(JsonNode? value)
        {
            if (value == null)
                return true;

            if (value is JsonValue jsonValue)
            {
                return jsonValue.TryGetValue<string>(out _)
                    || jsonValue.TryGetValue<bool>(out _)
                    || jsonValue.TryGetValue<int>(out _)
                    || jsonValue.TryGetValue<long>(out _)
                    || jsonValue.TryGetValue<double>(out _)
                    || jsonValue.TryGetValue<decimal>(out _);
            }

            return false;
        }

        #endregion

        #region Array type detection

        /// <summary>
        /// Checks if a JsonArray contains only primitive values.
        /// </summary>
        public static bool IsArrayOfPrimitives(JsonArray array)
        {
            return array.All(item => IsJsonPrimitive(item));
        }

        /// <summary>
        /// Checks if a JsonArray contains only objects.
        /// </summary>
        public static bool IsArrayOfObjects(JsonArray array)
        {
            return array.All(item => item is JsonObject);
        }

        #endregion
    }
}
