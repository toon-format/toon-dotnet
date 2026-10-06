#nullable enable
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Toon.Format.Internal.Shared
{
    internal static class LiteralUtils
    {
        private static readonly Regex NumericLiteralRegex = new(
            pattern: "^-?(?:0|[1-9][0-9]*)(?:\\.[0-9]+)?(?:e[+-]?[0-9]+)?$",
            options: RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Checks if the token is a boolean or null literal: true, false, null.
        /// </summary>
        internal static bool IsBooleanOrNullLiteral(string token)
        {
            return string.Equals(token, Constants.TRUE_LITERAL, StringComparison.Ordinal)
                || string.Equals(token, Constants.FALSE_LITERAL, StringComparison.Ordinal)
                || string.Equals(token, Constants.NULL_LITERAL, StringComparison.Ordinal);
        }

        /// <summary>
        /// Parses a token of the number grammar: an integer in <see cref="long"/> range becomes a <c>long</c>,
        /// any other finite number a <c>double</c>. Returns null for every other token, which then decodes as a string.
        /// </summary>
        internal static JsonValue? ParseNumber(string token)
        {
            if (!NumericLiteralRegex.IsMatch(token))
                return null;

            if (long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
                return JsonValue.Create(integer);

            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !NumericUtils.IsFinite(number))
                return null;

            return JsonValue.Create(number == 0 ? 0.0 : number);
        }
    }
}
