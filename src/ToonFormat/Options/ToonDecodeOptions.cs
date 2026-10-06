#nullable enable
namespace Toon.Format;

/// <summary>
/// Options for <see cref="ToonDecoder"/>.
/// </summary>
public class ToonDecodeOptions
{
    /// <summary>
    /// Spaces per indentation level. Default is 2.
    /// </summary>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    /// Whether to throw on input that strict mode rejects, such as length mismatches, blank lines
    /// inside arrays, and duplicate keys. Default is true.
    /// </summary>
    public bool Strict { get; set; } = true;
}
