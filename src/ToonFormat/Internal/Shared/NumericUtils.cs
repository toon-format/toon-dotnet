using System.Text.RegularExpressions;

namespace Toon.Format.Internal.Shared
{
    internal static class NumericUtils
    {
        /// <summary>
        /// Converts a double to a decimal in canonical form for accurate representation.
        /// </summary>
        /// <param name="value">The input double value</param>
        /// <returns>A decimal representation of the input value.</returns>
        /// <remarks>https://github.com/toon-format/spec/blob/main/SPEC.md#2-data-model</remarks>
        /// <example>1e-7 => 0.0000001</example>
        public static decimal EmitCanonicalDecimalForm(double value)
        {
            var scientificString = value.ToString("G17");
            var match = Regex.Match(scientificString, @"e[-+]\d+", RegexOptions.IgnoreCase);

            if (!match.Success) return (decimal)value;

            // The match is the exponent part, e.g., "E+04"
            var exponentPart = match.Value;

            // Remove the 'E' or 'e' and the sign to get just the digits
            var exponentDigits = exponentPart.Substring(2);

            // Parse the actual exponent value (4 in this example)
            var exponent = int.Parse(exponentDigits);

            // You also need to check the sign to determine if it's positive or negative
            if (exponentPart.Contains('-'))
            {
                exponent = -exponent;
            }

            var mantissa =
                scientificString.Substring(0, scientificString.IndexOf(match.Value, StringComparison.Ordinal));

            var decimalValue = decimal.Parse(mantissa);

            if (exponent == 0) exponent++;

            decimalValue *= (decimal)Math.Pow(10, exponent);

            return decimalValue;
        }
    }
}