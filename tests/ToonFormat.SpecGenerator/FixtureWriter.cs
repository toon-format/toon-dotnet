using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ToonFormat.SpecGenerator.Extensions;
using ToonFormat.SpecGenerator.Types;

namespace ToonFormat.SpecGenerator;

internal class FixtureWriter<TTestCase, TIn, TOut>(Fixtures<TTestCase, TIn, TOut> fixture, string outputDir)
    where TTestCase : ITestCase<TIn, TOut>
{
    public Fixtures<TTestCase, TIn, TOut> Fixture { get; } = fixture;
    public string OutputDir { get; } = outputDir;

    private int indentLevel = 0;

    public void WriteFile()
    {
        var outputPath = Path.Combine(OutputDir, Fixture.FileName ?? throw new InvalidOperationException("Fixture FileName is not set"));

        Directory.CreateDirectory(OutputDir);

        using var writer = new StreamWriter(outputPath, false);

        WriteHeader(writer);
        WriteLine(writer);
        WriteLine(writer);

        WriteUsings(writer);
        WriteLine(writer);
        WriteLine(writer);

        WriteNamespace(writer);
        WriteLine(writer);
        WriteLine(writer);

        WriteLine(writer, $"[Trait(\"Category\", \"{Fixture.Category}\")]");
        WriteLine(writer, "public class " + FormatClassName(outputPath));
        WriteLine(writer, "{");

        Indent();

        // Write test methods here
        foreach (var testCase in Fixture.Tests)
        {
            WriteTestMethod(writer, testCase);

            //break; // TODO: Remove this
        }

        Unindent();
        WriteLine(writer, "}");
    }

    private string FormatClassName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (fileName == null) return string.Empty;

        return StripIllegalCharacters(fileName);
    }

    private string FormatMethodName(string methodName)
    {
        return StripIllegalCharacters(methodName.ToPascalCase());
    }

    private string StripIllegalCharacters(string input)
    {
        return new Regex(@"[\(_\-/\:\)=]").Replace(input, "")!;
    }

    private void WriteTestMethod(StreamWriter writer, TTestCase testCase)
    {
        WriteLineIndented(writer, "[Fact]");
        WriteLineIndented(writer, $"[Trait(\"Description\", \"{testCase.Name}\")]");
        WriteLineIndented(writer, $"public void {FormatMethodName(testCase.Name)}()");
        WriteLineIndented(writer, "{");

        Indent();

        // Arrange
        WriteLineIndented(writer, "// Arrange");
        switch (testCase)
        {
            case EncodeTestCase encodeTestCase:
                WriteLineIndented(writer, "var input =");
                Indent();
                WriteJsonNodeAsAnonymousType(writer, encodeTestCase.Input);
                Unindent();
                WriteLine(writer);
                WriteLineIndented(writer, "var expected =");
                WriteLine(writer, "\"\"\"");
                Write(writer, encodeTestCase.Expected);
                WriteLine(writer);
                WriteLine(writer, "\"\"\";");
                break;
            case DecodeTestCase decodeTestCase:
                WriteLineIndented(writer, $"var input = @\"{decodeTestCase.Input}\";");
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
                var hasOptions = encodeTestCase.Options != null;
                if (hasOptions)
                {
                    WriteLineIndented(writer, "var options = new ToonEncodeOptions");
                    WriteLineIndented(writer, "{");
                    Indent();

                    WriteLineIndented(writer, $"Delimiter = {GetToonDelimiterEnumFromChar(encodeTestCase.Options?.Delimiter)},");
                    WriteLineIndented(writer, $"Indent = {encodeTestCase.Options?.Indent ?? 2},");

                    Unindent();
                    WriteLineIndented(writer, "};");

                    WriteLine(writer);
                    WriteLineIndented(writer, $"var result = ToonEncoder.Encode(input, options);");
                }
                else
                {
                    WriteLineIndented(writer, $"var result = ToonEncoder.Encode(input);");
                }

                WriteLine(writer);
                WriteLineIndented(writer, $"Assert.Equal(expected, result);");
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

    private static string GetToonDelimiterEnumFromChar(string? delimiter)
    {
        return delimiter switch
        {
            "," => "ToonDelimiter.COMMA",
            "\t" => "ToonDelimiter.TAB",
            "|" => "ToonDelimiter.PIPE",
            _ => "ToonDelimiter.COMMA"
        };
    }

    private void WriteJsonNodeAsAnonymousType(StreamWriter writer, JsonNode node)
    {
        WriteJsonNode(writer, node);

        WriteLineIndented(writer, ";");
    }

    private void WriteJsonNode(StreamWriter writer, JsonNode? node)
    {
        var propertyName = node?.Parent is JsonObject ? node?.GetPropertyName() : null;

        void WriteFunc(string value)
        {
            if (propertyName is not null && node.Parent is not JsonArray)
            {
                Write(writer, value);
            }
            else
            {
                WriteIndented(writer, value);
            }
        }

        if (node is null)
        {
            WriteIndented(writer, "(string)null");
        }
        else if (node is JsonValue nodeValue)
        {
            if (propertyName is not null)
            {
                WriteIndented(writer, $"@{propertyName} = ");
            }

            var kind = nodeValue.GetValueKind();
            if (kind == JsonValueKind.String)
            {
                WriteFunc($"@\"{nodeValue.GetValue<string>().Replace("\"", "\"\"")}\"");
            }
            else
            {
                if (kind == JsonValueKind.True || kind == JsonValueKind.False)
                {
                    WriteFunc($"{nodeValue.GetValue<bool>().ToString().ToLower()}");
                }
                else if (kind == JsonValueKind.Number)
                {
                    var stringValue = nodeValue.ToString();

                    WriteFunc($"{stringValue}");
                }
                else
                {
                    WriteFunc($"{nodeValue.GetValue<object>()}");
                }
            }

            if (propertyName is not null)
            {
                WriteLine(writer, ",");
            }
        }
        else if (node is JsonObject nodeObject)
        {
            if (propertyName is not null)
            {
                WriteLineIndented(writer, $"@{propertyName} =");
            }

            WriteLineIndented(writer, "new");
            WriteLineIndented(writer, "{");
            Indent();

            foreach (var property in nodeObject)
            {
                if (property.Value is null)
                {
                    WriteFunc($"@{property.Key} = (string)null,");
                }
                else
                {
                    WriteJsonNode(writer, property.Value);
                }
            }

            Unindent();
            WriteLineIndented(writer, "}");

            if (propertyName is not null)
            {
                WriteLine(writer, ",");
            }
        }
        else if (node is JsonArray nodeArray)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                WriteIndented(writer, $"@{propertyName} =");
            }

            WriteFunc("new object[] {");

            WriteLineIndented(writer);
            Indent();

            foreach (var item in nodeArray)
            {
                WriteJsonNode(writer, item);

                if (item is JsonValue)
                {
                    WriteLine(writer, ",");
                }
                else
                {
                    WriteLineIndented(writer, ",");
                }
            }

            Unindent();
            WriteLineIndented(writer, "}");

            if (propertyName is not null)
            {
                WriteLine(writer, ",");
            }
        }
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

    private void WriteLineIndented(StreamWriter writer)
    {
        WriteLineIndented(writer, "");
    }

    private void WriteIndented(StreamWriter writer, string content)
    {
        writer.Write(new string(' ', indentLevel * 4) + content);
    }

    private void WriteIndented(StreamWriter writer)
    {
        WriteIndented(writer, "");
    }

    private void WriteHeader(StreamWriter writer)
    {
        WriteLine(writer, "// <auto-generated>"); ;
        WriteLine(writer, "//     This code was generated by ToonFormat.SpecGenerator.");
        WriteLine(writer, "//");
        WriteLine(writer, "//     Changes to this file may cause incorrect behavior and will be lost if");
        WriteLine(writer, "//     the code is regenerated.");
        WriteLine(writer, "// </auto-generated>");
    }

    private void WriteUsings(StreamWriter writer)
    {
        WriteLine(writer, "using System;");
        WriteLine(writer, "using System.Collections.Generic;");
        WriteLine(writer, "using System.Text.Json;");
        WriteLine(writer, "using System.Text.Json.Nodes;");
        WriteLine(writer, "using Toon.Format;");
        WriteLine(writer, "using Xunit;");
    }

    private void WriteNamespace(StreamWriter writer)
    {
        WriteLine(writer, "namespace ToonFormat.Tests;");
    }

    private void WriteLine(StreamWriter writer)
    {
        writer.WriteLine();
    }

    private void WriteLine(StreamWriter writer, string line)
    {
        writer.WriteLine(line);
    }

    private void Write(StreamWriter writer, string contents)
    {
        writer.Write(contents);
    }
}
