using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using Toon.Format.SpecGenerator.Types;
using Toon.Format.SpecGenerator.Util;
using Toon.Format.SpecGenerator.Extensions;

namespace Toon.Format.SpecGenerator;

internal class SpecGenerator(ILogger<SpecGenerator> logger)
{
    public void GenerateSpecs(SpecGeneratorOptions options)
    {
        var toonSpecDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        logger.LogInformation("Cloning Toon Format spec repository to {ToonSpecDir}", toonSpecDir);

        try
        {
            // Clean up test directory before generating new files
            CleanTestDirectory(options.AbsoluteOutputPath);

            logger.LogDebug("Cloning repository {RepoUrl} to {CloneDirectory}", options.SpecRepoUrl, toonSpecDir);

            GitTool.CloneRepository(options.SpecRepoUrl, toonSpecDir,
                branch: options.Branch, depth: 1, logger: logger);

            var testsToIgnore = GenerateTestsToIgnore(options.AbsoluteSpecIgnorePath);

            GenerateEncodeFixtures(
                toonSpecDir,
                Path.Combine(options.AbsoluteOutputPath, "Encode"),
                testsToIgnore
            );

            GenerateDecodeFixtures(
                toonSpecDir,
                Path.Combine(options.AbsoluteOutputPath, "Decode"),
                testsToIgnore
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed generating specs");

            return;
        }
        finally
        {
            logger.LogDebug("Removing temp clone directory {CloneDirectory}", toonSpecDir);

            // delete folder recursively
            TryDeleteDirectory(toonSpecDir);
        }

        logger.LogInformation("Spec generation completed.");
    }

    private void CleanTestDirectory(string testDirectory)
    {
        if (!Directory.Exists(testDirectory))
        {
            logger.LogDebug("Test directory {TestDirectory} does not exist, skipping cleanup", testDirectory);
            return;
        }

        logger.LogInformation("Cleaning test directory {TestDirectory}", testDirectory);

        // Delete all subdirectories
        foreach (var dir in Directory.GetDirectories(testDirectory))
        {
            try
            {
                Directory.Delete(dir, true);
                logger.LogDebug("Deleted directory {Directory}", dir);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete directory {Directory}", dir);
            }
        }

        // Delete all files except .csproj files
        foreach (var file in Directory.GetFiles(testDirectory))
        {
            if (Path.GetExtension(file).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Preserving project file {File}", file);
                continue;
            }

            try
            {
                File.Delete(file);
                logger.LogDebug("Deleted file {File}", file);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete file {File}", file);
            }
        }

        logger.LogInformation("Test directory cleanup completed");
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        // Create destination directory if it doesn't exist
        Directory.CreateDirectory(destDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);

            try
            {
                File.Copy(file, destFile, overwrite: true);
                logger.LogDebug("Copied file {SourceFile} to {DestFile}", file, destFile);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to copy file {SourceFile} to {DestFile}", file, destFile);
            }
        }

        // Copy all subdirectories recursively
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var subDirName = Path.GetFileName(subDir);
            var destSubDir = Path.Combine(destDir, subDirName);
            CopyDirectory(subDir, destSubDir);
        }
    }

    private void GenerateEncodeFixtures(string specDir, string outputDir, IEnumerable<string> ignores)
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

    private void GenerateDecodeFixtures(string specDir, string outputDir, IEnumerable<string> ignores)
    {
        var decodeFixtures = LoadDecodeFixtures(specDir);

        foreach (var fixture in decodeFixtures)
        {
            fixture.Tests = fixture.Tests.Where(t => !ignores.Contains(t.Name));

            // Process each decode fixture as needed
            var writer = new FixtureWriter<DecodeTestCase, string, JsonNode>(fixture, outputDir);

            writer.WriteFile();
        }
    }

    private IEnumerable<Fixtures<EncodeTestCase, JsonNode, string>> LoadEncodeFixtures(string specDir)
    {
        return LoadFixtures<EncodeTestCase, JsonNode, string>(specDir, "encode");
    }

    private IEnumerable<Fixtures<DecodeTestCase, string, JsonNode>> LoadDecodeFixtures(string specDir)
    {
        return LoadFixtures<DecodeTestCase, string, JsonNode>(specDir, "decode");
    }

    private IEnumerable<string> GenerateTestsToIgnore(string specIgnorePath)
    {
        const string specIgnoreFileName = ".specignore";
        var specIgnoreFileAbsolutePath = !specIgnorePath.EndsWith(specIgnoreFileName) ?
                                            Path.Combine(specIgnorePath, specIgnoreFileName) : specIgnorePath;

        if (!File.Exists(specIgnoreFileAbsolutePath))
        {
            logger.LogDebug("No spec ignore file found at path {Path}", specIgnoreFileAbsolutePath);

            return Array.Empty<string>();
        }

        // filter comments and empty lines
        var testNames = File.ReadAllLines(specIgnoreFileAbsolutePath)
            .Where(i => !string.IsNullOrWhiteSpace(i) && !i.StartsWith('#'));

        var set = new HashSet<string>(testNames, StringComparer.OrdinalIgnoreCase);

        logger.LogDebug("Found {Count} tests to ignore", set.Count);

        return set;
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
