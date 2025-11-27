#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Toon.Format
{
    /// <summary>
    /// Represents a TOON array (ordered list of values).
    /// </summary>
    /// <example>
    /// <code>
    /// // Primitive array (inline format)
    /// var tags = new ToonArray { "admin", "ops", "dev" };
    /// var obj = new ToonObject { ["tags"] = tags };
    /// var encoded = ToonEncoder.Encode(obj);
    /// // Output: tags[3]: admin,ops,dev
    ///
    /// // Tabular array (uniform objects)
    /// var users = new ToonArray
    /// {
    ///     new ToonObject { ["id"] = 1, ["name"] = "Alice", ["role"] = "admin" },
    ///     new ToonObject { ["id"] = 2, ["name"] = "Bob", ["role"] = "user" }
    /// };
    /// var data = new ToonObject { ["users"] = users };
    /// var encoded2 = ToonEncoder.Encode(data);
    /// // Output:
    /// // users[2]{id,name,role}:
    /// //   1,Alice,admin
    /// //   2,Bob,user
    ///
    /// // Mixed array (expanded format)
    /// var mixed = new ToonArray
    /// {
    ///     1,
    ///     "text",
    ///     new ToonObject { ["key"] = "value" }
    /// };
    /// </code>
    /// </example>
    public class ToonArray : ToonValue, IList<ToonValue?>
    {
        private readonly JsonArray _inner;

        /// <summary>
        /// Initializes a new empty ToonArray.
        /// </summary>
        public ToonArray()
        {
            _inner = new JsonArray();
        }

        /// <summary>
        /// Initializes a ToonArray from a JsonArray.
        /// </summary>
        internal ToonArray(JsonArray jsonArray)
        {
            _inner = jsonArray;
        }

        /// <summary>
        /// Gets the internal JsonArray representation.
        /// </summary>
        internal JsonArray Inner => _inner;

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public ToonValue? this[int index]
        {
            get => FromJsonNode(_inner[index]);
            set => _inner[index] = value?.ToJsonNode();
        }

        /// <summary>
        /// Gets the number of elements in the array.
        /// </summary>
        public int Count => _inner.Count;

        /// <summary>
        /// Gets a value indicating whether the array is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an element to the end of the array.
        /// </summary>
        public void Add(ToonValue? item)
        {
            _inner.Add(item?.ToJsonNode());
        }

        /// <summary>
        /// Removes all elements from the array.
        /// </summary>
        public void Clear()
        {
            _inner.Clear();
        }

        /// <summary>
        /// Determines whether the array contains a specific value.
        /// </summary>
        public bool Contains(ToonValue? item)
        {
            return _inner.Any(node => FromJsonNode(node)?.Equals(item) == true);
        }

        /// <summary>
        /// Copies the elements to an array.
        /// </summary>
        public void CopyTo(ToonValue?[] array, int arrayIndex)
        {
            var items = _inner.Select(FromJsonNode).ToArray();
            items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the array.
        /// </summary>
        public IEnumerator<ToonValue?> GetEnumerator()
        {
            return _inner.Select(FromJsonNode).GetEnumerator();
        }

        /// <summary>
        /// Determines the index of a specific item.
        /// </summary>
        public int IndexOf(ToonValue? item)
        {
            for (int i = 0; i < _inner.Count; i++)
            {
                if (FromJsonNode(_inner[i])?.Equals(item) == true)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Inserts an element at the specified index.
        /// </summary>
        public void Insert(int index, ToonValue? item)
        {
            _inner.Insert(index, item?.ToJsonNode());
        }

        /// <summary>
        /// Removes the first occurrence of a specific value.
        /// </summary>
        public bool Remove(ToonValue? item)
        {
            var index = IndexOf(item);
            if (index >= 0)
            {
                _inner.RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            _inner.RemoveAt(index);
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
        /// Implicitly converts a ToonArray to a JsonArray.
        /// </summary>
        public static implicit operator JsonArray(ToonArray toonArray)
        {
            return toonArray._inner;
        }

        /// <summary>
        /// Implicitly converts a JsonArray to a ToonArray.
        /// </summary>
        public static implicit operator ToonArray(JsonArray jsonArray)
        {
            return new ToonArray(jsonArray);
        }
    }
}
