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
}