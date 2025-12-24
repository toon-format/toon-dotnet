#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Decode
{
    /// <summary>
    /// Path expansion logic for dotted keys per SPEC §13.4
    /// </summary>
    internal static class PathExpansion
    {
        /// <summary>
        /// Expands dotted keys in a JsonObject into nested structures.
        /// Example: {"a.b.c": 1} -> {"a": {"b": {"c": 1}}}
        /// </summary>
        /// <param name="obj">The object to expand</param>
        /// <param name="strict">Whether to throw on conflicts</param>
        /// <param name="quotedKeys">Set of keys that were originally quoted (should not be expanded)</param>
        public static JsonObject ExpandPaths(JsonObject obj, bool strict, HashSet<string>? quotedKeys = null)
        {
            var result = new JsonObject();

            foreach (var kvp in obj)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                // Skip expansion for quoted keys (they should remain as literal dotted keys)
                bool wasQuoted = quotedKeys != null && quotedKeys.Contains(key);

                // Check if key contains dots and is eligible for expansion
                if (!wasQuoted && key.Contains(Constants.DOT) && IsExpandable(key))
                {
                    // Split and expand
                    var segments = key.Split(Constants.DOT);
                    SetNestedValue(result, segments, value, strict);
                }
                else
                {
                    // Not expandable or was quoted, set directly
                    SetValue(result, key, value, strict);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a dotted key is eligible for expansion.
        /// All segments must be valid identifiers.
        /// </summary>
        private static bool IsExpandable(string key)
        {
            var segments = key.Split(Constants.DOT);
            return segments.All(segment => ValidationShared.IsIdentifierSegment(segment));
        }

        /// <summary>
        /// Sets a nested value by traversing/creating the path
        /// </summary>
        private static void SetNestedValue(JsonObject target, string[] segments, JsonNode? value, bool strict)
        {
            var current = target;

            for (int i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];

                if (current.ContainsKey(segment))
                {
                    var existing = current[segment];

                    if (existing is JsonObject existingObj)
                    {
                        // Continue traversing
                        current = existingObj;
                    }
                    else
                    {
                        // Conflict: path requires object but found non-object
                        if (strict)
                        {
                            throw ToonPathExpansionException.TraversalConflict(
                                segment,
                                GetTypeName(existing),
                                string.Join(".", segments),
                                i
                            );
                        }
                        else
                        {
                            // LWW: replace with new object
                            var newObj = new JsonObject();
                            current[segment] = newObj;
                            current = newObj;
                        }
                    }
                }
                else
                {
                    // Create new object at this segment
                    var newObj = new JsonObject();
                    current[segment] = newObj;
                    current = newObj;
                }
            }

            // Set the final value
            var lastSegment = segments[segments.Length - 1];
            SetValue(current, lastSegment, value, strict);
        }

        /// <summary>
        /// Sets a value in an object, handling conflicts
        /// </summary>
        private static void SetValue(JsonObject target, string key, JsonNode? value, bool strict)
        {
            if (target.ContainsKey(key))
            {
                var existing = target[key];

                // Check for conflicts
                bool conflict = false;

                if (value is JsonObject && !(existing is JsonObject))
                {
                    conflict = true;
                }
                else if (!(value is JsonObject) && existing is JsonObject)
                {
                    conflict = true;
                }

                if (conflict)
                {
                    if (strict)
                    {
                        throw ToonPathExpansionException.AssignmentConflict(
                            key,
                            GetTypeName(value),
                            GetTypeName(existing)
                        );
                    }
                    // LWW: just overwrite
                }

                // If both are objects, deep merge
                if (value is JsonObject valueObj && existing is JsonObject existingObj)
                {
                    DeepMerge(existingObj, valueObj, strict);
                    return;
                }
            }

            // Set or overwrite
            target[key] = value?.DeepClone();
        }

        /// <summary>
        /// Deep merges source into target
        /// </summary>
        private static void DeepMerge(JsonObject target, JsonObject source, bool strict)
        {
            foreach (var kvp in source)
            {
                SetValue(target, kvp.Key, kvp.Value, strict);
            }
        }

        /// <summary>
        /// Gets a human-readable type name for error messages
        /// </summary>
        private static string GetTypeName(JsonNode? node)
        {
            if (node == null) return "null";
            if (node is JsonObject) return "object";
            if (node is JsonArray) return "array";
            return "primitive";
        }
    }
}
