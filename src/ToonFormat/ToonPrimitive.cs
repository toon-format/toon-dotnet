#nullable enable
using System;
using System.Text.Json.Nodes;

namespace Toon.Format
{
    /// <summary>
    /// Represents a TOON primitive value (string, number, boolean, or null).
    /// </summary>
    /// <example>
    /// <code>
    /// // Implicit conversions
    /// ToonValue stringVal = "Hello";
    /// ToonValue intVal = 42;
    /// ToonValue doubleVal = 3.14;
    /// ToonValue boolVal = true;
    ///
    /// // Creating primitives
    /// var obj = new ToonObject
    /// {
    ///     ["name"] = "Alice",           // string
    ///     ["age"] = 30,                 // int
    ///     ["score"] = 95.5,             // double
    ///     ["active"] = true,            // bool
    ///     ["data"] = new ToonPrimitive() // null
    /// };
    ///
    /// // Accessing values
    /// var primitive = new ToonPrimitive("test");
    /// var value = primitive.GetValue&lt;string&gt;();  // Returns "test"
    /// var asInt = primitive.GetValue&lt;int?&gt;();      // Returns null (type mismatch)
    /// </code>
    /// </example>
    public class ToonPrimitive : ToonValue
    {
        private readonly JsonValue? _inner;

        /// <summary>
        /// Initializes a new ToonPrimitive with a null value.
        /// </summary>
        public ToonPrimitive()
        {
            _inner = null;
        }

        /// <summary>
        /// Initializes a new ToonPrimitive from a string.
        /// </summary>
        public ToonPrimitive(string? value)
        {
            _inner = value == null ? null : JsonValue.Create(value);
        }

        /// <summary>
        /// Initializes a new ToonPrimitive from an int.
        /// </summary>
        public ToonPrimitive(int value)
        {
            _inner = JsonValue.Create(value);
        }

        /// <summary>
        /// Initializes a new ToonPrimitive from a long.
        /// </summary>
        public ToonPrimitive(long value)
        {
            _inner = JsonValue.Create(value);
        }

        /// <summary>
        /// Initializes a new ToonPrimitive from a double.
        /// </summary>
        public ToonPrimitive(double value)
        {
            _inner = JsonValue.Create(value);
        }

        /// <summary>
        /// Initializes a new ToonPrimitive from a decimal.
        /// </summary>
        public ToonPrimitive(decimal value)
        {
            _inner = JsonValue.Create(value);
        }

        /// <summary>
        /// Initializes a new ToonPrimitive from a bool.
        /// </summary>
        public ToonPrimitive(bool value)
        {
            _inner = JsonValue.Create(value);
        }

        /// <summary>
        /// Initializes a ToonPrimitive from a JsonValue.
        /// </summary>
        internal ToonPrimitive(JsonValue? jsonValue)
        {
            _inner = jsonValue;
        }

        /// <summary>
        /// Gets the value as a string, or null if not a string.
        /// </summary>
        public string? AsString() => _inner?.TryGetValue<string>(out var val) == true ? val : null;

        /// <summary>
        /// Gets the value as an int, or null if not convertible.
        /// </summary>
        public int? AsInt() => _inner?.TryGetValue<int>(out var val) == true ? val : null;

        /// <summary>
        /// Gets the value as a long, or null if not convertible.
        /// </summary>
        public long? AsLong() => _inner?.TryGetValue<long>(out var val) == true ? val : null;

        /// <summary>
        /// Gets the value as a double, or null if not convertible.
        /// </summary>
        public double? AsDouble() => _inner?.TryGetValue<double>(out var val) == true ? val : null;

        /// <summary>
        /// Gets the value as a decimal, or null if not convertible.
        /// </summary>
        public decimal? AsDecimal() => _inner?.TryGetValue<decimal>(out var val) == true ? val : null;

        /// <summary>
        /// Gets the value as a bool, or null if not a boolean.
        /// </summary>
        public bool? AsBool() => _inner?.TryGetValue<bool>(out var val) == true ? val : null;

        /// <summary>
        /// Gets a value indicating whether this primitive is null.
        /// </summary>
        public bool IsNull => _inner == null;

        internal override JsonNode? ToJsonNode()
        {
            return _inner;
        }

        /// <summary>
        /// Returns a string representation of this primitive value.
        /// </summary>
        public override string? ToString()
        {
            return _inner?.ToString();
        }

        /// <summary>
        /// Determines whether the specified object is equal to this primitive.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is ToonPrimitive other)
            {
                if (_inner == null && other._inner == null) return true;
                if (_inner == null || other._inner == null) return false;
                return _inner.ToJsonString() == other._inner.ToJsonString();
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this primitive value.
        /// </summary>
        public override int GetHashCode()
        {
            return _inner?.GetHashCode() ?? 0;
        }
    }
}
