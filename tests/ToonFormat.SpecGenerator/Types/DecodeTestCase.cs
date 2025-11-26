using System.Text.Json.Nodes;

namespace ToonFormat.SpecGenerator.Types;

public record DecodeTestCase : ITestCase<string, JsonNode>
{
    public required string Name { get; init; }
    public required string Input { get; init; }
    public required JsonNode Expected { get; init; }
    public bool ShouldError { get; init; } = false;
    public TestCaseOptions? Options { get; init; }
    public string? SpecSection { get; init; }
    public string? Note { get; init; }
    public string? MinSpecVersion { get; init; }
}

