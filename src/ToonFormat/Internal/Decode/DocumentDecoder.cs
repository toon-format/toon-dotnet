using System.Text.Json.Nodes;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Decode;

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
        if (first == null)
            return new JsonObject();

        if (first.Depth != 0)
            throw OverIndentedLineError(first, 0);

        if (first.Content == EmptyArray)
        {
            _cursor.Next();
            AssertFullyConsumed();
            return new JsonArray();
        }

        var rootHeader = ResolveArrayHeader(first);
        if (rootHeader != null && rootHeader.Key == null)
        {
            _cursor.Next();
            var array = DecodeArrayFromHeader(rootHeader, 0, first);
            AssertFullyConsumed();
            return array;
        }

        _cursor.Next();
        var following = _cursor.Peek();
        if (following == null && !IsKeyValueContent(first.Content))
            return At(first, () => Parser.ParsePrimitiveToken(first.Content));

        if (!IsKeyValueContent(first.Content) && following?.Depth == 0)
            throw ToonFormatException.Syntax("Top-level document must start with a key-value or array-header line", first.LineNumber, sourceLine: first.Raw);

        var root = new JsonObject();
        DecodeKeyValue(first, root, 0);

        for (var line = _cursor.Peek(); line != null; line = _cursor.Peek())
        {
            if (line.Depth != 0)
                throw OverIndentedLineError(line, 0);

            _cursor.Next();
            DecodeKeyValue(line, root, 0);
        }

        return root;
    }

    private static bool IsKeyValueContent(string content) => StringUtils.FindUnquotedChar(content, Constants.Colon) != -1;

    #endregion

    #region Objects

    private void DecodeKeyValue(ParsedLine line, JsonObject target, int baseDepth)
    {
        var content = line.Content;

        var header = ResolveArrayHeader(line);
        if (header?.Key != null)
        {
            AssertNewKey(target, header.Key, line);
            target[header.Key] = DecodeArrayFromHeader(header, baseDepth, line);
            return;
        }

        if (header != null)
            throw header.Keyed ? KeylessKeyedHeaderError(line) : ToonFormatException.Syntax("Keyless array header is only valid at the document root or as a list item", line.LineNumber, sourceLine: line.Raw);

        var keyToken = At(line, () => Parser.ParseKeyToken(content));
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

            if (line.Depth != fieldDepth)
                throw OverIndentedLineError(line, fieldDepth.Value);

            _cursor.Next();
            DecodeKeyValue(line, obj, fieldDepth.Value);
        }

        return obj;
    }

    #endregion

    #region Arrays

    private JsonNode DecodeArrayFromHeader(ArrayHeaderInfo header, int baseDepth, ParsedLine headerLine)
    {
        if (header.Keyed)
            return DecodeKeyedObject(header, baseDepth, headerLine);

        if (header.InlineValues != null)
            return DecodeInlinePrimitiveArray(header, headerLine);

        if (header.Fields != null)
            return DecodeTabularArray(header, baseDepth, headerLine);

        return DecodeListArray(header, baseDepth, headerLine);
    }

    private JsonArray DecodeInlinePrimitiveArray(ArrayHeaderInfo header, ParsedLine headerLine)
    {
        var values = ParseCells(headerLine, header.InlineValues!, header.Delimiter);
        if (_strict)
            Validation.AssertExpectedCount(values.Count, header.Length, "inline-form values", headerLine);
        return new JsonArray(values.ToArray());
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
                throw OverIndentedLineError(line, rowDepth);

            if (!Validation.IsDataRow(line.Content, header.Delimiter))
                break;

            _cursor.Next();
            startLine ??= line.LineNumber;
            lastRowLine = line;

            var cells = ParseCells(line, line.Content, header.Delimiter);
            Validation.AssertExpectedCount(cells.Count, Parser.CountLeafFields(header.Fields!), "tabular row values", line);

            var cellIndex = 0;
            rows.Add(ObjectFromFields(header.Fields!, cells, ref cellIndex));
        }

        if (_strict)
        {
            Validation.AssertExpectedCount(rows.Count, header.Length, "tabular rows", lastRowLine);

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
                throw OverIndentedLineError(line, itemDepth);

            if (!IsListItem(line.Content))
                break;

            startLine ??= line.LineNumber;
            items.Add(DecodeListItem(itemDepth));
            lastItemLine = _cursor.LastLine!;
        }

        if (_strict)
        {
            Validation.AssertExpectedCount(items.Count, header.Length, "list-form items", lastItemLine);

            if (startLine != null)
                Validation.ValidateNoBlankLinesInRange(startLine.Value, lastItemLine.LineNumber, _cursor.BlankLines, "list-form array");

            Validation.ValidateNoExtraListItems(_cursor.Peek(), itemDepth, header.Length);
        }

        return items;
    }

    /// <summary>
    /// Decodes keyed tabular entry rows (<c>key: cell,cell</c>) into an object of objects. The scope ends only
    /// by dedent or end of input, so every line at entry depth with an unquoted colon is an entry row.
    /// </summary>
    private JsonObject DecodeKeyedObject(ArrayHeaderInfo header, int baseDepth, ParsedLine headerLine)
    {
        var entries = new JsonObject();
        var entryDepth = ScopeContentDepth(baseDepth);
        var leafCount = Parser.CountLeafFields(header.Fields!);
        var lastEntryLine = headerLine;
        int? startLine = null;
        var entryCount = 0;

        for (var line = _cursor.Peek(); line != null && line.Depth > baseDepth; line = _cursor.Peek())
        {
            if (line.Depth != entryDepth)
                throw OverIndentedLineError(line, entryDepth);

            _cursor.Next();
            if (!IsKeyValueContent(line.Content))
                throw ToonFormatException.Syntax("Expected entry row inside keyed tabular object", line.LineNumber, sourceLine: line.Raw);

            startLine ??= line.LineNumber;
            lastEntryLine = line;

            var keyToken = At(line, () => Parser.ParseKeyToken(line.Content));
            AssertNewKey(entries, keyToken.Key, line);

            var cells = ParseCells(line, StringUtils.TrimSpaces(line.Content.Substring(keyToken.End)), header.Delimiter);
            Validation.AssertExpectedCount(cells.Count, leafCount, "keyed entry cells", line);

            var cellIndex = 0;
            entries[keyToken.Key] = ObjectFromFields(header.Fields!, cells, ref cellIndex);
            entryCount++;
        }

        if (_strict)
        {
            Validation.AssertExpectedCount(entryCount, header.Length, "keyed entries", lastEntryLine);

            if (startLine != null)
                Validation.ValidateNoBlankLinesInRange(startLine.Value, lastEntryLine.LineNumber, _cursor.BlankLines, "keyed tabular object");
        }

        return entries;
    }

    private static List<JsonNode?> ParseCells(ParsedLine line, string content, char delimiter) =>
        At(line, () => Parser.ParseDelimitedValues(content, delimiter).Select(Parser.ParsePrimitiveToken).ToList());

    /// <summary>
    /// Assigns a row's cells to the field list depth-first, so each nested field group becomes a nested object.
    /// </summary>
    private static JsonObject ObjectFromFields(List<FieldNode> fields, List<JsonNode?> cells, ref int cellIndex)
    {
        var obj = new JsonObject();
        foreach (var field in fields)
        {
            obj[field.Name] = field.Children != null ? ObjectFromFields(field.Children, cells, ref cellIndex) : cells[cellIndex++];
        }

        return obj;
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
        content.StartsWith(Constants.ListItemPrefix, StringComparison.Ordinal) || content == Constants.ListItemMarker.ToString();

    private JsonNode? DecodeListItem(int itemDepth)
    {
        var line = _cursor.Next()!;
        if (line.Content == Constants.ListItemMarker.ToString())
            return new JsonObject();

        // The scanner trims trailing spaces, so a bare `- ` arrives as the marker alone. Every space
        // after the hyphen goes, so `-   [2]: x` opens a header just as `-   a: 1` opens a field.
        var afterHyphen = line.Content.Substring(Constants.ListItemPrefix.Length).TrimStart(Constants.Space);
        if (afterHyphen == EmptyArray)
            return new JsonArray();

        var itemLine = new ParsedLine { Raw = line.Raw, Content = afterHyphen, Depth = line.Depth, LineNumber = line.LineNumber };

        var header = ResolveArrayHeader(itemLine);
        if (header != null && header.Key == null)
        {
            if (header.Fields != null)
                throw header.Keyed ? KeylessKeyedHeaderError(line) : ToonFormatException.Syntax("Keyless header with a field list is only valid at the document root", line.LineNumber, sourceLine: line.Raw);

            return DecodeArrayFromHeader(header, itemDepth, itemLine);
        }

        if (IsKeyValueContent(afterHyphen))
        {
            var obj = new JsonObject();
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
            if (line.Depth != fieldDepth)
                throw OverIndentedLineError(line, fieldDepth);

            // A hyphen marks a list item only at item depth, so a `- ` line here is a further field.
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

    private static ToonFormatException OverIndentedLineError(ParsedLine line, int contentDepth) =>
        ToonFormatException.Indentation($"Over-indented line: expected depth {contentDepth}, but found {line.Depth}", line.LineNumber, sourceLine: line.Raw);

    // Decoding never silently discards input, so a line after the root form is an error.
    private void AssertFullyConsumed()
    {
        var trailing = _cursor.Peek();
        if (trailing != null)
            throw ToonFormatException.Validation("Unexpected content after the document root", trailing.LineNumber, sourceLine: trailing.Raw);
    }

    /// <summary>
    /// Parses a header line; a grammar failure errors in both modes, a repeated field name in strict mode only.
    /// </summary>
    private ArrayHeaderInfo? ResolveArrayHeader(ParsedLine line)
    {
        string? error = null;
        var header = At(line, () => Parser.ParseArrayHeaderLine(line.Content, out error));
        var violation = error ?? (_strict ? header?.StrictError : null);
        if (violation != null)
            throw ToonFormatException.Syntax(violation, line.LineNumber, sourceLine: line.Raw);

        return header;
    }

    private static ToonFormatException KeylessKeyedHeaderError(ParsedLine line) =>
        ToonFormatException.Syntax("Keyless keyed header is only valid at the document root", line.LineNumber, sourceLine: line.Raw);

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
