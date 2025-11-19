#nullable enable
using ToonFormat;

namespace Toon.Format;

/// <summary>
/// Options for encoding data to TOON format.
/// </summary>
public class ToonEncodeOptions
{
    /// <summary>
    /// Number of spaces per indentation level.
    /// Default is 2.
    /// </summary>
    public int Indent { get; set; } = 2;

    /// <summary>
    /// Delimiter to use for tabular array rows and inline primitive arrays.
    /// Default is comma (,).
    /// </summary>
    public ToonDelimiter Delimiter { get; set; } = Constants.DEFAULT_DELIMITER_ENUM;
}
