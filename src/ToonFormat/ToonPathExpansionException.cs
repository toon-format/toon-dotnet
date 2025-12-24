#nullable enable
using System;
using System.Text;

namespace Toon.Format
{
    /// <summary>
    /// Exception thrown when path expansion conflicts occur during TOON decoding.
    /// </summary>
    public sealed class ToonPathExpansionException : Exception
    {
        /// <summary>The key or path segment where the conflict occurred.</summary>
        public string Key { get; }

        /// <summary>The full dotted path being expanded (if available).</summary>
        public string? FullPath { get; }

        /// <summary>The type that was expected at this location.</summary>
        public string ExpectedType { get; }

        /// <summary>The type that was actually found at this location.</summary>
        public string ActualType { get; }

        /// <summary>Depth in the path where the conflict occurred (optional).</summary>
        public int? Depth { get; }

        /// <summary>Constructs the exception with conflict details.</summary>
        public ToonPathExpansionException(
            string key,
            string expectedType,
            string actualType,
            string? fullPath = null,
            int? depth = null,
            Exception? inner = null)
            : base(BuildMessage(key, expectedType, actualType, fullPath, depth), inner)
        {
            Key = key;
            ExpectedType = expectedType;
            ActualType = actualType;
            FullPath = fullPath;
            Depth = depth;
        }

        /// <summary>Factory method for path traversal conflicts.</summary>
        public static ToonPathExpansionException TraversalConflict(
            string segment,
            string actualType,
            string? fullPath = null,
            int? depth = null)
            => new(segment, "object", actualType, fullPath, depth);

        /// <summary>Factory method for key assignment conflicts.</summary>
        public static ToonPathExpansionException AssignmentConflict(
            string key,
            string expectedType,
            string actualType,
            string? fullPath = null,
            int? depth = null)
            => new(key, expectedType, actualType, fullPath, depth);

        private static string BuildMessage(
            string key,
            string expectedType,
            string actualType,
            string? fullPath,
            int? depth)
        {
            var sb = new StringBuilder();
            sb.Append("[PathExpansion] Conflict at '").Append(key).Append("': ");
            sb.Append("expected ").Append(expectedType);
            sb.Append(" but found ").Append(actualType);

            if (!string.IsNullOrEmpty(fullPath))
            {
                sb.Append(" (in path '").Append(fullPath).Append("')");
            }

            if (depth is not null)
            {
                sb.Append(" (depth: ").Append(depth.Value).Append(")");
            }

            return sb.ToString();
        }
    }
}
