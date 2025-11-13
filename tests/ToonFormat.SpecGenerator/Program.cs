
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using ToonFormat.SpecGenerator.Extensions;
using ToonFormat.SpecGenerator.Types;

namespace ToonFormat.SpecGenerator;

public static class Program
{
    public static void Main(string[] args)
    {
        var outputPath = Path.GetFullPath(
            Path.Combine(Environment.GetEnvironmentVariable("PWD") ?? Environment.CurrentDirectory, "./tests/ToonFormat.Tests"));

        SpecGenerator.GenerateSpecs(outputPath);
    }
}

public class SpecGenerator
{
    public static void GenerateSpecs(string outputDir)
    {
        var toonSpecDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Console.WriteLine($"Cloning Toon Format spec repository to {toonSpecDir}");

        try
        {
            GitTool.CloneRepository("https://github.com/toon-format/spec.git", toonSpecDir, 1);

            GenerateEncodeFixtures(toonSpecDir, Path.Combine(outputDir, "Encode"));
        }
        finally
        {
            // delete folder recursively
            TryDeleteDirectory(toonSpecDir);
        }

        Console.WriteLine("Spec generation completed.");
    }

    private static void GenerateEncodeFixtures(string specDir, string outputDir)
    {
        var encodeFixtures = LoadEncodeFixtures(specDir);

        foreach (var fixture in encodeFixtures)
        {
            // Process each encode fixture as needed
            var writer = new FixtureWriter<EncodeTestCase, JsonNode, string>(fixture, outputDir);

            writer.WriteFile();

            // break; ; // TODO: Remove this break to process all fixtures
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
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    private static string FixtureNameToCSharpFileName(string fixtureName)
    {
        return Path.ChangeExtension(fixtureName.ToPascalCase(), ".cs");
    }
}

public class FixtureWriter<TTestCase, TIn, TOut>(Fixtures<TTestCase, TIn, TOut> fixture, string outputDir)
    where TTestCase : ITestCase<TIn, TOut>
{
    public Fixtures<TTestCase, TIn, TOut> Fixture { get; } = fixture;
    public string OutputDir { get; } = outputDir;

    private int indentLevel = 0;

    public void WriteFile()
    {
        var outputPath = Path.Combine(OutputDir, Fixture.FileName ?? throw new InvalidOperationException("Fixture FileName is not set"));

        Directory.CreateDirectory(OutputDir);

        using var stream = File.OpenWrite(outputPath);
        using var writer = new StreamWriter(stream);

        WriteHeader(writer);
        WriteLine(writer);
        WriteLine(writer);

        WriteUsings(writer);
        WriteLine(writer);
        WriteLine(writer);

        WriteNamespace(writer);
        WriteLine(writer);
        WriteLine(writer);

        WriteLine(writer, "[Trait(\"Category\", \"" + Fixture.Category + "\")]");
        WriteLine(writer, "public class " + Path.GetFileNameWithoutExtension(outputPath));
        WriteLine(writer, "{");

        Indent();

        // Write test methods here
        foreach (var testCase in Fixture.Tests)
        {
            WriteTestMethod(writer, testCase);
        }

        Unindent();
        WriteLine(writer, "}");
    }

    private void WriteTestMethod(StreamWriter writer, TTestCase testCase)
    {
        WriteLineIndented(writer, "[Fact]");
        WriteLineIndented(writer, $"public void {testCase.Name.ToPascalCase()}()");
        WriteLineIndented(writer, "{");

        Indent();

        // Arrange
        WriteLineIndented(writer, "// Arrange");
        switch (testCase.Input)
        {
            case JsonNode jsonNode:
                WriteLineIndented(writer, $"var input = JsonNode.Parse(\"\"\"{jsonNode.ToJsonString()}\"\"\");");
                WriteLineIndented(writer, $"var inputObject = input.Deserialize<object>();");
                break;
            case string str:
                WriteLineIndented(writer, $"var input = @\"{str}\";");
                break;
            default:
                WriteLineIndented(writer, $"var input = /* {typeof(TIn).Name} */; // TODO: Initialize input");
                break;
        }


        WriteLine(writer);

        // Act & Assert
        WriteLineIndented(writer, "// Act & Assert");
        switch (testCase)
        {
            case EncodeTestCase encodeTestCase:
                WriteLineIndented(writer, $"var result = ToonEncoder.Encode(inputObject);");
                WriteLine(writer);
                WriteLineIndented(writer, $"Assert.Equal(@\"{encodeTestCase.Expected}\", result);");
                break;
            case DecodeTestCase decodeTestCase:
                WriteLineIndented(writer, $"var result = ToonDecoder.Decode(input);");
                WriteLineIndented(writer, $"var expected = JsonNode.Parse(\"\"\"{decodeTestCase.Expected.ToJsonString()}\"\"\");");
                WriteLineIndented(writer, $"Assert.Equal(expected, result);");
                break;
            default:
                WriteLineIndented(writer, "// TODO: Implement test logic");
                break;
        }

        Unindent();
        WriteLineIndented(writer, "}");
        WriteLine(writer);
    }

    private void Indent()
    {
        indentLevel++;
    }

    private void Unindent()
    {
        indentLevel--;
    }

    private void WriteLineIndented(StreamWriter writer, string line)
    {
        writer.WriteLine(new string(' ', indentLevel * 4) + line);
    }

    private void WriteHeader(StreamWriter writer)
    {
        WriteLine(writer, "// <auto-generated>");
        WriteLine(writer, "//     This code was generated by ToonFormat.SpecGenerator.");
        WriteLine(writer, "//");
        WriteLine(writer, "//     Changes to this file may cause incorrect behavior and will be lost if");
        WriteLine(writer, "//     the code is regenerated.");
        WriteLine(writer, "// </auto-generated>");
    }

    private void WriteUsings(StreamWriter writer)
    {
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine("using Toon.Format;");
        writer.WriteLine("using Xunit;");
    }

    private void WriteNamespace(StreamWriter writer)
    {
        writer.WriteLine("namespace ToonFormat.Tests;");
    }

    private void WriteLine(StreamWriter writer)
    {
        writer.WriteLine();
    }

    private void WriteLine(StreamWriter writer, string line)
    {
        writer.WriteLine(line);
    }
}

public static class SpecSerializer
{
    public static T Deserialize<T>(string input)
    {
        return JsonSerializer.Deserialize<T>(input, jsonSerializerOptions) ?? throw new InvalidOperationException("Deserialization resulted in null");
    }

    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, jsonSerializerOptions);
    }

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}


public class GitTool
{
    public static void CloneRepository(string repositoryUrl, string destinationPath, int? depth = null)
    {
        // Implementation for cloning a Git repository
        using var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "git";
        var depthArg = depth.HasValue ? $"--depth {depth.Value}" : string.Empty;
        process.StartInfo.Arguments = $"clone {depthArg} {repositoryUrl} {destinationPath}";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
    }
}