namespace ToonFormat.SpecGenerator.Types;

public record TestCaseOptions
{
    public string? Delimiter { get; init; }
    public int? Indent { get; init; }
    public bool? Strict { get; init; }
    public string? KeyFolding { get; init; }
    public int? FlattenDepth { get; init; }
    public string? ExpandPaths { get; init; }
}

