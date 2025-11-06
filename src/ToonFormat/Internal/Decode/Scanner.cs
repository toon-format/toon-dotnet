#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToonFormat.Internal.Decode
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
    /// Aligned with TypeScript decode/scanner.ts LineCursor
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

        public ParsedLine? PeekAtDepth(int targetDepth)
        {
            var line = Peek();
            if (line == null || line.Depth < targetDepth)
                return null;
            if (line.Depth == targetDepth)
                return line;
            return null;
        }

        public bool HasMoreAtDepth(int targetDepth)
        {
            return PeekAtDepth(targetDepth) != null;
        }
    }

    /// <summary>
    /// Scanner utilities for parsing source text into structured lines.
    /// Aligned with TypeScript decode/scanner.ts
    /// </summary>
    internal static class Scanner
    {
        /// <summary>
        /// Parses source text into a list of structured lines with depth information.
        /// </summary>
        public static ScanResult ToParsedLines(string source, int indentSize, bool strict)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return new ScanResult();
            }

            var lines = source.Split('\n');
            var parsed = new List<ParsedLine>();
            var blankLines = new List<BlankLineInfo>();

            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var lineNumber = i + 1;
                int indent = 0;
                
                while (indent < raw.Length && raw[indent] == Constants.SPACE)
                {
                    indent++;
                }

                var content = raw.Substring(indent);

                // Track blank lines
                if (string.IsNullOrWhiteSpace(content))
                {
                    var depth = ComputeDepthFromIndent(indent, indentSize);
                    blankLines.Add(new BlankLineInfo 
                    { 
                        LineNumber = lineNumber, 
                        Indent = indent, 
                        Depth = depth 
                    });
                    continue;
                }

                var lineDepth = ComputeDepthFromIndent(indent, indentSize);

                // Strict mode validation
                if (strict)
                {
                    // Find the full leading whitespace region (spaces and tabs)
                    int wsEnd = 0;
                    while (wsEnd < raw.Length && (raw[wsEnd] == Constants.SPACE || raw[wsEnd] == Constants.TAB))
                    {
                        wsEnd++;
                    }

                    // Check for tabs in leading whitespace (before actual content)
                    if (raw.Substring(0, wsEnd).Contains(Constants.TAB))
                    {
                        throw ToonFormatException.Syntax($"Line {lineNumber}: Tabs are not allowed in indentation in strict mode");
                    }

                    // Check for exact multiples of indentSize
                    if (indent > 0 && indent % indentSize != 0)
                    {
                        throw ToonFormatException.Syntax($"Line {lineNumber}: Indentation must be exact multiple of {indentSize}, but found {indent} spaces");
                    }
                }

                parsed.Add(new ParsedLine
                {
                    Raw = raw,
                    Indent = indent,
                    Content = content,
                    Depth = lineDepth,
                    LineNumber = lineNumber
                });
            }

            return new ScanResult { Lines = parsed, BlankLines = blankLines };
        }

        private static int ComputeDepthFromIndent(int indentSpaces, int indentSize)
        {
            return indentSpaces / indentSize;
        }
    }
}
