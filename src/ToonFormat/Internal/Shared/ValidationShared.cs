#nullable enable
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Toon.Format;

namespace Toon.Format.Internal.Shared
{
    internal static class ValidationShared
    {
        private static readonly Regex ValidUnquotedKeyRegex = new(
            pattern: "^[A-Z_][\\w.]*$",
            options: RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex NumericLikeRegex = new(
            pattern: "^[+-]?[0-9]+(?:\\.[0-9]+)?(?:e[+-]?[0-9]+)?$",
            options: RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly char[] StructuralBracketsAndBraces =
        {
            Constants.OPEN_BRACKET,
            Constants.CLOSE_BRACKET,
            Constants.OPEN_BRACE,
            Constants.CLOSE_BRACE
        };

        /// <summary>Whether the key name can be without quotes.</summary>
        internal static bool IsValidUnquotedKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return ValidUnquotedKeyRegex.IsMatch(key);
        }

        /// <summary>Whether the string value can be safely without quotes.</summary>
        internal static bool IsSafeUnquoted(string value, ToonDelimiter delimiter = Constants.DEFAULT_DELIMITER_ENUM)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Only space and tab force quoting; other Unicode whitespace survives decoding as content.
            if (IsSpaceOrTab(value[0]) || IsSpaceOrTab(value[value.Length - 1]))
                return false;

            if (LiteralUtils.IsBooleanOrNullLiteral(value) || IsNumericLike(value))
                return false;

            if (value.IndexOf(Constants.COLON) >= 0)
                return false;

            if (value.IndexOf(Constants.DOUBLE_QUOTE) >= 0 || value.IndexOf(Constants.BACKSLASH) >= 0)
                return false;

            if (value.IndexOfAny(StructuralBracketsAndBraces) >= 0)
                return false;

            if (value.Any(ch => ch < ' '))
                return false;

            var delimiterChar = Constants.ToDelimiterChar(delimiter);
            if (value.IndexOf(delimiterChar) >= 0)
                return false;

            if (value[0] == Constants.LIST_ITEM_MARKER || value[0] == Constants.COMMENT_MARKER)
                return false;

            return true;
        }

        private static bool IsNumericLike(string value) => NumericLikeRegex.IsMatch(value);

        private static bool IsSpaceOrTab(char c) => c == Constants.SPACE || c == Constants.TAB;
    }
}