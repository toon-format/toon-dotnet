using System.Text.RegularExpressions;

namespace Toon.Format.Internal.Shared;

internal static class ValidationShared
{
    private static readonly Regex ValidUnquotedKeyRegex = new(
        pattern: "^[A-Za-z_][A-Za-z0-9_.]*$",
        options: RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex NumericLikeRegex = new(
        pattern: "^[+-]?[0-9]+(?:\\.[0-9]+)?(?:e[+-]?[0-9]+)?$",
        options: RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly char[] StructuralBracketsAndBraces =
    {
        Constants.OpenBracket,
        Constants.CloseBracket,
        Constants.OpenBrace,
        Constants.CloseBrace
    };

    /// <summary>Whether the key may stay unquoted.</summary>
    internal static bool IsValidUnquotedKey(string key) => ValidUnquotedKeyRegex.IsMatch(key);

    /// <summary>Whether the string value may stay unquoted under the active delimiter.</summary>
    internal static bool IsSafeUnquoted(string value, char delimiter)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Only space and tab force quoting; other Unicode whitespace survives decoding as content.
        if (IsSpaceOrTab(value[0]) || IsSpaceOrTab(value[value.Length - 1]))
            return false;

        if (LiteralUtils.IsBooleanOrNullLiteral(value) || IsNumericLike(value))
            return false;

        if (value.IndexOf(Constants.Colon) >= 0)
            return false;

        if (value.IndexOf(Constants.DoubleQuote) >= 0 || value.IndexOf(Constants.Backslash) >= 0)
            return false;

        if (value.IndexOfAny(StructuralBracketsAndBraces) >= 0)
            return false;

        if (value.Any(ch => ch < ' '))
            return false;

        if (value.IndexOf(delimiter) >= 0)
            return false;

        if (value[0] == Constants.ListItemMarker || value[0] == Constants.CommentMarker)
            return false;

        return true;
    }

    private static bool IsNumericLike(string value) => NumericLikeRegex.IsMatch(value);

    private static bool IsSpaceOrTab(char c) => c == Constants.Space || c == Constants.Tab;
}