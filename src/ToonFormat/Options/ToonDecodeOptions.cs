#nullable enable
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
    /// Controls path expansion for dotted keys.
    /// "off" (default): Dotted keys are treated as literal keys.
    /// "safe": Expand eligible dotted keys into nested objects.
    /// </summary>
    public ToonFormat.ToonPathExpansion ExpandPaths { get; set; } = ToonFormat.ToonPathExpansion.Off;
}
