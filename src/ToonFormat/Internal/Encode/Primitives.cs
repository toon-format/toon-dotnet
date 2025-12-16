#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using ToonFormat.Internal.Shared;

namespace ToonFormat.Internal.Encode
{
    /// <summary>
    /// Primitive value encoding, key encoding, and header formatting utilities.
    /// Aligned with TypeScript encode/primitives.ts
    /// </summary>
    internal static class Primitives
    {
        // #region Primitive encoding

        /// <summary>
        /// Encodes a primitive JSON value (null, boolean, number, or string) to its TOON representation.
        /// </summary>
        public static string EncodePrimitive(JsonNode? value, char delimiter = Constants.COMMA)
        {
            if (value == null)
                return Constants.NULL_LITERAL;

            if (value is JsonValue jsonValue)
            {
                // Boolean
                if (jsonValue.TryGetValue<bool>(out var boolVal))
                    return boolVal ? Constants.TRUE_LITERAL : Constants.FALSE_LITERAL;

                // Number
                if (jsonValue.TryGetValue<int>(out var intVal))
                    return intVal.ToString();

                if (jsonValue.TryGetValue<long>(out var longVal))
                    return longVal.ToString();

                if (jsonValue.TryGetValue<double>(out var doubleVal))
                    return doubleVal.ToString("G17"); // Full precision

                if (jsonValue.TryGetValue<decimal>(out var decimalVal))
                    return decimalVal.ToString();

                // String
                if (jsonValue.TryGetValue<string>(out var strVal))
                    return EncodeStringLiteral(strVal ?? string.Empty, delimiter);
            }

            return Constants.NULL_LITERAL;
        }

        /// <summary>
        /// Encodes a string literal, adding quotes if necessary.
        /// </summary>
        public static string EncodeStringLiteral(string value, char delimiter = Constants.COMMA)
        {
            var delimiterEnum = Constants.FromDelimiterChar(delimiter);
            
            if (ValidationShared.IsSafeUnquoted(value, delimiterEnum))
            {
                return value;
            }

            var escaped = StringUtils.EscapeString(value);
            return $"{Constants.DOUBLE_QUOTE}{escaped}{Constants.DOUBLE_QUOTE}";
        }

        // #endregion

        // #region Key encoding

        /// <summary>
        /// Encodes a key, adding quotes if necessary.
        /// </summary>
        public static string EncodeKey(string key)
        {
            if (ValidationShared.IsValidUnquotedKey(key))
            {
                return key;
            }

            var escaped = StringUtils.EscapeString(key);
            return $"{Constants.DOUBLE_QUOTE}{escaped}{Constants.DOUBLE_QUOTE}";
        }

        // #endregion

        // #region Value joining

        /// <summary>
        /// Encodes and joins an array of primitive values with the specified delimiter.
        /// </summary>
        public static string EncodeAndJoinPrimitives(IEnumerable<JsonNode?> values, char delimiter = Constants.COMMA)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var value in values)
            {
                if (!first)
                    sb.Append(delimiter);
                first = false;

                sb.Append(EncodePrimitive(value, delimiter));
            }
            return sb.ToString();
        }

        // #endregion

        // #region Header formatters

        /// <summary>
        /// Formats an array header with optional key, length marker, delimiter, and field names.
        /// Examples:
        /// - "[3]:" for unnamed array of 3 items
        /// - "items[5]:" for named array
        /// - "users[#2]{name,age}:" for tabular format with length marker
        /// </summary>
        public static string FormatHeader(
            int length,
            string? key = null,
            IReadOnlyList<string>? fields = null,
            char? delimiter = null,
            bool lengthMarker = false)
        {
            var delimiterChar = delimiter ?? Constants.DEFAULT_DELIMITER_CHAR;
            var sb = new StringBuilder();

            // Add key if present
            if (!string.IsNullOrEmpty(key))
                sb.Append(EncodeKey(key));

            // Add array length with optional marker and delimiter
            sb.Append(Constants.OPEN_BRACKET);
            if (lengthMarker)
                sb.Append(Constants.HASH);
            sb.Append(length);
            if (delimiterChar != Constants.DEFAULT_DELIMITER_CHAR)
                sb.Append(delimiterChar);
            sb.Append(Constants.CLOSE_BRACKET);

            // Add field names for tabular format
            if (fields is { Count: > 0 })
            {
                sb.Append(Constants.OPEN_BRACE);
                for (int i = 0; i < fields.Count; i++)
                {
                    if (i > 0)
                        sb.Append(delimiterChar);
                    sb.Append(EncodeKey(fields[i]));
                }
                sb.Append(Constants.CLOSE_BRACE);
            }

            sb.Append(Constants.COLON);
            return sb.ToString();
        }

        // #endregion
    }
}
