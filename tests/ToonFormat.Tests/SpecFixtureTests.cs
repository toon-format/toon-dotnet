using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Toon.Format.Tests;

/// <summary>
/// Runs every case of the toon-format/spec fixtures from the <c>tests/spec</c> submodule.
/// </summary>
public class SpecFixtureTests
{
    private static readonly string FixtureRoot = Path.Combine(AppContext.BaseDirectory, "fixtures");

    private static readonly JsonElement NoOptions = JsonDocument.Parse("{}").RootElement;

    public static IEnumerable<object[]> Cases() =>
        from path in Directory.GetFiles(FixtureRoot, "*.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal)
        let file = $"{Path.GetFileName(Path.GetDirectoryName(path))}/{Path.GetFileName(path)}"
        from testCase in JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("tests").EnumerateArray()
        select new object[] { file, testCase.GetProperty("name").GetString()! };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Fixture(string file, string name)
    {
        var testCase = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixtureRoot, file))).RootElement
            .GetProperty("tests").EnumerateArray()
            .First(candidate => candidate.GetProperty("name").GetString() == name);

        Run(file, testCase);
    }

    private static void Run(string file, JsonElement testCase)
    {
        var options = testCase.TryGetProperty("options", out var fixtureOptions) ? fixtureOptions : NoOptions;
        var shouldError = testCase.TryGetProperty("shouldError", out var shouldErrorValue) && shouldErrorValue.GetBoolean();

        if (file.StartsWith("encode/"))
        {
            var encodeOptions = new ToonEncodeOptions();
            if (options.TryGetProperty("indentSize", out var indentSize))
                encodeOptions.Indent = indentSize.GetInt32();
            if (options.TryGetProperty("delimiter", out var delimiter))
            {
                encodeOptions.Delimiter = delimiter.GetString() switch
                {
                    "\t" => ToonDelimiter.TAB,
                    "|" => ToonDelimiter.PIPE,
                    _ => ToonDelimiter.COMMA,
                };
            }

            var input = ToClr(testCase.GetProperty("input"));
            if (shouldError)
            {
                Assert.ThrowsAny<ToonFormatException>(() => ToonEncoder.Encode(input, encodeOptions));
                return;
            }

            Assert.Equal(testCase.GetProperty("expected").GetString(), ToonEncoder.Encode(input, encodeOptions));
            return;
        }

        var decodeOptions = new ToonDecodeOptions();
        if (options.TryGetProperty("indentSize", out var decodeIndentSize))
            decodeOptions.Indent = decodeIndentSize.GetInt32();
        if (options.TryGetProperty("strict", out var strict))
            decodeOptions.Strict = strict.GetBoolean();

        var toon = testCase.GetProperty("input").GetString()!;
        if (shouldError)
        {
            Assert.ThrowsAny<ToonFormatException>(() => ToonDecoder.Decode(toon, decodeOptions));
            return;
        }

        var expected = JsonNode.Parse(testCase.GetProperty("expected").GetRawText());
        Assert.Equal(Canonical(expected), Canonical(ToonDecoder.Decode(toon, decodeOptions)));
    }

    /// <summary>
    /// Maps fixture input to the dictionaries, lists, and primitives a .NET caller hands the encoder.
    /// </summary>
    private static object? ToClr(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name, property => ToClr(property.Value)),
        JsonValueKind.Array => element.EnumerateArray().Select(ToClr).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var integer) ? integer : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };

    /// <summary>
    /// JSON text with every number re-printed as a double, so key order and strings must match
    /// exactly while numbers compare by value (spec §2).
    /// </summary>
    private static string Canonical(JsonNode? node) => node switch
    {
        null => "null",
        JsonObject obj => "{" + string.Join(",", obj.Select(property => JsonSerializer.Serialize(property.Key) + ":" + Canonical(property.Value))) + "}",
        JsonArray array => "[" + string.Join(",", array.Select(Canonical)) + "]",
        JsonValue value when value.GetValueKind() == JsonValueKind.Number =>
            double.Parse(value.ToJsonString(), CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture),
        _ => node.ToJsonString(),
    };
}
