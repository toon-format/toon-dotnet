using System.Text.Json.Nodes;
using ToonFormat.SpecGenerator.Extensions;
using ToonFormat.SpecGenerator.Types;
using ToonFormat.SpecGenerator.Util;

namespace ToonFormat.SpecGenerator;

internal class SpecGenerator
{
    public static void GenerateSpecs(SpecGeneratorOptions options)
    {
        var toonSpecDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Console.WriteLine($"Cloning Toon Format spec repository to {toonSpecDir}");

        try
        {
            GitTool.CloneRepository(options.SpecRepoUrl, toonSpecDir, branch: options.Branch, depth: 1);

            var testsToIgnore = GenerateTestsToIgnore(options.AbsoluteSpecIgnorePath);

            GenerateEncodeFixtures(
                toonSpecDir,
                Path.Combine(options.AbsoluteOutputPath, "Encode"),
                testsToIgnore
            );
        }
        finally
        {
            // delete folder recursively
            TryDeleteDirectory(toonSpecDir);
        }

        Console.WriteLine("Spec generation completed.");
    }

    private static void GenerateEncodeFixtures(string specDir, string outputDir, IEnumerable<string> ignores)
    {
        var encodeFixtures = LoadEncodeFixtures(specDir);

        foreach (var fixture in encodeFixtures)
        {
            fixture.Tests = fixture.Tests.Where(t => !ignores.Contains(t.Name));

            // Process each encode fixture as needed
            var writer = new FixtureWriter<EncodeTestCase, JsonNode, string>(fixture, outputDir);

            writer.WriteFile();
        }
    }

    private static IEnumerable<Fixtures<EncodeTestCase, JsonNode, string>> LoadEncodeFixtures(string specDir)
    {
        return LoadFixtures<EncodeTestCase, JsonNode, string>(specDir, "encode");
    }

    private static IEnumerable<Fixtures<DecodeTestCase, string, JsonNode>> LoadDecodeFixtures(string specDir)
    {
        return LoadFixtures<DecodeTestCase, string, JsonNode>(specDir, "decode");
    }

    private static IEnumerable<string> GenerateTestsToIgnore(string specIgnorePath)
    {
        const string specIgnoreFileName = ".specignore";
        var specIgnoreFileAbsolutePath = !specIgnorePath.EndsWith(specIgnoreFileName) ?
                                            Path.Combine(specIgnorePath, specIgnoreFileName) : specIgnorePath;

        if (!File.Exists(specIgnoreFileAbsolutePath))
            return Array.Empty<string>();

        var testNames = File.ReadAllLines(specIgnoreFileAbsolutePath);

        return new HashSet<string>(testNames, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<Fixtures<TTestCase, TIn, TOut>> LoadFixtures<TTestCase, TIn, TOut>(string specDir, string testType)
        where TTestCase : ITestCase<TIn, TOut>
    {
        var fixturesPath = Path.Combine(specDir, "tests", "fixtures", testType);

        foreach (var testFixture in Directory.GetFiles(fixturesPath, "*.json"))
        {
            var fixtureFileName = Path.GetFileName(testFixture);

            var fixtureContent = File.ReadAllText(testFixture);
            var fixture = SpecSerializer.Deserialize<Fixtures<TTestCase, TIn, TOut>>(fixtureContent) ?? throw new InvalidOperationException($"Failed to deserialize fixture file: {testFixture}");

            fixture.FileName = FixtureNameToCSharpFileName(fixtureFileName);

            yield return fixture;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        int i = 0;
        do
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);

                    break;
                }
                catch (Exception) { i++; }
            }
        }
        while (i < 3);
    }

    private static string FixtureNameToCSharpFileName(string fixtureName)
    {
        return Path.ChangeExtension(fixtureName.ToPascalCase(), ".cs");
    }
}
