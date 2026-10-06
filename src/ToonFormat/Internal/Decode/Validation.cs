using System.Collections.Generic;
using System.Linq;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Decode
{
    /// <summary>
    /// Count, blank-line, and row checks shared by the array and keyed-object rules.
    /// </summary>
    internal static class Validation
    {
        public static void AssertExpectedCount(int actual, int expected, string itemType, bool strict, ParsedLine line)
        {
            if (strict && actual != expected)
                throw ToonFormatException.Range($"Expected {expected} {itemType}, but got {actual}", line.LineNumber, sourceLine: line.Raw);
        }

        public static void ValidateNoExtraListItems(ParsedLine? nextLine, int itemDepth, int expectedCount)
        {
            if (nextLine != null && nextLine.Depth == itemDepth && nextLine.Content.StartsWith(Constants.LIST_ITEM_PREFIX, StringComparison.Ordinal))
                throw ToonFormatException.Range($"Expected {expectedCount} list-form items, but found more", nextLine.LineNumber, sourceLine: nextLine.Raw);
        }

        public static void ValidateNoExtraTabularRows(ParsedLine? nextLine, int rowDepth, ArrayHeaderInfo header)
        {
            if (nextLine != null
                && nextLine.Depth == rowDepth
                && !nextLine.Content.StartsWith(Constants.LIST_ITEM_PREFIX, StringComparison.Ordinal)
                && IsDataRow(nextLine.Content, header.Delimiter))
            {
                throw ToonFormatException.Range($"Expected {header.Length} tabular rows, but found more", nextLine.LineNumber, sourceLine: nextLine.Raw);
            }
        }

        /// <summary>
        /// Rejects a blank line strictly between <paramref name="startLine"/> and <paramref name="endLine"/>, at any indentation.
        /// </summary>
        public static void ValidateNoBlankLinesInRange(int startLine, int endLine, List<int> blankLines, string context)
        {
            var firstBlank = blankLines.FirstOrDefault(blank => blank > startLine && blank < endLine);
            if (firstBlank != 0)
                throw ToonFormatException.Syntax($"Blank lines inside {context} are not allowed in strict mode", firstBlank);
        }

        /// <summary>
        /// Tells a tabular row from a key-value line: a row has no unquoted colon, or a delimiter before it.
        /// </summary>
        public static bool IsDataRow(string content, char delimiter)
        {
            var colonIndex = StringUtils.FindUnquotedChar(content, Constants.COLON);
            var delimiterIndex = StringUtils.FindUnquotedChar(content, delimiter);
            return colonIndex == -1 || (delimiterIndex != -1 && delimiterIndex < colonIndex);
        }
    }
}
