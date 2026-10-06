using System;
using System.Collections.Generic;
using System.Linq;

namespace Toon.Format.Internal.Decode
{
    /// <summary>
    /// A non-blank, non-comment line: its content without indentation and trailing spaces, and its depth.
    /// </summary>
    internal sealed class ParsedLine
    {
        public string Raw { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Depth { get; set; }
        public int LineNumber { get; set; }
    }

    internal sealed class ScanResult
    {
        public List<ParsedLine> Lines { get; } = new();

        /// <summary>Line numbers of blank lines, which strict mode rejects inside arrays and keyed objects.</summary>
        public List<int> BlankLines { get; } = new();
    }

    /// <summary>
    /// Reads scanned lines with one line of lookahead.
    /// </summary>
    internal sealed class LineCursor
    {
        private readonly List<ParsedLine> _lines;
        private int _index;

        public LineCursor(ScanResult scan)
        {
            _lines = scan.Lines;
            BlankLines = scan.BlankLines;
        }

        public List<int> BlankLines { get; }

        /// <summary>The line <see cref="Next"/> returned last.</summary>
        public ParsedLine? LastLine => _index > 0 ? _lines[_index - 1] : null;

        public ParsedLine? Peek() => _index < _lines.Count ? _lines[_index] : null;

        public ParsedLine? Next() => _index < _lines.Count ? _lines[_index++] : null;
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

                if (lineNumber == 1 && raw.Length > 0 && raw[0] == Constants.BYTE_ORDER_MARK)
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

                if (content.Length == 0)
                {
                    result.BlankLines.Add(lineNumber);
                    continue;
                }

                if (strict)
                {
                    if (firstTab != -1)
                        throw ToonFormatException.Indentation("Tabs are not allowed in indentation in strict mode", lineNumber, sourceLine: raw);

                    if (indent % indentSize != 0)
                        throw ToonFormatException.Indentation($"Indentation must be exact multiple of {indentSize}, but found {indent} spaces", lineNumber, sourceLine: raw);
                }

                var depth = (indent - tabIndent) / indentSize + tabIndent;
                result.Lines.Add(new ParsedLine { Raw = raw, Content = content, Depth = depth, LineNumber = lineNumber });
            }

            return result;
        }
    }
}
