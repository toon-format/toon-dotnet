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
    /// <see cref="ToonPathExpansion.Off" /> (default): Dotted keys are treated as literal keys.
    /// <see cref="ToonPathExpansion.Safe" />: Expand eligible dotted keys into nested objects.
    /// </summary>
    public ToonPathExpansion ExpandPaths { get; set; } = ToonPathExpansion.Off;
}
