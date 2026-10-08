using System.Globalization;
using System.Text;

namespace Toon.Format.Internal.Shared;

internal static class StringUtils
{
    /// <summary>
    /// Trims U+0020 spaces only: other whitespace, such as NBSP or a tab outside its delimiter role,
    /// is part of the token.
    /// </summary>
    internal static string TrimSpaces(string value) => value.Trim(Constants.Space);

    /// <summary>
    /// Escapes backslash, quote, newline, carriage return, and tab, and every other control character as <c>\uXXXX</c>.
    /// </summary>
    internal static string EscapeString(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case Constants.Backslash: sb.Append(@"\\"); break;
                case Constants.DoubleQuote: sb.Append(@"\"""); break;
                case Constants.Newline: sb.Append(@"\n"); break;
                case Constants.CarriageReturn: sb.Append(@"\r"); break;
                case Constants.Tab: sb.Append(@"\t"); break;
                case < ' ': sb.Append(@"\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture)); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Unescapes \n, \t, \r, \\, \", and \uXXXX. Invalid sequences throw <see cref="ToonFormatException"/>.
    /// </summary>
    internal static string UnescapeString(string value)
    {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != Constants.Backslash)
            {
                sb.Append(value[i]);
                continue;
            }

            if (i + 1 >= value.Length)
                throw ToonFormatException.Syntax("Invalid escape sequence: backslash at end of string");

            var next = value[++i];
            sb.Append(next switch
            {
                'n' => Constants.Newline,
                't' => Constants.Tab,
                'r' => Constants.CarriageReturn,
                '\\' => Constants.Backslash,
                '"' => Constants.DoubleQuote,
                'u' => ParseUnicodeEscape(value, i - 1),
                _ => throw ToonFormatException.Syntax($"Invalid escape sequence: \\{next}"),
            });

            if (next == 'u')
                i += 4;
        }

        return sb.ToString();
    }

    // Supplementary code points must appear as literal UTF-8, so every surrogate escape is rejected, lone or paired.
    private static char ParseUnicodeEscape(string value, int backslashIndex)
    {
        var hex = value.Substring(backslashIndex + 2, Math.Min(4, value.Length - backslashIndex - 2));
        if (hex.Length != 4 || !hex.All(Uri.IsHexDigit))
            throw ToonFormatException.Syntax($"Invalid escape sequence: \\u must be followed by 4 hex digits, got \"{hex}\"");

        var codeUnit = (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        if (char.IsSurrogate(codeUnit))
            throw ToonFormatException.Syntax($"Invalid escape sequence: \\u{hex} is a surrogate; supplementary code points must appear as literal UTF-8");

        return codeUnit;
    }

    /// <summary>
    /// Returns the index of the quote that closes the one at index 0, skipping escaped characters, or -1.
    /// </summary>
    internal static int FindClosingQuote(string content)
    {
        for (var i = 1; i < content.Length; i++)
        {
            if (content[i] == Constants.Backslash)
                i++;
            else if (content[i] == Constants.DoubleQuote)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Returns the index of the first <paramref name="target"/> outside quotes from <paramref name="start"/>, or -1.
    /// </summary>
    internal static int FindUnquotedChar(string content, char target, int start = 0)
    {
        var inQuotes = false;
        for (var i = start; i < content.Length; i++)
        {
            var ch = content[i];
            if (inQuotes && ch == Constants.Backslash)
                i++;
            else if (ch == Constants.DoubleQuote)
                inQuotes = !inQuotes;
            else if (!inQuotes && ch == target)
                return i;
        }

        return -1;
    }
}