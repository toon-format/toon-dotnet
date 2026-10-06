#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Toon.Format.Internal.Encode
{
    internal class ResolvedEncodeOptions
    {
        public int Indent { get; set; } = 2;
        public char Delimiter { get; set; } = Constants.COMMA;
    }

    /// <summary>
    /// Main encoding functions for converting normalized JsonNode values to TOON format.
    /// </summary>
    internal static class Encoders
    {
        #region Encode normalized JsonValue

        /// <summary>
        /// Encodes a normalized JsonNode value to TOON format string.
        /// </summary>
        public static string EncodeValue(JsonNode? value, ResolvedEncodeOptions options)
        {
            if (Normalize.IsJsonPrimitive(value))
            {
                return Primitives.EncodePrimitive(value, options.Delimiter);
            }

            var writer = new LineWriter(options.Indent);

            if (Normalize.IsJsonArray(value))
            {
                EncodeArray(null, (JsonArray)value!, writer, 0, options);
            }
            else if (Normalize.IsJsonObject(value))
            {
                EncodeObject((JsonObject)value!, writer, 0, options);
            }

            return writer.ToString();
        }

        #endregion

        #region Object encoding

        /// <summary>
        /// Encodes a JsonObject as key-value pairs.
        /// </summary>
        public static void EncodeObject(JsonObject value, LineWriter writer, int depth, ResolvedEncodeOptions options)
        {
            foreach (var kvp in value)
            {
                EncodeKeyValuePair(kvp.Key, kvp.Value, writer, depth, options);
            }
        }

        /// <summary>
        /// Encodes a single key-value pair.
        /// </summary>
        public static void EncodeKeyValuePair(string key, JsonNode? value, LineWriter writer, int depth, ResolvedEncodeOptions options)
        {
            var encodedKey = Primitives.EncodeKey(key);

            if (Normalize.IsJsonPrimitive(value))
            {
                writer.Push(depth, $"{encodedKey}{Constants.COLON} {Primitives.EncodePrimitive(value, options.Delimiter)}");
            }
            else if (Normalize.IsJsonArray(value))
            {
                EncodeArray(key, (JsonArray)value!, writer, depth, options);
            }
            else if (Normalize.IsJsonObject(value))
            {
                writer.Push(depth, $"{encodedKey}{Constants.COLON}");
                var obj = value!.AsObject();
                if (!Normalize.IsEmptyObject(obj))
                {
                    EncodeObject(obj, writer, depth + 1, options);
                }
            }
        }

        #endregion

        #region Array encoding

        /// <summary>
        /// Encodes a JsonArray with appropriate formatting (inline, tabular, or expanded).
        /// </summary>
        public static void EncodeArray(
            string? key,
            JsonArray value,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            if (value.Count == 0)
            {
                var header = Primitives.FormatHeader(0, key, null, options.Delimiter);
                writer.Push(depth, header);
                return;
            }

            if (Normalize.IsArrayOfPrimitives(value))
            {
                var formatted = EncodeInlineArrayLine(value, options.Delimiter, key);
                writer.Push(depth, formatted);
                return;
            }

            if (Normalize.IsArrayOfArrays(value))
            {
                var allPrimitiveArrays = value.All(item =>
                    item is JsonArray arr && Normalize.IsArrayOfPrimitives(arr));

                if (allPrimitiveArrays)
                {
                    EncodeArrayOfArraysAsListItems(key, value.Cast<JsonArray>().ToList(), writer, depth, options);
                    return;
                }
            }

            if (Normalize.IsArrayOfObjects(value))
            {
                var objects = value.Cast<JsonObject>().ToList();
                var header = ExtractTabularHeader(objects);
                if (header != null)
                {
                    EncodeArrayOfObjectsAsTabular(key, objects, header, writer, depth, options);
                }
                else
                {
                    EncodeMixedArrayAsListItems(key, value, writer, depth, options);
                }
                return;
            }

            EncodeMixedArrayAsListItems(key, value, writer, depth, options);
        }

        #endregion

        #region Array of arrays (expanded format)

        /// <summary>
        /// Encodes an array of arrays as list items.
        /// </summary>
        public static void EncodeArrayOfArraysAsListItems(
            string? prefix,
            IReadOnlyList<JsonArray> values,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            var header = Primitives.FormatHeader(values.Count, prefix, null, options.Delimiter);
            writer.Push(depth, header);

            foreach (var arr in values)
            {
                if (Normalize.IsArrayOfPrimitives(arr))
                {
                    var inline = EncodeInlineArrayLine(arr, options.Delimiter, null);
                    writer.PushListItem(depth + 1, inline);
                }
            }
        }

        /// <summary>
        /// Encodes an array as a single inline line with header.
        /// </summary>
        public static string EncodeInlineArrayLine(
            JsonArray values,
            char delimiter,
            string? prefix = null)
        {
            var header = Primitives.FormatHeader(values.Count, prefix, null, delimiter);

            if (values.Count == 0)
            {
                return header;
            }

            var joinedValue = Primitives.EncodeAndJoinPrimitives(values, delimiter);
            return $"{header} {joinedValue}";
        }

        #endregion

        #region Array of objects (tabular format)

        /// <summary>
        /// Encodes an array of objects in tabular format.
        /// </summary>
        public static void EncodeArrayOfObjectsAsTabular(
            string? prefix,
            IReadOnlyList<JsonObject> rows,
            IReadOnlyList<string> header,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            var formattedHeader = Primitives.FormatHeader(rows.Count, prefix, header, options.Delimiter);
            writer.Push(depth, formattedHeader);

            WriteTabularRows(rows, header, writer, depth + 1, options);
        }

        /// <summary>
        /// Extracts a uniform header from an array of objects if all objects have the same keys.
        /// Returns null if the array cannot be represented in tabular format.
        /// </summary>
        public static IReadOnlyList<string>? ExtractTabularHeader(IReadOnlyList<JsonObject> rows)
        {
            if (rows.Count == 0)
                return null;

            var firstRow = rows[0];
            var firstKeys = firstRow.Select(kvp => kvp.Key).ToList();

            if (firstKeys.Count == 0)
                return null;

            if (IsTabularArray(rows, firstKeys))
            {
                return firstKeys;
            }

            return null;
        }

        /// <summary>
        /// Checks if an array of objects can be represented in tabular format.
        /// All objects must have the same keys and all values must be primitives.
        /// </summary>
        public static bool IsTabularArray(
            IReadOnlyList<JsonObject> rows,
            IReadOnlyList<string> header)
        {
            foreach (var row in rows)
            {
                var keys = row.Select(kvp => kvp.Key).ToList();

                // All objects must have the same keys (but order can differ)
                if (keys.Count != header.Count)
                    return false;

                foreach (var key in header)
                {
                    if (!row.ContainsKey(key))
                        return false;

                    if (!Normalize.IsJsonPrimitive(row[key]))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Writes tabular rows to the writer.
        /// The depth parameter determines the indentation level of the rows.
        /// Per SPEC v3.0 §10: When writing rows for a tabular array on a hyphen line,
        /// depth should be +2 relative to the hyphen line (not +1 as in normal cases).
        /// </summary>
        private static void WriteTabularRows(
            IReadOnlyList<JsonObject> rows,
            IReadOnlyList<string> header,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            foreach (var joinedValue in rows.Select(row =>
                Primitives.EncodeAndJoinPrimitives(header.Select(key => row[key]).ToList(), options.Delimiter)))
            {
                writer.Push(depth, joinedValue);
            }
        }

        #endregion

        #region Array of objects (expanded format)

        /// <summary>
        /// Encodes a mixed array as list items (expanded format).
        /// </summary>
        public static void EncodeMixedArrayAsListItems(
            string? prefix,
            JsonArray items,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            var header = Primitives.FormatHeader(items.Count, prefix, null, options.Delimiter);
            writer.Push(depth, header);

            foreach (var item in items)
            {
                EncodeListItemValue(item, writer, depth + 1, options);
            }
        }

        /// <summary>
        /// Encodes an object as a list item with special formatting for the first property.
        /// Per SPEC v3.0 §10: When the first field is an array (tabular or list), the array header
        /// appears on the hyphen line, array contents appear at depth +2, and sibling fields at depth +1.
        /// This ensures visual clarity and LLM readability for nested structures.
        /// </summary>
        public static void EncodeObjectAsListItem(JsonObject obj, LineWriter writer, int depth, ResolvedEncodeOptions options)
        {
            var keys = obj.Select(kvp => kvp.Key).ToList();

            if (keys.Count == 0)
            {
                writer.Push(depth, Constants.LIST_ITEM_MARKER.ToString());
                return;
            }

            // First key-value on the same line as "- "
            var firstKey = keys[0];
            var encodedKey = Primitives.EncodeKey(firstKey);
            var firstValue = obj[firstKey];

            if (Normalize.IsJsonPrimitive(firstValue))
            {
                writer.PushListItem(depth, $"{encodedKey}{Constants.COLON} {Primitives.EncodePrimitive(firstValue, options.Delimiter)}");
            }
            else if (Normalize.IsJsonArray(firstValue))
            {
                var arr = (JsonArray)firstValue!;

                if (Normalize.IsArrayOfPrimitives(arr))
                {
                    var formatted = EncodeInlineArrayLine(arr, options.Delimiter, firstKey);
                    writer.PushListItem(depth, formatted);
                }
                else if (Normalize.IsArrayOfObjects(arr))
                {
                    var objects = arr.Cast<JsonObject>().ToList();
                    var header = ExtractTabularHeader(objects);

                    if (header != null)
                    {
                        var formattedHeader = Primitives.FormatHeader(arr.Count, firstKey, header, options.Delimiter);
                        writer.PushListItem(depth, formattedHeader);
                        // SPEC v3.0 §10: Tabular rows MUST appear at depth +2 relative to the hyphen line
                        WriteTabularRows(objects, header, writer, depth + 2, options);
                    }
                    else
                    {
                        writer.PushListItem(depth, $"{encodedKey}{Constants.OPEN_BRACKET}{arr.Count}{Constants.CLOSE_BRACKET}{Constants.COLON}");
                        foreach (var itemObj in arr.OfType<JsonObject>())
                        {
                            EncodeObjectAsListItem(itemObj, writer, depth + 2, options);
                        }
                    }
                }
                else
                {
                    writer.PushListItem(depth, $"{encodedKey}{Constants.OPEN_BRACKET}{arr.Count}{Constants.CLOSE_BRACKET}{Constants.COLON}");

                    // Encode array contents at depth + 2 (SPEC v3.0 §10)
                    foreach (var item in arr)
                    {
                        EncodeListItemValue(item, writer, depth + 2, options);
                    }
                }
            }
            else if (Normalize.IsJsonObject(firstValue))
            {
                var nestedObj = (JsonObject)firstValue!;

                if (nestedObj.Count == 0)
                {
                    writer.PushListItem(depth, $"{encodedKey}{Constants.COLON}");
                }
                else
                {
                    writer.PushListItem(depth, $"{encodedKey}{Constants.COLON}");
                    EncodeObject(nestedObj, writer, depth + 2, options);
                }
            }

            for (int i = 1; i < keys.Count; i++)
            {
                var key = keys[i];
                EncodeKeyValuePair(key, obj[key], writer, depth + 1, options);
            }
        }

        #endregion

        #region List item encoding helpers

        /// <summary>
        /// Encodes a value as a list item.
        /// </summary>
        private static void EncodeListItemValue(
            JsonNode? value,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            if (Normalize.IsJsonPrimitive(value))
            {
                writer.PushListItem(depth, Primitives.EncodePrimitive(value, options.Delimiter));
            }
            else if (Normalize.IsJsonArray(value) && Normalize.IsArrayOfPrimitives((JsonArray)value!))
            {
                var arr = (JsonArray)value!;
                var inline = EncodeInlineArrayLine(arr, options.Delimiter, null);
                writer.PushListItem(depth, inline);
            }
            else if (Normalize.IsJsonObject(value))
            {
                EncodeObjectAsListItem((JsonObject)value!, writer, depth, options);
            }
            else if (Normalize.IsJsonArray(value))
            {
                var arr = (JsonArray)value!;

                if (Normalize.IsArrayOfObjects(arr))
                {
                    var objects = arr.Cast<JsonObject>().ToList();
                    var header = ExtractTabularHeader(objects);
                    if (header != null)
                    {
                        var formattedHeader = Primitives.FormatHeader(arr.Count, null, header, options.Delimiter);
                        writer.PushListItem(depth, formattedHeader);
                        WriteTabularRows(objects, header, writer, depth + 2, options);
                        return;
                    }
                }

                var headerStr = Primitives.FormatHeader(arr.Count, null, null, options.Delimiter);
                writer.PushListItem(depth, headerStr);

                foreach (var item in arr)
                {
                    EncodeListItemValue(item, writer, depth + 1, options);
                }
            }
        }

        #endregion
    }
}
