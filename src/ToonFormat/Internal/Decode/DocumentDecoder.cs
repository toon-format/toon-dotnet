#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Decode
{
    /// <summary>
    /// Decodes scanned TOON lines into a <see cref="JsonNode"/>, one rule per syntactic form.
    /// </summary>
    internal sealed class DocumentDecoder
    {
        private const string EmptyArray = "[]";

        private readonly LineCursor _cursor;
        private readonly bool _strict;

        public DocumentDecoder(LineCursor cursor, bool strict)
        {
            _cursor = cursor;
            _strict = strict;
        }

        #region Document

        public JsonNode? DecodeDocument()
        {
            var first = _cursor.Peek();
            var skippedLeading = false;
            while (first != null && first.Depth != 0)
            {
                SkipOverIndentedLine(first, 0);
                skippedLeading = true;
                first = _cursor.Peek();
            }

            if (first == null)
                return new JsonObject();

            if (StringUtils.TrimSpaces(first.Content) == EmptyArray)
            {
                _cursor.Next();
                AssertFullyConsumed();
                return new JsonArray();
            }

            if (Parser.IsArrayHeaderAfterHyphen(first.Content))
            {
                var header = At(first, () => Parser.ParseArrayHeaderLine(first.Content, Constants.DEFAULT_DELIMITER_CHAR));
                if (header != null)
                {
                    _cursor.Next();
                    var array = DecodeArrayFromHeader(header.Header, header.InlineValues, 0, first);
                    AssertFullyConsumed();
                    return array;
                }
            }

            _cursor.Next();
            var following = _cursor.Peek();
            // A skipped leading line makes the document multi-line, so no root primitive.
            if (following == null && !skippedLeading && !IsKeyValueContent(first.Content))
                return At(first, () => Parser.ParsePrimitiveToken(first.Content));

            if (!IsKeyValueContent(first.Content) && following?.Depth == 0)
                throw ToonFormatException.Syntax("Top-level document must start with a key-value or array-header line", first.LineNumber, sourceLine: first.Raw);

            var root = new JsonObject();
            DecodeKeyValue(first, root, 0);

            for (var line = _cursor.Peek(); line != null; line = _cursor.Peek())
            {
                if (line.Depth != 0)
                {
                    SkipOverIndentedLine(line, 0);
                    continue;
                }

                _cursor.Next();
                DecodeKeyValue(line, root, 0);
            }

            return root;
        }

        private static bool IsKeyValueContent(string content) => StringUtils.FindUnquotedChar(content, Constants.COLON) != -1;

        #endregion

        #region Objects

        private void DecodeKeyValue(ParsedLine line, JsonObject target, int baseDepth)
        {
            var content = line.Content;

            var header = At(line, () => Parser.ParseArrayHeaderLine(content, Constants.DEFAULT_DELIMITER_CHAR));
            if (header?.Header.Key != null)
            {
                AssertNewKey(target, header.Header.Key, line);
                target[header.Header.Key] = DecodeArrayFromHeader(header.Header, header.InlineValues, baseDepth, line);
                return;
            }

            var keyToken = At(line, () => Parser.ParseKeyToken(content, 0));
            var rest = StringUtils.TrimSpaces(content.Substring(keyToken.End));
            AssertNewKey(target, keyToken.Key, line);

            if (rest.Length == 0)
            {
                var next = _cursor.Peek();
                if (next != null && next.Depth > baseDepth)
                {
                    AssertNoDepthJump(next, baseDepth);
                    target[keyToken.Key] = DecodeObjectFields(baseDepth + 1);
                    return;
                }

                target[keyToken.Key] = new JsonObject();
                return;
            }

            if (rest == EmptyArray)
            {
                target[keyToken.Key] = new JsonArray();
                return;
            }

            target[keyToken.Key] = At(line, () => Parser.ParsePrimitiveToken(rest));
        }

        private JsonObject DecodeObjectFields(int baseDepth)
        {
            var obj = new JsonObject();
            int? fieldDepth = null;

            for (var line = _cursor.Peek(); line != null && line.Depth >= baseDepth; line = _cursor.Peek())
            {
                fieldDepth ??= line.Depth;

                if (line.Depth == fieldDepth)
                {
                    _cursor.Next();
                    DecodeKeyValue(line, obj, fieldDepth.Value);
                }
                else
                {
                    SkipOverIndentedLine(line, fieldDepth.Value);
                }
            }

            return obj;
        }

        #endregion

        #region Arrays

        private JsonNode DecodeArrayFromHeader(ArrayHeaderInfo header, string? inlineValues, int baseDepth, ParsedLine headerLine)
        {
            if (inlineValues != null)
                return DecodeInlinePrimitiveArray(header, inlineValues, headerLine);

            if (header.Fields != null && header.Fields.Count > 0)
                return DecodeTabularArray(header, baseDepth, headerLine);

            return DecodeListArray(header, baseDepth, headerLine);
        }

        private JsonArray DecodeInlinePrimitiveArray(ArrayHeaderInfo header, string inlineValues, ParsedLine headerLine)
        {
            var array = new JsonArray();
            if (StringUtils.TrimSpaces(inlineValues).Length > 0)
            {
                foreach (var value in At(headerLine, () => Parser.ParseDelimitedValues(inlineValues, header.Delimiter)))
                    array.Add(At(headerLine, () => Parser.ParsePrimitiveToken(value)));
            }

            Validation.AssertExpectedCount(array.Count, header.Length, "inline-form values", _strict, headerLine);
            return array;
        }

        private JsonArray DecodeTabularArray(ArrayHeaderInfo header, int baseDepth, ParsedLine headerLine)
        {
            var rows = new JsonArray();
            var rowDepth = ScopeContentDepth(baseDepth);
            var lastRowLine = headerLine;
            int? startLine = null;

            // Only strict stops at N and leaves the surplus to the extra-row check; non-strict reads on, so [N] never truncates.
            while (!_strict || rows.Count < header.Length)
            {
                var line = _cursor.Peek();
                if (line == null || line.Depth <= baseDepth)
                    break;

                if (line.Depth != rowDepth)
                {
                    SkipOverIndentedLine(line, rowDepth);
                    continue;
                }

                if (!Validation.IsDataRow(line.Content, header.Delimiter))
                    break;

                _cursor.Next();
                startLine ??= line.LineNumber;
                lastRowLine = line;

                var values = At(line, () => Parser.ParseDelimitedValues(line.Content, header.Delimiter));
                Validation.AssertExpectedCount(values.Count, header.Fields!.Count, "tabular row values", _strict, line);

                var row = new JsonObject();
                for (var i = 0; i < header.Fields.Count && i < values.Count; i++)
                    row[header.Fields[i]] = At(line, () => Parser.ParsePrimitiveToken(values[i]));
                rows.Add(row);
            }

            Validation.AssertExpectedCount(rows.Count, header.Length, "tabular rows", _strict, lastRowLine);

            if (_strict)
            {
                if (startLine != null)
                    Validation.ValidateNoBlankLinesInRange(startLine.Value, lastRowLine.LineNumber, _cursor.BlankLines, "tabular array");

                Validation.ValidateNoExtraTabularRows(_cursor.Peek(), rowDepth, header);
            }

            return rows;
        }

        private JsonArray DecodeListArray(ArrayHeaderInfo header, int baseDepth, ParsedLine headerLine)
        {
            var items = new JsonArray();
            var itemDepth = ScopeContentDepth(baseDepth);
            var lastItemLine = headerLine;
            int? startLine = null;

            // Only strict stops at N and leaves the surplus to the extra-item check; non-strict reads on, so [N] never truncates.
            while (!_strict || items.Count < header.Length)
            {
                var line = _cursor.Peek();
                if (line == null || line.Depth <= baseDepth)
                    break;

                if (line.Depth != itemDepth)
                {
                    SkipOverIndentedLine(line, itemDepth);
                    continue;
                }

                if (!IsListItem(line.Content))
                    break;

                startLine ??= line.LineNumber;
                items.Add(DecodeListItem(itemDepth));
                lastItemLine = _cursor.LastLine!;
            }

            Validation.AssertExpectedCount(items.Count, header.Length, "list-form items", _strict, lastItemLine);

            if (_strict)
            {
                if (startLine != null)
                    Validation.ValidateNoBlankLinesInRange(startLine.Value, lastItemLine.LineNumber, _cursor.BlankLines, "list-form array");

                Validation.ValidateNoExtraListItems(_cursor.Peek(), itemDepth, header.Length);
            }

            return items;
        }

        private int ScopeContentDepth(int baseDepth)
        {
            var first = _cursor.Peek();
            if (first == null || first.Depth <= baseDepth + 1)
                return baseDepth + 1;

            AssertNoDepthJump(first, baseDepth);
            return first.Depth;
        }

        #endregion

        #region List items

        private static bool IsListItem(string content) =>
            content.StartsWith(Constants.LIST_ITEM_PREFIX, StringComparison.Ordinal) || content == Constants.LIST_ITEM_MARKER.ToString();

        private JsonNode? DecodeListItem(int itemDepth)
        {
            var line = _cursor.Next()!;
            if (line.Content == Constants.LIST_ITEM_MARKER.ToString())
                return new JsonObject();

            var afterHyphen = line.Content.Substring(Constants.LIST_ITEM_PREFIX.Length);
            if (StringUtils.TrimSpaces(afterHyphen).Length == 0)
                return new JsonObject();

            if (StringUtils.TrimSpaces(afterHyphen) == EmptyArray)
                return new JsonArray();

            var itemLine = new ParsedLine { Raw = line.Raw, Indent = line.Indent, Content = afterHyphen, Depth = line.Depth, LineNumber = line.LineNumber };

            if (Parser.IsArrayHeaderAfterHyphen(afterHyphen))
            {
                var header = At(itemLine, () => Parser.ParseArrayHeaderLine(afterHyphen, Constants.DEFAULT_DELIMITER_CHAR));
                if (header != null)
                    return DecodeArrayFromHeader(header.Header, header.InlineValues, itemDepth, itemLine);
            }

            if (IsKeyValueContent(afterHyphen))
            {
                var obj = new JsonObject();
                var header = At(itemLine, () => Parser.ParseArrayHeaderLine(afterHyphen, Constants.DEFAULT_DELIMITER_CHAR));
                if (header?.Header.Key != null && header.Header.Fields != null)
                    obj[header.Header.Key] = DecodeArrayFromHeader(header.Header, header.InlineValues, itemDepth + 1, itemLine);
                else
                    DecodeKeyValue(itemLine, obj, itemDepth + 1);

                FollowSiblingFields(obj, itemDepth + 1);
                return obj;
            }

            return At(itemLine, () => Parser.ParsePrimitiveToken(afterHyphen));
        }

        private void FollowSiblingFields(JsonObject obj, int fieldDepth)
        {
            for (var line = _cursor.Peek(); line != null && line.Depth >= fieldDepth; line = _cursor.Peek())
            {
                // A hyphen marks a list item only at item depth, so a `- ` line here is a further field.
                if (line.Depth != fieldDepth)
                {
                    SkipOverIndentedLine(line, fieldDepth);
                    continue;
                }

                _cursor.Next();
                DecodeKeyValue(line, obj, fieldDepth);
            }
        }

        #endregion

        #region Errors

        private void AssertNoDepthJump(ParsedLine firstNestedLine, int parentDepth)
        {
            if (_strict && firstNestedLine.Depth > parentDepth + 1)
                throw ToonFormatException.Indentation($"Indentation depth jump: expected depth {parentDepth + 1}, but found {firstNestedLine.Depth}", firstNestedLine.LineNumber, sourceLine: firstNestedLine.Raw);
        }

        private void SkipOverIndentedLine(ParsedLine line, int contentDepth)
        {
            if (_strict)
                throw ToonFormatException.Indentation($"Over-indented line: expected depth {contentDepth}, but found {line.Depth}", line.LineNumber, sourceLine: line.Raw);

            AssertNotScalarLine(line);
            _cursor.Next();
        }

        // Strict decoding never silently discards input, so a line after the root form is an error.
        // Non-strict decoding skips it, except a bare token, which errors in both modes.
        private void AssertFullyConsumed()
        {
            if (!_strict)
            {
                for (var line = _cursor.Next(); line != null; line = _cursor.Next())
                    AssertNotScalarLine(line);
                return;
            }

            var trailing = _cursor.Peek();
            if (trailing != null)
                throw ToonFormatException.Validation("Unexpected content after the document root", trailing.LineNumber, sourceLine: trailing.Raw);
        }

        // Both modes reject a bare token outside root primitive position, so it must not reach the
        // non-strict paths that drop an over-indented line. A hyphen-leading line reaching here is
        // off item depth, so it is no list item either.
        private static void AssertNotScalarLine(ParsedLine line)
        {
            if (StringUtils.FindUnquotedChar(line.Content, Constants.COLON) == -1)
                throw ToonFormatException.Syntax("Unexpected bare token line outside root primitive position", line.LineNumber, sourceLine: line.Raw);
        }

        private void AssertNewKey(JsonObject target, string key, ParsedLine line)
        {
            if (_strict && target.ContainsKey(key))
                throw ToonFormatException.Validation($"Duplicate sibling key \"{key}\"", line.LineNumber, sourceLine: line.Raw);
        }

        /// <summary>
        /// Runs a parse helper, which can't know its line, and attaches the line to any format error.
        /// </summary>
        private static T At<T>(ParsedLine line, Func<T> parse)
        {
            try
            {
                return parse();
            }
            catch (ToonFormatException ex) when (ex.LineNumber is null)
            {
                throw ex.AtLine(line.LineNumber, line.Raw);
            }
        }

        #endregion
    }
}
