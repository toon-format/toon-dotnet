using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Encode
{
    internal class FoldResult
    {
        /// <summary>
        /// The folded key with dot-separated segments (e.g., "data.metadata.items")
        /// </summary>
        public string FoldedKey { get; set; } = null!;

        /// <summary>
        /// The remainder value after folding:
        /// <list type="bullet">
        /// <item>`null` if the chain was fully folded to a leaf (primitive, array, or empty object)</item>
        /// <item>An object if the chain was partially folded (depth limit reached with nested tail)</item>
        /// </list>
        /// </summary>
        public JsonNode? Remainder { get; set; }

        /// <summary>
        /// The leaf value at the end of the folded chain.
        /// Used to avoid redundant traversal when encoding the folded value.
        /// </summary>
        public JsonNode LeafValue { get; set; } = null!;

        /// <summary>
        /// The number of segments that were folded.
        /// Used to calculate remaining depth budget for nested encoding.
        /// </summary>
        public int SegmentCount { get; set; }
    }

    internal class KeyChain
    {
        public IReadOnlyCollection<string> Segments { get; set; } = null!;
        public JsonNode? Tail { get; set; }
        public JsonNode LeafValue { get; set; } = null!;
    }

    internal static class Folding
    {
        public static FoldResult? TryFoldKeyChain(string key, JsonNode? value, IReadOnlyCollection<string> siblings, ResolvedEncodeOptions options, IReadOnlyCollection<string>? rootLiteralKeys = null,
            string? pathPrefix = null, int? flattenDepth = null)
        {
            if (options.KeyFolding != ToonKeyFolding.Safe)
                return null;

            if (!Normalize.IsJsonObject(value))
                return null;

            var effectiveFlattenDepth = flattenDepth ?? options.FlattenDepth;

            var keyChain = CollectSingleKeyChain(key, value, effectiveFlattenDepth);

            var segments = keyChain.Segments;
            var tail = keyChain.Tail;
            var leafValue = keyChain.LeafValue;

            // Need at least 2 segments for folding to be worthwhile
            if (segments.Count < 2)
                return null;

            if (!segments.All(ValidationShared.IsIdentifierSegment))
                return null;

            var foldedKey = BuildFoldedKey(segments);

            var absolutePath = pathPrefix != null ? $"{pathPrefix}{Constants.DOT}{foldedKey}" : foldedKey;

            // Check for collision with existing literal sibling keys (at current level)
            if (siblings.Contains(foldedKey))
                return null;

            // Check for collision with root-level literal dotted keys
            if (rootLiteralKeys != null && rootLiteralKeys.Contains(absolutePath))
                return null;

            return new FoldResult
            {
                FoldedKey = foldedKey,
                Remainder = tail,
                LeafValue = leafValue,
                SegmentCount = segments.Count
            };
        }

        private static KeyChain CollectSingleKeyChain(string startKey, JsonNode? startValue, int maxDepth)
        {
            List<string> segments = [startKey];
            var currentValue = startValue;

            while (segments.Count < maxDepth)
            {
                if (!Normalize.IsJsonObject(currentValue))
                    break;

                var jsonObject = currentValue?.AsObject();
                var keys = (jsonObject as IDictionary<string, JsonNode>)!.Keys;

                if (keys == null || keys.Count != 1)
                    break;

                var nextKey = keys.ElementAt(0);
                var nextValue = jsonObject[nextKey];

                segments.Add(nextKey);
                currentValue = nextValue;
            }

            if (!Normalize.IsJsonObject(currentValue) || Normalize.IsEmptyObject(currentValue))
            {
                return new KeyChain
                {
                    Segments =
                    segments,
                    Tail = null,
                    LeafValue = currentValue!
                };
            }

            return new KeyChain
            {
                Segments = segments,
                Tail = currentValue,
                LeafValue = currentValue!
            };
        }

        private static string BuildFoldedKey(IReadOnlyCollection<string> segments)
        {
#if NETSTANDARD2_0
            return string.Join(Constants.DOT.ToString(), segments);
#else
            return string.Join(Constants.DOT, segments);
#endif
        }
    }
}
