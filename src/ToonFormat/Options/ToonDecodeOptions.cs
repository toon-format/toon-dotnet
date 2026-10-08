namespace Toon.Format;

/// <summary>
/// Options for <see cref="ToonDecoder"/>.
/// </summary>
public class ToonDecodeOptions
{
    /// <summary>
    /// Spaces per indentation level, at least 1. Default is 2.
    /// </summary>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    /// Whether to throw on count mismatches, duplicate keys, tab or misaligned indentation, blank lines
    /// inside an array or keyed tabular object, and depth jumps. When false, decoding recovers from these
    /// five – the last duplicate key wins – and still throws on any other invalid input. Default is true.
    /// </summary>
    public bool Strict { get; set; } = true;
}
