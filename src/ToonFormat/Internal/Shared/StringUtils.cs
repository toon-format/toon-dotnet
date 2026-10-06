#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Toon.Format.Internal.Shared
{
    internal static class StringUtils
    {
        /// <summary>
        /// Trims U+0020 spaces only: other whitespace, such as NBSP or a tab outside its delimiter role,
        /// is part of the token.
        /// </summary>
        internal static string TrimSpaces(string value) => value.Trim(Constants.SPACE);

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
                    case Constants.BACKSLASH: sb.Append(@"\\"); break;
                    case Constants.DOUBLE_QUOTE: sb.Append(@"\"""); break;
                    case Constants.NEWLINE: sb.Append(@"\n"); break;
                    case Constants.CARRIAGE_RETURN: sb.Append(@"\r"); break;
                    case Constants.TAB: sb.Append(@"\t"); break;
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
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;

            var sb = new StringBuilder(value.Length);
            int i = 0;
            while (i < value.Length)
            {
                var ch = value[i];
                if (ch == Constants.BACKSLASH)
                {
                    if (i + 1 >= value.Length)
                        throw ToonFormatException.Syntax("Invalid escape sequence: backslash at end of string");

                    var next = value[i + 1];
                    switch (next)
                    {
                        case 'n':
                            sb.Append(Constants.NEWLINE);
                            i += 2;
                            continue;
                        case 't':
                            sb.Append(Constants.TAB);
                            i += 2;
                            continue;
                        case 'r':
                            sb.Append(Constants.CARRIAGE_RETURN);
                            i += 2;
                            continue;
                        case '\\':
                            sb.Append(Constants.BACKSLASH);
                            i += 2;
                            continue;
                        case '"':
                            sb.Append(Constants.DOUBLE_QUOTE);
                            i += 2;
                            continue;
                        case 'u':
                            sb.Append(ParseUnicodeEscape(value, i));
                            i += 6;
                            continue;
                        default:
                            throw ToonFormatException.Syntax($"Invalid escape sequence: \\{next}");
                    }
                }

                sb.Append(ch);
                i++;
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
                if (content[i] == Constants.BACKSLASH)
                    i++;
                else if (content[i] == Constants.DOUBLE_QUOTE)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Finds the position of the target character not inside quotes; returns -1 if not found.
        /// Escape sequences inside quotes are skipped.
        /// </summary>
        internal static int FindUnquotedChar(string content, char target, int start = 0)
        {
            bool inQuotes = false;
            int i = start;

            while (i < content.Length)
            {
                if (inQuotes && content[i] == Constants.BACKSLASH && i + 1 < content.Length)
                {
                    // Skip the next character for escape sequences inside quotes
                    i += 2;
                    continue;
                }

                if (content[i] == Constants.DOUBLE_QUOTE)
                {
                    inQuotes = !inQuotes;
                    i++;
                    continue;
                }

                if (!inQuotes && content[i] == target)
                    return i;

                i++;
            }

            return -1;
        }
    }
}