#nullable enable
using System;
using System.Text.Json.Nodes;

namespace Toon.Format
{
    /// <summary>
    /// Base class for all TOON values.
    /// </summary>
    /// <example>
    /// <code>
    /// // Create a ToonObject
    /// var obj = new ToonObject
    /// {
    ///     ["name"] = "Alice",
    ///     ["age"] = 30,
    ///     ["active"] = true
    /// };
    ///
    /// // Encode to TOON format
    /// var encoded = ToonEncoder.Encode(obj);
    /// // Output:
    /// // name: Alice
    /// // age: 30
    /// // active: true
    /// </code>
    /// </example>
    public abstract class ToonValue
    {
        /// <summary>
        /// Converts this ToonValue to its internal JsonNode representation.
        /// </summary>
        internal abstract JsonNode? ToJsonNode();

        /// <summary>
        /// Creates a ToonValue from a JsonNode.
        /// </summary>
        internal static ToonValue? FromJsonNode(JsonNode? node)
        {
            if (node == null)
                return null;

            if (node is JsonObject jsonObject)
                return new ToonObject(jsonObject);

            if (node is JsonArray jsonArray)
                return new ToonArray(jsonArray);

            if (node is JsonValue jsonValue)
                return new ToonPrimitive(jsonValue);

            return null;
        }

        /// <summary>
        /// Implicitly converts a string to a ToonPrimitive.
        /// </summary>
        public static implicit operator ToonValue?(string? value)
        {
            return value == null ? null : new ToonPrimitive(value);
        }

        /// <summary>
        /// Implicitly converts an int to a ToonPrimitive.
        /// </summary>
        public static implicit operator ToonValue(int value)
        {
            return new ToonPrimitive(value);
        }

        /// <summary>
        /// Implicitly converts a double to a ToonPrimitive.
        /// </summary>
        public static implicit operator ToonValue(double value)
        {
            return new ToonPrimitive(value);
        }

        /// <summary>
        /// Implicitly converts a bool to a ToonPrimitive.
        /// </summary>
        public static implicit operator ToonValue(bool value)
        {
            return new ToonPrimitive(value);
        }

        /// <summary>
        /// Implicitly converts a long to a ToonPrimitive.
        /// </summary>
        public static implicit operator ToonValue(long value)
        {
            return new ToonPrimitive(value);
        }

        /// <summary>
        /// Implicitly converts a decimal to a ToonPrimitive.
        /// </summary>
        public static implicit operator ToonValue(decimal value)
        {
            return new ToonPrimitive(value);
        }
    }
}
