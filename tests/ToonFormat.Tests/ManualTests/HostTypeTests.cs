using System.Text.Json;

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
    public void Encode_MapsEveryNumericTypeToANumber()
    {
        var numbers = new
        {
            Byte = (byte)1,
            SByte = (sbyte)-2,
            Short = (short)-3,
            UShort = (ushort)4,
            UInt = 5u,
            ULong = ulong.MaxValue,
            Float = 0.1f,
            Decimal = 1.50m,
        };

        Assert.Equal("""
            Byte: 1
            SByte: -2
            Short: -3
            UShort: 4
            UInt: 5
            ULong: 18446744073709552000
            Float: 0.1
            Decimal: 1.5
            """, ToonEncoder.Encode(numbers));
    }

    [Fact]
    public void Encode_ReadsJsonNodesAndElements()
    {
        const string toon = "a: 1.5\nb[2]: x,2";

        Assert.Equal(toon, ToonEncoder.Encode(ToonDecoder.Decode(toon)));
        Assert.Equal(toon, ToonEncoder.Encode(JsonDocument.Parse("""{"a":1.5,"b":["x",2]}""").RootElement));
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
