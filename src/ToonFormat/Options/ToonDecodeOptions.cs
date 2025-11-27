#nullable enable
using ToonFormat;
namespace Toon.Format;

/// <summary>
/// Options for decoding TOON format strings.
/// </summary>
public class ToonDecodeOptions
{
    /// <summary>
    /// Number of spaces per indentation level.
    /// Default is 2.
    /// </summary>
    public int Indent { get; set; } = 2;

    /// <summary>
    /// When true, enforce strict validation of array lengths and tabular row counts.
    /// Default is true.
    /// </summary>
    public bool Strict { get; set; } = true;

    /// <summary>
    /// Strategy for expanding dotted keys.
    /// Default is Off.
    /// </summary>
    public ExpandPaths ExpandPaths { get; set; } = ExpandPaths.Off;
}
