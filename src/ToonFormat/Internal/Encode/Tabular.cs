#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Encode
{
    /// <summary>
    /// Classifies arrays of objects into tabular field lists and reads row cells in field order.
    /// </summary>
    internal static class Tabular
    {
        /// <summary>
        /// Returns the field list of rows that share one key set, with uniform nested object columns
        /// as nested field groups, or null when the rows are not tabular.
        /// </summary>
        public static List<FieldNode>? ExtractTabularFields(IReadOnlyList<JsonObject> rows)
        {
            if (rows.Count == 0 || rows[0].Count == 0)
                return null;

            var firstKeys = rows[0].Select(property => property.Key).ToList();
            if (rows.Any(row => row.Count != firstKeys.Count || !firstKeys.All(row.ContainsKey)))
                return null;

            var fields = new List<FieldNode>(firstKeys.Count);
            foreach (var key in firstKeys)
            {
                var field = ClassifyColumn(key, rows.Select(row => row[key]).ToList());
                if (field == null)
                    return null;
                fields.Add(field);
            }

            return fields;
        }

        /// <summary>
        /// Returns the field list for an object of at least two non-empty objects that share one key set,
        /// or null when the object is not keyed-tabular.
        /// </summary>
        public static List<FieldNode>? ExtractKeyedTabularFields(JsonObject value)
        {
            if (value.Count < 2 || !value.All(entry => entry.Value is JsonObject obj && obj.Count > 0))
                return null;

            return ExtractTabularFields(value.Select(entry => (JsonObject)entry.Value!).ToList());
        }

        /// <summary>
        /// Appends one row's leaf cells in the order <see cref="ExtractTabularFields"/> produced.
        /// </summary>
        public static void CollectRowLeaves(JsonObject row, IReadOnlyList<FieldNode> fields, List<JsonNode?> leaves)
        {
            foreach (var field in fields)
            {
                if (field.Children != null)
                    CollectRowLeaves((JsonObject)row[field.Name]!, field.Children, leaves);
                else
                    leaves.Add(row[field.Name]);
            }
        }

        private static FieldNode? ClassifyColumn(string name, List<JsonNode?> values)
        {
            if (values.All(Normalize.IsJsonPrimitive))
                return new FieldNode(name);

            if (!values.All(value => value is JsonObject obj && obj.Count > 0))
                return null;

            var children = ExtractTabularFields(values.Cast<JsonObject>().ToList());
            return children == null ? null : new FieldNode(name, children);
        }
    }
}
