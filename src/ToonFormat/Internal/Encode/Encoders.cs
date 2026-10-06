using System.Text.Json.Nodes;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Encode;

internal sealed class ResolvedEncodeOptions(int indentSize, char delimiter)
{
    public int IndentSize { get; } = indentSize;
    public char Delimiter { get; } = delimiter;
}

/// <summary>
/// Encodes normalized <see cref="JsonNode"/> values as TOON lines.
/// </summary>
internal static class Encoders
{
    public static string EncodeValue(JsonNode? value, ResolvedEncodeOptions options)
    {
        if (Normalize.IsJsonPrimitive(value))
        {
            // Unquoted, a leading U+FEFF would read as the document's byte-order mark and vanish on decode.
            if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text) && text.Length > 0 && text[0] == Constants.BYTE_ORDER_MARK)
                return Primitives.Quote(text);

            return Primitives.EncodePrimitive(value, options.Delimiter);
        }

        var writer = new LineWriter(options.IndentSize);

        if (value is JsonArray array)
            EncodeArray(null, array, writer, 0, options);
        else if (value is JsonObject obj && Tabular.ExtractKeyedTabularFields(obj) is { } keyedFields)
            EncodeKeyedObject(null, obj, keyedFields, writer, 0, options);
        else if (value is JsonObject plain)
            EncodeObject(plain, writer, 0, options);

        return writer.ToString();
    }

    #region Objects

    private static void EncodeObject(JsonObject value, LineWriter writer, int depth, ResolvedEncodeOptions options)
    {
        foreach (var property in value)
            EncodeKeyValuePair(property.Key, property.Value, writer, depth, options);
    }

    private static void EncodeKeyValuePair(string key, JsonNode? value, LineWriter writer, int depth, ResolvedEncodeOptions options)
    {
        var encodedKey = Primitives.EncodeKey(key);

        if (Normalize.IsJsonPrimitive(value))
        {
            writer.Push(depth, $"{encodedKey}: {Primitives.EncodePrimitive(value, options.Delimiter)}");
        }
        else if (value is JsonArray array)
        {
            EncodeArray(key, array, writer, depth, options);
        }
        else if (value is JsonObject obj && Tabular.ExtractKeyedTabularFields(obj) is { } keyedFields)
        {
            EncodeKeyedObject(key, obj, keyedFields, writer, depth, options);
        }
        else if (value is JsonObject nested)
        {
            writer.Push(depth, $"{encodedKey}:");
            EncodeObject(nested, writer, depth + 1, options);
        }
    }

    private static void EncodeKeyedObject(string? key, JsonObject value, IReadOnlyList<FieldNode> fields, LineWriter writer, int depth, ResolvedEncodeOptions options)
    {
        writer.Push(depth, Primitives.FormatHeader(value.Count, key, fields, options.Delimiter, keyed: true));
        WriteEntryRows(value, fields, writer, depth + 1, options);
    }

    private static void WriteEntryRows(JsonObject value, IReadOnlyList<FieldNode> fields, LineWriter writer, int depth, ResolvedEncodeOptions options)
    {
        foreach (var entry in value)
            writer.Push(depth, $"{Primitives.EncodeKey(entry.Key)}: {JoinRowCells(entry.Value, fields, options)}");
    }

    #endregion

    #region Arrays

    private static void EncodeArray(string? key, JsonArray value, LineWriter writer, int depth, ResolvedEncodeOptions options)
    {
        if (value.Count == 0)
        {
            writer.Push(depth, key != null ? $"{Primitives.EncodeKey(key)}: []" : "[]");
            return;
        }

        if (Normalize.IsArrayOfPrimitives(value))
        {
            writer.Push(depth, EncodeInlineArrayLine(value, options.Delimiter, key));
            return;
        }

        if (TabularFields(value) is { } fields)
        {
            writer.Push(depth, Primitives.FormatHeader(value.Count, key, fields, options.Delimiter));
            WriteTabularRows(value, fields, writer, depth + 1, options);
            return;
        }

        writer.Push(depth, Primitives.FormatHeader(value.Count, key, null, options.Delimiter));
        foreach (var item in value)
            EncodeListItemValue(item, writer, depth + 1, options);
    }

    private static List<FieldNode>? TabularFields(JsonArray value) =>
        Normalize.IsArrayOfObjects(value) ? Tabular.ExtractTabularFields(value.Cast<JsonObject>().ToList()) : null;

    private static string EncodeInlineArrayLine(JsonArray values, char delimiter, string? key)
    {
        var header = Primitives.FormatHeader(values.Count, key, null, delimiter);
        return values.Count == 0 ? header : $"{header} {Primitives.EncodeAndJoinPrimitives(values, delimiter)}";
    }

    private static void WriteTabularRows(IEnumerable<JsonNode?> rows, IReadOnlyList<FieldNode> fields, LineWriter writer, int depth, ResolvedEncodeOptions options)
    {
        foreach (var row in rows)
            writer.Push(depth, JoinRowCells(row, fields, options));
    }

    private static string JoinRowCells(JsonNode? row, IReadOnlyList<FieldNode> fields, ResolvedEncodeOptions options)
    {
        var leaves = new List<JsonNode?>();
        Tabular.CollectRowLeaves((JsonObject)row!, fields, leaves);
        return Primitives.EncodeAndJoinPrimitives(leaves, options.Delimiter);
    }

    #endregion

    #region List items

    private static void EncodeListItemValue(JsonNode? value, LineWriter writer, int depth, ResolvedEncodeOptions options)
    {
        if (Normalize.IsJsonPrimitive(value))
        {
            writer.PushListItem(depth, Primitives.EncodePrimitive(value, options.Delimiter));
        }
        else if (value is JsonArray array)
        {
            // Encoders must not emit `- []`, so an empty inner array keeps its `[0]:` header.
            if (Normalize.IsArrayOfPrimitives(array))
            {
                writer.PushListItem(depth, EncodeInlineArrayLine(array, options.Delimiter, null));
                return;
            }

            writer.PushListItem(depth, Primitives.FormatHeader(array.Count, null, null, options.Delimiter));
            foreach (var item in array)
                EncodeListItemValue(item, writer, depth + 1, options);
        }
        else if (value is JsonObject obj)
        {
            EncodeObjectAsListItem(obj, writer, depth, options);
        }
    }

    /// <summary>
    /// Puts the first field on the hyphen line; rows or items of an array there sit at depth + 2,
    /// below the sibling fields at depth + 1.
    /// </summary>
    private static void EncodeObjectAsListItem(JsonObject obj, LineWriter writer, int depth, ResolvedEncodeOptions options)
    {
        if (obj.Count == 0)
        {
            writer.Push(depth, Constants.LIST_ITEM_MARKER.ToString());
            return;
        }

        var first = obj.First();
        var encodedKey = Primitives.EncodeKey(first.Key);

        if (Normalize.IsJsonPrimitive(first.Value))
        {
            writer.PushListItem(depth, $"{encodedKey}: {Primitives.EncodePrimitive(first.Value, options.Delimiter)}");
        }
        else if (first.Value is JsonArray array)
        {
            if (array.Count == 0)
            {
                writer.PushListItem(depth, $"{encodedKey}: []");
            }
            else if (Normalize.IsArrayOfPrimitives(array))
            {
                writer.PushListItem(depth, EncodeInlineArrayLine(array, options.Delimiter, first.Key));
            }
            else if (TabularFields(array) is { } fields)
            {
                writer.PushListItem(depth, Primitives.FormatHeader(array.Count, first.Key, fields, options.Delimiter));
                WriteTabularRows(array, fields, writer, depth + 2, options);
            }
            else
            {
                writer.PushListItem(depth, Primitives.FormatHeader(array.Count, first.Key, null, options.Delimiter));
                foreach (var item in array)
                    EncodeListItemValue(item, writer, depth + 2, options);
            }
        }
        else if (first.Value is JsonObject keyed && Tabular.ExtractKeyedTabularFields(keyed) is { } keyedFields)
        {
            writer.PushListItem(depth, Primitives.FormatHeader(keyed.Count, first.Key, keyedFields, options.Delimiter, keyed: true));
            WriteEntryRows(keyed, keyedFields, writer, depth + 2, options);
        }
        else if (first.Value is JsonObject nested)
        {
            writer.PushListItem(depth, $"{encodedKey}:");
            EncodeObject(nested, writer, depth + 2, options);
        }

        foreach (var property in obj.Skip(1))
            EncodeKeyValuePair(property.Key, property.Value, writer, depth + 1, options);
    }

    #endregion
}
