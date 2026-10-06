#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Decode
{
    internal sealed class ArrayHeaderInfo
    {
        public string? Key { get; set; }
        public int Length { get; set; }
        public char Delimiter { get; set; }
        public List<FieldNode>? Fields { get; set; }

        /// <summary>A keyed tabular header <c>[N:]</c> declares N entries of an object, not N array items.</summary>
        public bool Keyed { get; set; }
        public string? InlineValues { get; set; }

        /// <summary>A violation that strict mode rejects and non-strict mode resolves, such as a repeated field name.</summary>
        public string? StrictError { get; set; }
    }

    /// <summary>
    /// Parsing utilities for TOON format tokens, headers, and values.
    /// </summary>
    internal static class Parser
    {
        private static readonly Regex BracketLengthRegex = new("^(?:0|[1-9][0-9]*)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        #region Array header parsing

        /// <summary>
        /// Parses a header line such as <c>key[3]:</c> or <c>users[2|]{name|age}:</c>, free of strict-mode policy:
        /// returns null with a null <paramref name="error"/> for a line that is no header, and null with the
        /// <paramref name="error"/> for a header-shaped line that breaks the grammar.
        /// </summary>
        public static ArrayHeaderInfo? ParseArrayHeaderLine(string content, out string? error)
        {
            error = null;
            int bracketStart;

            var trimmed = content.TrimStart();
            if (trimmed.StartsWith("\"", StringComparison.Ordinal))
            {
                var closingQuoteIndex = StringUtils.FindClosingQuote(trimmed);
                if (closingQuoteIndex == -1 || closingQuoteIndex + 1 >= trimmed.Length || trimmed[closingQuoteIndex + 1] != Constants.OPEN_BRACKET)
                    return null;

                bracketStart = content.Length - trimmed.Length + closingQuoteIndex + 1;
            }
            else
            {
                bracketStart = StringUtils.FindUnquotedChar(content, Constants.OPEN_BRACKET);
            }

            if (bracketStart == -1)
                return null;

            // A header needs a colon, and its key can't contain one. Past this check, a grammar failure
            // makes the line an invalid header instead of a key-value line.
            var firstColonIndex = StringUtils.FindUnquotedChar(content, Constants.COLON);
            if (firstColonIndex == -1 || firstColonIndex < bracketStart)
                return null;

            var bracketEnd = StringUtils.FindUnquotedChar(content, Constants.CLOSE_BRACKET, bracketStart);
            if (bracketEnd == -1)
                return Invalid("Unterminated bracket segment", out error);

            var fieldsEnd = bracketEnd + 1;
            var braceStart = StringUtils.FindUnquotedChar(content, Constants.OPEN_BRACE, bracketEnd);
            if (braceStart != -1 && braceStart < StringUtils.FindUnquotedChar(content, Constants.COLON, bracketEnd))
            {
                if (braceStart != bracketEnd + 1)
                    return Invalid(GapError(content.Substring(bracketEnd + 1, braceStart - bracketEnd - 1), "field list"), out error);

                var braceEnd = FindMatchingBrace(content, braceStart);
                if (braceEnd != -1)
                    fieldsEnd = braceEnd + 1;
            }

            var colonIndex = StringUtils.FindUnquotedChar(content, Constants.COLON, fieldsEnd);
            if (colonIndex == -1)
                return Invalid("Missing colon after array header", out error);
            if (colonIndex != fieldsEnd)
                return Invalid(GapError(content.Substring(fieldsEnd, colonIndex - fieldsEnd), "colon"), out error);

            string? key = null;
            if (bracketStart > 0)
            {
                var rawKey = content.Substring(0, bracketStart);
                // Trimming here would silently turn `foo [2]:` into a header with key `foo`.
                if (char.IsWhiteSpace(rawKey[rawKey.Length - 1]))
                    return Invalid("Unexpected whitespace between key and bracket segment", out error);

                key = rawKey[0] == Constants.DOUBLE_QUOTE ? ParseStringLiteral(rawKey) : rawKey;
            }

            if (!TryParseBracketSegment(content.Substring(bracketStart + 1, bracketEnd - bracketStart - 1), out var length, out var delimiter, out var keyed, out error))
                return null;

            List<FieldNode>? fields = null;
            if (fieldsEnd > bracketEnd + 1)
            {
                var fieldsContent = content.Substring(braceStart + 1, fieldsEnd - braceStart - 2);
                var mismatchedDelimiter = FindUnquotedMismatchedDelimiter(fieldsContent, delimiter);
                if (mismatchedDelimiter != null)
                    return Invalid($"Header delimiter mismatch: bracket declares \"{FormatDelimiter(delimiter)}\" but field list contains unquoted \"{FormatDelimiter(mismatchedDelimiter.Value)}\"", out error);

                try
                {
                    fields = ParseFieldEntries(fieldsContent, delimiter);
                }
                catch (ToonFormatException ex)
                {
                    return Invalid(ex.Detail, out error);
                }
            }

            // Non-strict mode resolves a repeated field name by last write wins.
            var duplicateField = fields == null ? null : FindDuplicateFieldName(fields);
            var duplicateError = duplicateField == null ? null : $"Duplicate field name \"{duplicateField}\" in field list";

            if (keyed && fields == null)
                return Invalid("Keyed header requires a field list", out error);

            var afterColon = StringUtils.TrimSpaces(content.Substring(colonIndex + 1));

            // Decoding the values as an inline array would silently drop the fields.
            if (fields != null && afterColon.Length > 0)
                return Invalid(duplicateError ?? "Unexpected content after fields-bearing header colon", out error);

            return new ArrayHeaderInfo
            {
                Key = key,
                Length = length,
                Delimiter = delimiter,
                Fields = fields,
                Keyed = keyed,
                StrictError = duplicateError,
                InlineValues = afterColon.Length == 0 ? null : afterColon,
            };
        }

        private static ArrayHeaderInfo? Invalid(string reason, out string? error)
        {
            error = reason;
            return null;
        }

        private static string GapError(string gap, string next) =>
            gap.Trim().Length == 0
                ? $"Unexpected whitespace between bracket segment and {next}"
                : $"Unexpected content \"{gap.Trim()}\" between bracket segment and {next}";

        private static bool TryParseBracketSegment(string segment, out int length, out char delimiter, out bool keyed, out string? error)
        {
            var content = segment;
            delimiter = Constants.DEFAULT_DELIMITER_CHAR;
            if (content.Length > 0 && (content[content.Length - 1] == Constants.TAB || content[content.Length - 1] == Constants.PIPE))
            {
                delimiter = content[content.Length - 1];
                content = content.Substring(0, content.Length - 1);
            }

            // Only a colon between the length and the optional delimiter symbol marks a keyed header;
            // any other placement leaves a token that fails the length check below.
            keyed = content.Length > 0 && content[content.Length - 1] == Constants.COLON;
            if (keyed)
                content = content.Substring(0, content.Length - 1);

            if (!BracketLengthRegex.IsMatch(content))
            {
                length = 0;
                error = $"Invalid array length: \"{segment}\" (expected non-negative integer with no leading zeros)";
                return false;
            }

            // A length beyond int range can never match a count, so it saturates instead of wrapping.
            length = int.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : int.MaxValue;
            error = null;
            return true;
        }

        /// <summary>
        /// Parses a field list, descending into nested field groups such as <c>customer{name,country}</c>.
        /// </summary>
        private static List<FieldNode> ParseFieldEntries(string fieldsContent, char delimiter)
        {
            var fields = new List<FieldNode>();
            foreach (var entry in SplitFieldEntries(fieldsContent, delimiter))
            {
                var trimmed = StringUtils.TrimSpaces(entry);
                if (trimmed.Length == 0)
                    throw ToonFormatException.Syntax("Empty field name in field list");

                var groupStart = StringUtils.FindUnquotedChar(trimmed, Constants.OPEN_BRACE);
                if (groupStart == -1)
                {
                    fields.Add(new FieldNode(ParseStringLiteral(trimmed)));
                    continue;
                }

                var name = trimmed.Substring(0, groupStart);
                if (name.Length == 0)
                    throw ToonFormatException.Syntax("Missing field name before nested field group");
                if (char.IsWhiteSpace(name[name.Length - 1]))
                    throw ToonFormatException.Syntax("Unexpected whitespace before nested field group");

                var groupEnd = FindMatchingBrace(trimmed, groupStart);
                if (groupEnd == -1)
                    throw ToonFormatException.Syntax("Unmatched brace in field list");
                if (groupEnd != trimmed.Length - 1)
                    throw ToonFormatException.Syntax("Unexpected content after nested field group");

                fields.Add(new FieldNode(ParseStringLiteral(name), ParseFieldEntries(trimmed.Substring(groupStart + 1, groupEnd - groupStart - 1), delimiter)));
            }

            return fields;
        }

        /// <summary>
        /// Splits a field list at the active delimiter outside quotes and nested field groups.
        /// </summary>
        private static List<string> SplitFieldEntries(string content, char delimiter)
        {
            var entries = new List<string>();
            var entryStart = 0;
            var inQuotes = false;
            var braceDepth = 0;

            for (var i = 0; i < content.Length; i++)
            {
                var ch = content[i];
                if (inQuotes && ch == Constants.BACKSLASH)
                    i++;
                else if (ch == Constants.DOUBLE_QUOTE)
                    inQuotes = !inQuotes;
                else if (!inQuotes && ch == Constants.OPEN_BRACE)
                    braceDepth++;
                else if (!inQuotes && ch == Constants.CLOSE_BRACE)
                    braceDepth--;
                else if (!inQuotes && ch == delimiter && braceDepth == 0)
                {
                    entries.Add(content.Substring(entryStart, i - entryStart));
                    entryStart = i + 1;
                }
            }

            entries.Add(content.Substring(entryStart));
            return entries;
        }

        /// <summary>
        /// Finds the brace that closes the one at <paramref name="braceStart"/>, ignoring braces inside quoted names.
        /// </summary>
        private static int FindMatchingBrace(string content, int braceStart)
        {
            var inQuotes = false;
            var braceDepth = 0;

            for (var i = braceStart; i < content.Length; i++)
            {
                var ch = content[i];
                if (inQuotes && ch == Constants.BACKSLASH)
                    i++;
                else if (ch == Constants.DOUBLE_QUOTE)
                    inQuotes = !inQuotes;
                else if (!inQuotes && ch == Constants.OPEN_BRACE)
                    braceDepth++;
                else if (!inQuotes && ch == Constants.CLOSE_BRACE && --braceDepth == 0)
                    return i;
            }

            return -1;
        }

        private static string? FindDuplicateFieldName(List<FieldNode> fields)
        {
            var seen = new HashSet<string>();
            foreach (var field in fields)
            {
                if (!seen.Add(field.Name))
                    return field.Name;

                var nested = field.Children == null ? null : FindDuplicateFieldName(field.Children);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        /// <summary>
        /// Counts the leaf fields of a field list: the number of cells each row carries.
        /// </summary>
        public static int CountLeafFields(List<FieldNode> fields) =>
            fields.Sum(field => field.Children == null ? 1 : CountLeafFields(field.Children));

        private static char? FindUnquotedMismatchedDelimiter(string content, char activeDelimiter)
        {
            foreach (var candidate in new[] { Constants.COMMA, Constants.TAB, Constants.PIPE })
            {
                if (candidate != activeDelimiter && StringUtils.FindUnquotedChar(content, candidate) != -1)
                    return candidate;
            }

            return null;
        }

        private static string FormatDelimiter(char delimiter) => delimiter == Constants.TAB ? "\\t" : delimiter.ToString();

        #endregion

        #region Delimited value parsing

        /// <summary>
        /// Splits delimiter-separated values outside quotes and trims each at U+0020.
        /// </summary>
        public static List<string> ParseDelimitedValues(string input, char delimiter)
        {
            var values = new List<string>();
            var valueStart = 0;
            var inQuotes = false;

            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                if (inQuotes && ch == Constants.BACKSLASH)
                    i++;
                else if (ch == Constants.DOUBLE_QUOTE)
                    inQuotes = !inQuotes;
                else if (!inQuotes && ch == delimiter)
                {
                    values.Add(StringUtils.TrimSpaces(input.Substring(valueStart, i - valueStart)));
                    valueStart = i + 1;
                }
            }

            if (input.Length > 0 || values.Count > 0)
                values.Add(StringUtils.TrimSpaces(input.Substring(valueStart)));

            return values;
        }

        #endregion

        #region Primitive and key parsing

        /// <summary>
        /// Parses a primitive token: a quoted string, true, false, null, a number, or else an unquoted string.
        /// </summary>
        public static JsonNode? ParsePrimitiveToken(string token)
        {
            var trimmed = StringUtils.TrimSpaces(token);

            if (trimmed.Length > 0 && trimmed[0] == Constants.DOUBLE_QUOTE)
                return JsonValue.Create(ParseStringLiteral(trimmed));

            return trimmed switch
            {
                Constants.TRUE_LITERAL => JsonValue.Create(true),
                Constants.FALSE_LITERAL => JsonValue.Create(false),
                Constants.NULL_LITERAL => null,
                _ => LiteralUtils.ParseNumber(trimmed) ?? JsonValue.Create(trimmed),
            };
        }

        /// <summary>
        /// Unquotes and unescapes a quoted token; an unquoted token comes back trimmed.
        /// </summary>
        public static string ParseStringLiteral(string token)
        {
            var trimmed = StringUtils.TrimSpaces(token);
            if (trimmed.Length == 0 || trimmed[0] != Constants.DOUBLE_QUOTE)
                return trimmed;

            var closingQuoteIndex = StringUtils.FindClosingQuote(trimmed);
            if (closingQuoteIndex == -1)
                throw ToonFormatException.Syntax("Unterminated string: missing closing quote");
            if (closingQuoteIndex != trimmed.Length - 1)
                throw ToonFormatException.Syntax("Unexpected characters after closing quote");

            return StringUtils.UnescapeString(trimmed.Substring(1, closingQuoteIndex - 1));
        }

        /// <summary>
        /// Parses the key that starts <paramref name="content"/> and returns it with the index after its colon.
        /// </summary>
        public static (string Key, int End) ParseKeyToken(string content) =>
            content[0] == Constants.DOUBLE_QUOTE ? ParseQuotedKey(content) : ParseUnquotedKey(content);

        private static (string Key, int End) ParseUnquotedKey(string content)
        {
            // A raw scan would cut `a "b:c" d: 1` at the quoted colon and split the key in two.
            var colonIndex = StringUtils.FindUnquotedChar(content, Constants.COLON);
            if (colonIndex == -1)
                throw ToonFormatException.Syntax("Missing colon after key");

            return (StringUtils.TrimSpaces(content.Substring(0, colonIndex)), colonIndex + 1);
        }

        private static (string Key, int End) ParseQuotedKey(string content)
        {
            var closingQuoteIndex = StringUtils.FindClosingQuote(content);
            if (closingQuoteIndex == -1)
                throw ToonFormatException.Syntax("Unterminated quoted key");

            var key = StringUtils.UnescapeString(content.Substring(1, closingQuoteIndex - 1));
            var end = closingQuoteIndex + 1;
            while (end < content.Length && content[end] == Constants.SPACE)
                end++;

            if (end >= content.Length || content[end] != Constants.COLON)
                throw ToonFormatException.Syntax("Missing colon after key");

            return (key, end + 1);
        }

        #endregion
    }
}
