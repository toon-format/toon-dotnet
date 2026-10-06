namespace Toon.Format.Tests;

/// <summary>
/// Covers how .NET values map onto the JSON data model, which the spec fixtures can't express.
/// </summary>
public class HostTypeTests
{
    private const string ProjectToon = """
        Name: Insights
        CreatedAt: "2025-11-20T10:32:00.0000000Z"
        Tags[2]: research,growth
        Costs:
          usd: 12500.75
        """;

    [Fact]
    public void Encode_MapsPropertiesDatesListsAndDictionaries()
    {
        var project = new Project
        {
            Name = "Insights",
            CreatedAt = new DateTime(2025, 11, 20, 10, 32, 0, DateTimeKind.Utc),
            Tags = ["research", "growth"],
            Costs = new() { ["usd"] = 12500.75 },
        };

        Assert.Equal(ProjectToon, ToonEncoder.Encode(project));
    }

    [Fact]
    public void DecodeT_DeserializesThroughSystemTextJson()
    {
        var project = ToonDecoder.Decode<Project>(ProjectToon)!;

        Assert.Equal("Insights", project.Name);
        Assert.Equal(new DateTime(2025, 11, 20, 10, 32, 0, DateTimeKind.Utc), project.CreatedAt);
        Assert.Equal(["research", "growth"], project.Tags);
        Assert.Equal(12500.75, project.Costs["usd"]);
    }

    [Fact]
    public void Encode_RejectsUnpairedSurrogates()
    {
        foreach (var text in new[] { "a\uD800b", "\uDC00" })
        {
            Assert.Throws<ToonFormatException>(() => ToonEncoder.Encode(text));
            Assert.Throws<ToonFormatException>(() => ToonEncoder.Encode(new Dictionary<string, int> { [text] = 1 }));
        }
    }

    private sealed class Project
    {
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = [];
        public Dictionary<string, double> Costs { get; set; } = [];
    }
}
