#nullable enable
using Toon.Format;

namespace Toon.Format;

/// <summary>
/// Options for encoding data to TOON format.
/// </summary>
public class ToonEncodeOptions
{
    /// <summary>
    /// Number of spaces per indentation level.
    /// </summary>
    /// <remarks>Default is 2</remarks>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    /// Delimiter to use for tabular array rows and inline primitive arrays.
    /// Default is comma (,).
    /// </summary>
    /// <remarks>Default is <see cref="ToonDelimiter.COMMA"/></remarks>
    public ToonDelimiter Delimiter { get; set; } = Constants.DEFAULT_DELIMITER_ENUM;
}
