#nullable enable
using System.Globalization;

namespace Toon.Format.Internal.Shared
{
    internal static class LiteralUtils
    {
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
        /// Checks if the token is a valid numeric literal.
        /// Rules:
        /// - Rejects leading zeros (except "0" itself or decimals like "0.xxx")
        /// - Parses successfully and is a finite number (not NaN/Infinity)
        /// </summary>
        internal static bool IsNumericLiteral(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            // Must not have leading zeros (except "0" itself or decimals like "0.5")
            if (token.Length > 1 && token[0] == '0' && token[1] != '.')
                return false;

            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                return false;

            return !double.IsNaN(num) && !double.IsInfinity(num);
        }
    }
}