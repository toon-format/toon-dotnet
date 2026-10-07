using System.Text.Json.Serialization;

namespace Toon.Format.Tests;

/// <summary>
/// Covers how .NET values map onto the JSON data model, which the spec fixtures can't express.
/// </summary>
public class HostTypeTests
{
    private const string ProjectToon = """
        name: Insights
        CreatedAt: "2025-11-20T10:32:00Z"
        Weekday: 4
        Tags[2]: research,growth
        Costs:
          usd: 12500.75
        """;

    [Fact]
    public void Encode_SerializesObjectsThroughSystemTextJson()
    {
        var project = new Project
        {
            Name = "Insights",
            CreatedAt = new DateTime(2025, 11, 20, 10, 32, 0, DateTimeKind.Utc),
            Weekday = DayOfWeek.Thursday,
            Secret = "hidden",
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
        Assert.Equal(DayOfWeek.Thursday, project.Weekday);
        Assert.Equal(["research", "growth"], project.Tags);
        Assert.Equal(12500.75, project.Costs["usd"]);
    }

    private sealed class Project
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DayOfWeek Weekday { get; set; }
        [JsonIgnore]
        public string Secret { get; set; } = "";
        public List<string> Tags { get; set; } = [];
        public Dictionary<string, double> Costs { get; set; } = [];
    }
}
