using System.Collections.Generic;

namespace Toon.Format.Internal.Shared
{
    /// <summary>
    /// One entry of a tabular field list: a leaf maps to one row cell, a nested field group to a nested object.
    /// </summary>
    internal sealed class FieldNode
    {
        public FieldNode(string name, List<FieldNode>? children = null)
        {
            Name = name;
            Children = children;
        }

        public string Name { get; }

        public List<FieldNode>? Children { get; }
    }
}
