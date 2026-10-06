namespace Toon.Format.Internal.Encode
{
    /// <summary>
    /// Collects indented output lines.
    /// </summary>
    internal class LineWriter
    {
        private readonly List<string> _lines = new();
        private readonly int _indentSize;

        public LineWriter(int indentSize)
        {
            _indentSize = indentSize;
        }

        public void Push(int depth, string content) => _lines.Add(new string(Constants.SPACE, depth * _indentSize) + content);

        public void PushListItem(int depth, string content) => Push(depth, Constants.LIST_ITEM_PREFIX + content);

        public override string ToString() => string.Join("\n", _lines);
    }
}
