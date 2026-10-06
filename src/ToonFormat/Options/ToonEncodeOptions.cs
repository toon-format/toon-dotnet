namespace Toon.Format;

/// <summary>
/// Options for <see cref="ToonEncoder"/>.
/// </summary>
public class ToonEncodeOptions
{
    /// <summary>
    /// Spaces per indentation level, at least 1. Default is 2.
    /// </summary>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    /// Delimiter between inline array values and tabular row cells. Default is <see cref="ToonDelimiter.COMMA"/>.
    /// </summary>
    public ToonDelimiter Delimiter { get; set; } = ToonDelimiter.COMMA;
}
