using System.Text.Json.Nodes;

namespace ToonFormat.SpecGenerator.Types;

public record EncodeTestCase : ITestCase<JsonNode, string>
{
    public required string Name { get; init; }
    public required JsonNode Input { get; init; }
    public required string Expected { get; init; }
    public bool ShouldError { get; init; } = false;
    public TestCaseOptions? Options { get; init; }
    public string? SpecSection { get; init; }
    public string? Note { get; init; }
    public string? MinSpecVersion { get; init; }
}

