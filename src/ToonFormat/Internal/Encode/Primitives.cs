using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Toon.Format.Internal.Shared;

namespace Toon.Format.Internal.Encode;

/// <summary>
/// Primitive value encoding, key encoding, and header formatting utilities.
/// </summary>
internal static class Primitives
{
    /// <summary>
    /// Formats a double with its shortest round-trip digits, in plain decimal form for 1e-6 ≤ |n| &lt; 1e21
    /// and in JSON exponent form (<c>1e-7</c>, <c>1e+21</c>) outside that range; -0 becomes 0.
    /// </summary>
    private static string FormatNumber(double value)
    {
        if (value == 0)
            return "0";

        var roundTrip = value.ToString("R", CultureInfo.InvariantCulture);
        var exponentIndex = roundTrip.IndexOf('E');
        var mantissa = (exponentIndex < 0 ? roundTrip : roundTrip.Substring(0, exponentIndex)).TrimStart('-');
        var exponent = exponentIndex < 0 ? 0 : int.Parse(roundTrip.Substring(exponentIndex + 1), CultureInfo.InvariantCulture);

        var pointIndex = mantissa.IndexOf('.');
        var digits = mantissa.Replace(".", "");
        var decimalPoint = (pointIndex < 0 ? mantissa.Length : pointIndex) + exponent;
        var significant = digits.TrimStart('0');
        decimalPoint -= digits.Length - significant.Length;
        digits = significant.TrimEnd('0');

        var abs = Math.Abs(value);
        string formatted;
        if (abs < 1e-6 || abs >= 1e21)
        {
            var scientificExponent = decimalPoint - 1;
            formatted = (digits.Length > 1 ? $"{digits[0]}.{digits.Substring(1)}" : digits)
                + (scientificExponent < 0 ? "e-" : "e+") + Math.Abs(scientificExponent).ToString(CultureInfo.InvariantCulture);
        }
        else if (decimalPoint <= 0)
            formatted = "0." + new string('0', -decimalPoint) + digits;
        else if (decimalPoint >= digits.Length)
            formatted = digits + new string('0', decimalPoint - digits.Length);
        else
            formatted = digits.Substring(0, decimalPoint) + "." + digits.Substring(decimalPoint);

        return value < 0 ? "-" + formatted : formatted;
    }

    #region Primitive encoding

    /// <summary>
    /// Encodes a primitive JSON value (null, boolean, number, or string) to its TOON representation.
    /// </summary>
    public static string EncodePrimitive(JsonNode? value, char delimiter)
    {
        if (value == null)
            return Constants.NULL_LITERAL;

        var jsonValue = value.AsValue();

        if (jsonValue.TryGetValue<bool>(out var boolVal))
            return boolVal ? Constants.TRUE_LITERAL : Constants.FALSE_LITERAL;

        if (jsonValue.TryGetValue<long>(out var longVal))
            return longVal.ToString(CultureInfo.InvariantCulture);

        if (jsonValue.TryGetValue<double>(out var doubleVal))
            return FormatNumber(doubleVal);

        return EncodeStringLiteral(jsonValue.GetValue<string>(), delimiter);
    }

    /// <summary>
    /// Encodes a string literal, adding quotes if necessary.
    /// </summary>
    public static string EncodeStringLiteral(string value, char delimiter) =>
        ValidationShared.IsSafeUnquoted(value, delimiter) ? value : Quote(value);

    public static string Quote(string value) => $"{Constants.DOUBLE_QUOTE}{StringUtils.EscapeString(value)}{Constants.DOUBLE_QUOTE}";

    #endregion

    #region Key encoding

    /// <summary>
    /// Encodes a key, adding quotes if necessary.
    /// </summary>
    public static string EncodeKey(string key) => ValidationShared.IsValidUnquotedKey(key) ? key : Quote(key);

    #endregion

    #region Value joining

    public static string EncodeAndJoinPrimitives(IEnumerable<JsonNode?> values, char delimiter) =>
        string.Join(delimiter.ToString(), values.Select(value => EncodePrimitive(value, delimiter)));

    #endregion

    #region Header formatters

    /// <summary>
    /// Formats a header such as <c>[3]:</c>, <c>items[5|]:</c>, <c>users[2]{id,name{first,last}}:</c>,
    /// or the keyed <c>servers[2:]{host,port}:</c>.
    /// </summary>
    public static string FormatHeader(int length, string? key, IReadOnlyList<FieldNode>? fields, char delimiter, bool keyed = false)
    {
        var header = new StringBuilder();

        if (key != null)
            header.Append(EncodeKey(key));

        header.Append(Constants.OPEN_BRACKET).Append(length);
        if (keyed)
            header.Append(Constants.COLON);
        if (delimiter != Constants.DEFAULT_DELIMITER_CHAR)
            header.Append(delimiter);
        header.Append(Constants.CLOSE_BRACKET);

        if (fields != null)
            AppendFieldList(header, fields, delimiter);

        return header.Append(Constants.COLON).ToString();
    }

    private static void AppendFieldList(StringBuilder header, IReadOnlyList<FieldNode> fields, char delimiter)
    {
        header.Append(Constants.OPEN_BRACE);
        for (var i = 0; i < fields.Count; i++)
        {
            if (i > 0)
                header.Append(delimiter);
            header.Append(EncodeKey(fields[i].Name));
            if (fields[i].Children != null)
                AppendFieldList(header, fields[i].Children!, delimiter);
        }
        header.Append(Constants.CLOSE_BRACE);
    }

    #endregion
}
