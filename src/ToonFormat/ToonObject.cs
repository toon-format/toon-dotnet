#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;

namespace Toon.Format
{
    /// <summary>
    /// Represents a TOON object (key-value dictionary).
    /// </summary>
    /// <example>
    /// <code>
    /// // Create an object
    /// var user = new ToonObject
    /// {
    ///     ["id"] = 123,
    ///     ["name"] = "Alice",
    ///     ["email"] = "alice@example.com"
    /// };
    ///
    /// // Access values
    /// var name = user["name"];
    ///
    /// // Nested objects
    /// var obj = new ToonObject
    /// {
    ///     ["user"] = new ToonObject
    ///     {
    ///         ["profile"] = new ToonObject
    ///         {
    ///             ["name"] = "Alice"
    ///         }
    ///     }
    /// };
    ///
    /// // With key folding
    /// var options = new ToonEncodeOptions { KeyFolding = KeyFolding.Safe };
    /// var encoded = ToonEncoder.Encode(obj, options);
    /// // Output: user.profile.name: Alice
    /// </code>
    /// </example>
    public class ToonObject : ToonValue, IDictionary<string, ToonValue?>
    {
        private readonly JsonObject _inner;

        /// <summary>
        /// Initializes a new empty ToonObject.
        /// </summary>
        public ToonObject()
        {
            _inner = new JsonObject();
        }

        /// <summary>
        /// Initializes a ToonObject from a JsonObject.
        /// </summary>
        internal ToonObject(JsonObject jsonObject)
        {
            _inner = jsonObject;
        }

        /// <summary>
        /// Gets the internal JsonObject representation.
        /// </summary>
        internal JsonObject Inner => _inner;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        public ToonValue? this[string key]
        {
            get => FromJsonNode(_inner[key]);
            set => _inner[key] = value?.ToJsonNode();
        }

        /// <summary>
        /// Gets the number of key-value pairs in the object.
        /// </summary>
        public int Count => _inner.Count;

        /// <summary>
        /// Gets a value indicating whether the object is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets a collection containing the keys.
        /// </summary>
        public ICollection<string> Keys => _inner.Select(kvp => kvp.Key).ToList();

        /// <summary>
        /// Gets a collection containing the values.
        /// </summary>
        public ICollection<ToonValue?> Values => _inner.Select(kvp => FromJsonNode(kvp.Value)).ToList();

        /// <summary>
        /// Adds a key-value pair to the object.
        /// </summary>
        public void Add(string key, ToonValue? value)
        {
            _inner.Add(key, value?.ToJsonNode());
        }

        /// <summary>
        /// Adds a key-value pair to the object.
        /// </summary>
        public void Add(KeyValuePair<string, ToonValue?> item)
        {
            _inner.Add(item.Key, item.Value?.ToJsonNode());
        }

        /// <summary>
        /// Removes all key-value pairs from the object.
        /// </summary>
        public void Clear()
        {
            _inner.Clear();
        }

        /// <summary>
        /// Determines whether the object contains a specific key-value pair.
        /// </summary>
        public bool Contains(KeyValuePair<string, ToonValue?> item)
        {
            return _inner.TryGetPropertyValue(item.Key, out var value) &&
                   FromJsonNode(value)?.Equals(item.Value) == true;
        }

        /// <summary>
        /// Determines whether the object contains the specified key.
        /// </summary>
        public bool ContainsKey(string key)
        {
            return _inner.ContainsKey(key);
        }

        /// <summary>
        /// Copies the elements to an array.
        /// </summary>
        public void CopyTo(KeyValuePair<string, ToonValue?>[] array, int arrayIndex)
        {
            var items = _inner.Select(kvp => new KeyValuePair<string, ToonValue?>(kvp.Key, FromJsonNode(kvp.Value))).ToArray();
            items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the object.
        /// </summary>
        public IEnumerator<KeyValuePair<string, ToonValue?>> GetEnumerator()
        {
            return _inner.Select(kvp => new KeyValuePair<string, ToonValue?>(kvp.Key, FromJsonNode(kvp.Value))).GetEnumerator();
        }

        /// <summary>
        /// Removes the value with the specified key.
        /// </summary>
        public bool Remove(string key)
        {
            return _inner.Remove(key);
        }

        /// <summary>
        /// Removes a specific key-value pair.
        /// </summary>
        public bool Remove(KeyValuePair<string, ToonValue?> item)
        {
            if (Contains(item))
            {
                return _inner.Remove(item.Key);
            }
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out ToonValue? value)
        {
            if (_inner.TryGetPropertyValue(key, out var jsonValue))
            {
                value = FromJsonNode(jsonValue);
                return true;
            }
            value = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal override JsonNode? ToJsonNode()
        {
            return _inner;
        }

        /// <summary>
        /// Implicitly converts a ToonObject to a JsonObject.
        /// </summary>
        public static implicit operator JsonObject(ToonObject toonObject)
        {
            return toonObject._inner;
        }

        /// <summary>
        /// Implicitly converts a JsonObject to a ToonObject.
        /// </summary>
        public static implicit operator ToonObject(JsonObject jsonObject)
        {
            return new ToonObject(jsonObject);
        }
    }
}
