#nullable enable
using System.Collections.Generic;
using System.Text;

namespace ToonFormat.Internal.Encode
{
    /// <summary>
    /// Helper class for building indented lines of TOON output.
    /// Aligned with TypeScript encode/writer.ts
    /// </summary>
    internal class LineWriter
    {
        private readonly StringBuilder _builder = new();
        private readonly string _indentationUnit;
        private readonly List<string> _indentCache = new() { string.Empty };
        private bool _hasAnyLine;

        /// <summary>
        /// Creates a new LineWriter with the specified indentation size.
        /// </summary>
        /// <param name="indentSize">Number of spaces per indentation level.</param>
        public LineWriter(int indentSize)
        {
            _indentationUnit = new string(' ', indentSize);
        }

        /// <summary>
        /// Pushes a new line with the specified depth and content.
        /// </summary>
        /// <param name="depth">Indentation depth level.</param>
        /// <param name="content">The content of the line.</param>
        public void Push(int depth, string content)
        {
            if (_hasAnyLine)
            {
                _builder.Append('\n');
            }
            else
            {
                _hasAnyLine = true;
            }

            _builder.Append(GetIndent(depth));
            _builder.Append(content);
        }

        /// <summary>
        /// Pushes a list item (prefixed with "- ") at the specified depth.
        /// </summary>
        /// <param name="depth">Indentation depth level.</param>
        /// <param name="content">The content after the list item marker.</param>
        public void PushListItem(int depth, string content)
        {
            Push(depth, Constants.LIST_ITEM_PREFIX + content);
        }

        /// <summary>
        /// Returns the complete output as a single string with newlines.
        /// </summary>
        public override string ToString()
        {
            return _builder.ToString();
        }

        private string GetIndent(int depth)
        {
            if (depth <= 0)
                return string.Empty;

            while (_indentCache.Count <= depth)
            {
                _indentCache.Add(string.Concat(_indentCache[^1], _indentationUnit));
            }

            return _indentCache[depth];
        }
    }
}
