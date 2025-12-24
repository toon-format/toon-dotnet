using System.Runtime.CompilerServices;

namespace Toon.Format.Internal
{
    internal static class SpanExtensions
    {
#if !NET9_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>? =>
            span.Length != 0 && (span[0]?.Equals(value) ?? (object?)value is null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>? =>
            span.Length != 0 && (span[^1]?.Equals(value) ?? (object?)value is null);
#endif
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this ReadOnlySpan<T> span, T value, int startIndex) where T : IEquatable<T>?
        {
            if (span.Length == 0) return -1;

            var index = span.Slice(startIndex).IndexOf(value);
            if (index == -1) return -1;

            return index + startIndex;
        }
    }
}