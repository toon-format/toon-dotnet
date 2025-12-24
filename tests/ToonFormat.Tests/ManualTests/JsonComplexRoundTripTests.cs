using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Toon.Format;
using Xunit.Abstractions;

namespace Toon.Format.Tests;

/// <summary>
/// Tests for complex multi-level JSON structures to validate TOON format encoding and decoding.
/// </summary>
public class JsonComplexRoundTripTests
{
    private readonly ITestOutputHelper _output;

    public JsonComplexRoundTripTests(ITestOutputHelper output)
    {
        _output = output;
    }
    private const string ComplexJson = @"{
  ""project"": {
    ""id"": ""PX-4921"",
    ""name"": ""Customer Insights Expansion"",
    ""description"": ""This is a long descriptive text containing more than fifteen words to simulate a realistic business scenario for testing purposes."",
    ""createdAt"": ""2025-11-20T10:32:00Z"",
    ""metadata"": {
      ""owner"": ""john.doe@example.com"",
      ""website"": ""https://example.org/products/insights?ref=test&lang=en"",
      ""tags"": [""analysis"", ""insights"", ""growth"", ""R&D""],
      ""cost"": {
        ""currency"": ""USD"",
        ""amount"": 12500.75
      }
    },
    ""phases"": [
      {
        ""phaseId"": 1,
        ""title"": ""Discovery & Research"",
        ""deadline"": ""2025-12-15"",
        ""status"": ""In Progress"",
        ""details"": {
          ""notes"": ""Team is conducting interviews, market analysis, and reviewing historical performance metrics & competitors."",
          ""specialChars"": ""!@#$%^&*()_+=-{}[]|:;<>,.?/""
        }
      },
      {
        ""phaseId"": 2,
        ""title"": ""Development"",
        ""deadline"": ""2026-01-30"",
        ""budget"": {
          ""currency"": ""EUR"",
          ""amount"": 7800.00
        },
        ""resources"": {
          ""leadDeveloper"": ""alice.smith@example.com"",
          ""repository"": ""https://github.com/example/repo""
        }
      }
    ]
  }
}";

    [Fact]
    public void ComplexJson_RoundTrip_ShouldPreserveKeyFields()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<Root>(ComplexJson, options);

        // Sanity
        Assert.NotNull(root);
        Assert.NotNull(root.project);

        // Act - encode to TOON and decode back
        var toonText = ToonEncoder.Encode(root);
        Assert.NotNull(toonText);
        _output.WriteLine("TOON Encoded Output:");
        _output.WriteLine(toonText);
        _output.WriteLine("---");

        var decoded = ToonDecoder.Decode(toonText);
        Assert.NotNull(decoded);

        // The encoder reflects C# property names (we used lowercase names to match original JSON keys)
        var project = decoded["project"]?.AsObject();
        Assert.NotNull(project);

        // Assert key scalar values
        Assert.Equal("PX-4921", project["id"]?.GetValue<string>());
        Assert.Equal("Customer Insights Expansion", project["name"]?.GetValue<string>());

        // Metadata checks
        var metadata = project["metadata"]?.AsObject();
        Assert.NotNull(metadata);
        Assert.Equal("john.doe@example.com", metadata["owner"]?.GetValue<string>());
        Assert.Equal("https://example.org/products/insights?ref=test&lang=en", metadata["website"]?.GetValue<string>());

        // Tags array validation
        var tags = metadata["tags"]?.AsArray();
        Assert.NotNull(tags);
        Assert.Equal(4, tags.Count);
        Assert.Equal("analysis", tags[0]?.GetValue<string>());
        Assert.Equal("insights", tags[1]?.GetValue<string>());
        Assert.Equal("growth", tags[2]?.GetValue<string>());
        Assert.Equal("R&D", tags[3]?.GetValue<string>());

        var cost = metadata["cost"]?.AsObject();
        Assert.NotNull(cost);
        Assert.Equal("USD", cost["currency"]?.GetValue<string>());
        Assert.Equal(12500.75, cost["amount"]?.GetValue<double>());

        // Phases checks
        var phases = project["phases"]?.AsArray();
        Assert.NotNull(phases);
        Assert.Equal(2, phases.Count);

        var phase1 = phases[0]?.AsObject();
        Assert.NotNull(phase1);
        Assert.Equal(1.0, phase1["phaseId"]?.GetValue<double>());
        var details = phase1["details"]?.AsObject();
        Assert.NotNull(details);
        Assert.Contains("market analysis", details["notes"]?.GetValue<string>() ?? string.Empty);
        Assert.Equal("!@#$%^&*()_+=-{}[]|:;<>,.?/", details["specialChars"]?.GetValue<string>());

        var phase2 = phases[1]?.AsObject();
        Assert.NotNull(phase2);
        Assert.Equal(2.0, phase2["phaseId"]?.GetValue<double>());
        Assert.Equal("Development", phase2["title"]?.GetValue<string>());
        Assert.Equal("2026-01-30", phase2["deadline"]?.GetValue<string>());

        var budget = phase2["budget"]?.AsObject();
        Assert.NotNull(budget);
        Assert.Equal("EUR", budget["currency"]?.GetValue<string>());
        Assert.Equal(7800.0, budget["amount"]?.GetValue<double>());

        var resources = phase2["resources"]?.AsObject();
        Assert.NotNull(resources);
        Assert.Equal("alice.smith@example.com", resources["leadDeveloper"]?.GetValue<string>());
        Assert.Equal("https://github.com/example/repo", resources["repository"]?.GetValue<string>());
    }

    [Fact]
    public void ComplexJson_Encode_ShouldProduceValidToonFormat()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<Root>(ComplexJson, options);
        Assert.NotNull(root);

        // Act
        var toonText = ToonEncoder.Encode(root);

        // Assert - verify TOON format structure
        Assert.NotNull(toonText);
        Assert.Contains("project:", toonText);
        Assert.Contains("id:", toonText);
        Assert.Contains("PX-4921", toonText);
        Assert.Contains("metadata:", toonText);
        Assert.Contains("phases[2]", toonText);

        _output.WriteLine("TOON Output:");
        _output.WriteLine(toonText);
    }

    [Fact]
    public void ComplexJson_SpecialCharacters_ShouldBePreserved()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<Root>(ComplexJson, options);
        Assert.NotNull(root);

        // Act
        var toonText = ToonEncoder.Encode(root);
        var decoded = ToonDecoder.Decode(toonText);

        // Assert - verify special characters in details.specialChars
        Assert.NotNull(decoded);
        var project = decoded["project"]?.AsObject();
        var phases = project?["phases"]?.AsArray();
        var phase1 = phases?[0]?.AsObject();
        var details = phase1?["details"]?.AsObject();

        Assert.NotNull(details);
        var specialChars = details["specialChars"]?.GetValue<string>();
        Assert.Equal("!@#$%^&*()_+=-{}[]|:;<>,.?/", specialChars);
    }

    [Fact]
    public void ComplexJson_DateTime_ShouldBePreservedAsString()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<Root>(ComplexJson, options);
        Assert.NotNull(root);

        // Act
        var toonText = ToonEncoder.Encode(root);
        var decoded = ToonDecoder.Decode(toonText);

        // Assert - verify DateTime is preserved as full ISO 8601 UTC timestamp
        Assert.NotNull(decoded);
        var project = decoded["project"]?.AsObject();
        Assert.NotNull(project);

        var createdAt = project["createdAt"]?.GetValue<string>();
        Assert.NotNull(createdAt);
        // Validate full ISO 8601 UTC timestamp (with or without fractional seconds)
        // .NET DateTime.ToString("O") produces: 2025-11-20T10:32:00.0000000Z
        Assert.StartsWith("2025-11-20T10:32:00", createdAt);
        Assert.EndsWith("Z", createdAt);
    }

    // POCOs with lowercase property names to preserve original JSON keys when encoding via reflection
    public class Root { public Project project { get; set; } = null!; }

    public class Project
    {
        public string id { get; set; } = null!;
        public string name { get; set; } = null!;
        public string description { get; set; } = null!;
        public DateTime createdAt { get; set; }
        public Metadata metadata { get; set; } = null!;
        public List<Phase> phases { get; set; } = new();
    }

    public class Metadata
    {
        public string owner { get; set; } = null!;
        public string website { get; set; } = null!;
        public List<string> tags { get; set; } = new();
        public Cost cost { get; set; } = null!;
    }

    public class Cost { public string currency { get; set; } = null!; public double amount { get; set; } }

    public class Phase
    {
        public int phaseId { get; set; }
        public string title { get; set; } = null!;
        public string deadline { get; set; } = null!;
        public string? status { get; set; }
        public Details? details { get; set; }
        public Budget? budget { get; set; }
        public Resources? resources { get; set; }
    }

    public class Details { public string notes { get; set; } = null!; public string specialChars { get; set; } = null!; }

    public class Budget { public string currency { get; set; } = null!; public double amount { get; set; } }

    public class Resources { public string leadDeveloper { get; set; } = null!; public string repository { get; set; } = null!; }
}
