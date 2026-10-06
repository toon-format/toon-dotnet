namespace Toon.Format;

internal static class Constants
{
    public const char ListItemMarker = '-';

    public const string ListItemPrefix = "- ";

    public const char Comma = ',';
    public const char Colon = ':';
    public const char Space = ' ';
    public const char Pipe = '|';
    public const char CommentMarker = '#';

    public const char OpenBracket = '[';
    public const char CloseBracket = ']';
    public const char OpenBrace = '{';
    public const char CloseBrace = '}';

    public const string NullLiteral = "null";
    public const string TrueLiteral = "true";
    public const string FalseLiteral = "false";

    public const char Backslash = '\\';
    public const char DoubleQuote = '"';
    public const char Newline = '\n';
    public const char CarriageReturn = '\r';
    public const char Tab = '\t';
    public const char ByteOrderMark = '\uFEFF';

    public const char DefaultDelimiter = Comma;

    public static char ToDelimiterChar(ToonDelimiter delimiter) => delimiter switch
    {
        ToonDelimiter.Comma => Comma,
        ToonDelimiter.Tab => Tab,
        ToonDelimiter.Pipe => Pipe,
        _ => Comma
    };
}

/// <summary>
/// Delimiter between the values of inline arrays and tabular rows.
/// </summary>
public enum ToonDelimiter
{
    /// <summary>Comma ,</summary>
    Comma,

    /// <summary>Tab \t</summary>
    Tab,

    /// <summary>Pipe |</summary>
    Pipe
}
