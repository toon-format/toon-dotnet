namespace ToonFormat.SpecGenerator.Types;

public record Fixtures<TTestCase, TIn, TOut>
    where TTestCase : ITestCase<TIn, TOut>
{
    public required string Version { get; init; }
    public required string Category { get; init; } // "encode" | "decode"
    public required string Description { get; init; }
    public required IEnumerable<TTestCase> Tests { get; set; }
    public string? FileName { get; set; }
}

