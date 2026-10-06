#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Toon.Format.Internal.Decode
{
    /// <summary>
    /// Represents a parsed line with its raw content, indentation, depth, and line number.
    /// </summary>
    internal class ParsedLine
    {
        public string Raw { get; set; } = string.Empty;
        public int Indent { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Depth { get; set; }
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// Information about a blank line in the source.
    /// </summary>
    internal class BlankLineInfo
    {
        public int LineNumber { get; set; }
        public int Indent { get; set; }
        public int Depth { get; set; }
    }

    /// <summary>
    /// Result of scanning source text into parsed lines.
    /// </summary>
    internal class ScanResult
    {
        public List<ParsedLine> Lines { get; set; } = new();
        public List<BlankLineInfo> BlankLines { get; set; } = new();
    }

    /// <summary>
    /// Cursor for navigating through parsed lines during decoding.
    /// </summary>
    internal class LineCursor
    {
        private readonly List<ParsedLine> _lines;
        private readonly List<BlankLineInfo> _blankLines;
        private int _index;

        public LineCursor(List<ParsedLine> lines, List<BlankLineInfo> blankLines)
        {
            _lines = lines;
            _blankLines = blankLines;
            _index = 0;
        }

        public List<BlankLineInfo> GetBlankLines() => _blankLines;

        public ParsedLine? Peek()
        {
            return _index < _lines.Count ? _lines[_index] : null;
        }

        public ParsedLine? Next()
        {
            return _index < _lines.Count ? _lines[_index++] : null;
        }

        public ParsedLine? Current()
        {
            return _index > 0 ? _lines[_index - 1] : null;
        }

        public void Advance()
        {
            _index++;
        }

        public bool AtEnd()
        {
            return _index >= _lines.Count;
        }

        public int Length => _lines.Count;
    }

    /// <summary>
    /// Splits source text into indented lines and records blank lines.
    /// </summary>
    internal static class Scanner
    {
        public static ScanResult ToParsedLines(string source, int indentSize, bool strict)
        {
            var result = new ScanResult();
            var rawLines = source.Split(Constants.NEWLINE);

            for (var i = 0; i < rawLines.Length; i++)
            {
                var raw = rawLines[i];
                var lineNumber = i + 1;

                if (lineNumber == 1 && raw.Length > 0 && raw[0] == '\uFEFF')
                    raw = raw.Substring(1);

                // A trailing carriage return belongs to the CRLF terminator, not to the content.
                if (raw.Length > 0 && raw[raw.Length - 1] == Constants.CARRIAGE_RETURN)
                    raw = raw.Substring(0, raw.Length - 1);

                var whitespaceEnd = 0;
                while (whitespaceEnd < raw.Length && (raw[whitespaceEnd] == Constants.SPACE || raw[whitespaceEnd] == Constants.TAB))
                    whitespaceEnd++;
                var firstTab = raw.IndexOf(Constants.TAB, 0, whitespaceEnd);

                // Strict rejects tab indentation below, so only the spaces before the first tab are indentation there.
                var indent = strict && firstTab != -1 ? firstTab : whitespaceEnd;
                // Non-strict input may indent with tabs, and each tab counts as one depth level.
                var tabIndent = strict || firstTab == -1 ? 0 : raw.Take(whitespaceEnd).Count(ch => ch == Constants.TAB);

                var content = raw.Substring(indent).TrimEnd(Constants.SPACE);

                // Only spaces may precede the comment marker. Comment lines vanish before blank-line
                // tracking and strict validation, so they never count as rows, items, entries, or blank lines.
                if (firstTab == -1 && content.Length > 0 && content[0] == Constants.COMMENT_MARKER)
                    continue;

                var depth = (indent - tabIndent) / indentSize + tabIndent;

                if (content.Length == 0)
                {
                    result.BlankLines.Add(new BlankLineInfo { LineNumber = lineNumber, Indent = indent, Depth = depth });
                    continue;
                }

                if (strict)
                {
                    if (firstTab != -1)
                        throw ToonFormatException.Indentation("Tabs are not allowed in indentation in strict mode", lineNumber, sourceLine: raw);

                    if (indent % indentSize != 0)
                        throw ToonFormatException.Indentation($"Indentation must be exact multiple of {indentSize}, but found {indent} spaces", lineNumber, sourceLine: raw);
                }

                result.Lines.Add(new ParsedLine { Raw = raw, Indent = indent, Content = content, Depth = depth, LineNumber = lineNumber });
            }

            return result;
        }
    }
}
