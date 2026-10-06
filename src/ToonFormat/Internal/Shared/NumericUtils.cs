using System.Globalization;

namespace Toon.Format.Internal.Shared;

internal static class NumericUtils
{
    public static bool IsFinite(double value)
    {
#if NETSTANDARD2_0
        return !(double.IsNaN(value) || double.IsInfinity(value));
#else
        return double.IsFinite(value);
#endif
    }

    public static bool IsFinite(float value)
    {
#if NETSTANDARD2_0
        return !(float.IsNaN(value) || float.IsInfinity(value));
#else
        return float.IsFinite(value);
#endif
    }

#if NETSTANDARD2_0
    // On .NET Framework, "R" falls back to 17 digits (9 for float) where fewer parse back to the value.
    public static string ToRoundTripString(double value) =>
        Shortest(value, 17, text => double.Parse(text, CultureInfo.InvariantCulture) == value);

    public static string ToRoundTripString(float value) =>
        Shortest(value, 9, text => float.Parse(text, CultureInfo.InvariantCulture) == value);

    private static string Shortest(IFormattable value, int maxDigits, Func<string, bool> roundTrips)
    {
        for (var digits = 1; digits < maxDigits; digits++)
        {
            var text = value.ToString("E" + (digits - 1), CultureInfo.InvariantCulture);
            if (roundTrips(text))
                return text;
        }

        return value.ToString("E" + (maxDigits - 1), CultureInfo.InvariantCulture);
    }
#else
    public static string ToRoundTripString(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    public static string ToRoundTripString(float value) => value.ToString("R", CultureInfo.InvariantCulture);
#endif
}
