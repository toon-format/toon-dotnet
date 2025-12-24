using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Toon.Format.SpecGenerator.Types;
using Toon.Format.SpecGenerator.Extensions;

namespace Toon.Format.SpecGenerator;

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
        writer.NewLine = "\n"; // Use Unix line endings for cross-platform compatibility

        WriteHeader(writer);
        WriteLine(writer);
        WriteLine(writer);

        WriteUsings(writer);
        WriteLine(writer);
        WriteLine(writer);

        WriteNamespace(writer, Fixture.Category);
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
        return new Regex(@"[\(_\-/\:\)=,+]").Replace(input, "")!;
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
                Write(writer, NormalizeLineEndings(encodeTestCase.Expected));
                WriteLine(writer);
                WriteLine(writer, "\"\"\";");

                break;

            case DecodeTestCase decodeTestCase:

                WriteLineIndented(writer, "var input =");
                WriteLine(writer, "\"\"\"");
                Write(writer, NormalizeLineEndings(decodeTestCase.Input));
                WriteLine(writer);
                WriteLine(writer, "\"\"\";");

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
                var hasEncodeOptions = encodeTestCase.Options != null;
                if (hasEncodeOptions)
                {
                    WriteLineIndented(writer, "var options = new ToonEncodeOptions");
                    WriteLineIndented(writer, "{");
                    Indent();

                    if (encodeTestCase.Options?.Delimiter != null)
                        WriteLineIndented(writer, $"Delimiter = {GetToonDelimiterEnumFromChar(encodeTestCase.Options.Delimiter)},");

                    if (encodeTestCase.Options?.Indent != null)
                        WriteLineIndented(writer, $"Indent = {encodeTestCase.Options.Indent},");

                    if (encodeTestCase.Options?.KeyFolding != null)
                        WriteLineIndented(writer, $"KeyFolding = {GetToonKeyFoldingEnumFromString(encodeTestCase.Options.KeyFolding)},");

                    if (encodeTestCase.Options?.FlattenDepth != null)
                        WriteLineIndented(writer, $"FlattenDepth = {encodeTestCase.Options.FlattenDepth},");

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
                var hasDecodeOptions = decodeTestCase.Options != null;
                if (hasDecodeOptions)
                {
                    WriteLineIndented(writer, "var options = new ToonDecodeOptions");
                    WriteLineIndented(writer, "{");
                    Indent();

                    WriteLineIndented(writer, $"Indent = {decodeTestCase.Options?.Indent ?? 2},");
                    WriteLineIndented(writer, $"Strict = {(decodeTestCase.Options?.Strict ?? true).ToString().ToLower()},");

                    if (decodeTestCase.Options?.ExpandPaths != null)
                        WriteLineIndented(writer, $"ExpandPaths = {GetToonPathExpansionEnumFromString(decodeTestCase.Options.ExpandPaths)}");


                    Unindent();
                    WriteLineIndented(writer, "};");

                    WriteLine(writer);
                }

                if (decodeTestCase.ShouldError)
                {
                    // Determine which exception type to expect based on the options
                    var isPathExpansionError = decodeTestCase.Options?.ExpandPaths != null;
                    var exceptionType = isPathExpansionError ? "ToonPathExpansionException" : "ToonFormatException";

                    if (hasDecodeOptions)
                    {
                        WriteLineIndented(writer, $"Assert.Throws<{exceptionType}>(() => ToonDecoder.Decode(input, options));");
                    }
                    else
                    {
                        WriteLineIndented(writer, $"Assert.Throws<{exceptionType}>(() => ToonDecoder.Decode(input));");
                    }
                }
                else
                {
                    var valueAsRawString = decodeTestCase.Expected?.ToString();
                    var isNumeric = decodeTestCase.Expected?.GetValueKind() == JsonValueKind.Number;
                    var hasEmptyRawString = valueAsRawString == string.Empty;
                    var value = hasEmptyRawString || isNumeric ? valueAsRawString : decodeTestCase.Expected?.ToJsonString() ?? "null";

                    WriteIndented(writer, "var result = ToonDecoder.Decode");
                    if (isNumeric)
                    {
                        if (decodeTestCase.Expected is JsonValue jsonValue)
                        {
                            if (jsonValue.TryGetValue<double>(out var doubleValue))
                            {
                                Write(writer, "<double>");
                            }
                            else if (jsonValue.TryGetValue<int>(out var intValue))
                            {
                                Write(writer, "<int>");
                            }
                            else if (jsonValue.TryGetValue<long>(out var longValue))
                            {
                                Write(writer, "<long>");
                            }
                        }
                    }
                    Write(writer, "(input");
                    if (hasDecodeOptions)
                    {
                        Write(writer, ", options");
                    }
                    WriteLine(writer, ");");

                    WriteLine(writer);

                    if (isNumeric)
                    {
                        WriteLineIndented(writer, $"var expected = {value};");

                        WriteLine(writer);
                        WriteLineIndented(writer, "Assert.Equal(result, expected);");
                    }
                    else
                    {
                        if (hasEmptyRawString)
                        {
                            WriteLineIndented(writer, $"var expected = string.Empty;");
                        }
                        else
                        {
                            WriteLineIndented(writer, $"var expected = JsonNode.Parse(\"\"\"\n{value}\n\"\"\");");
                        }

                        WriteLine(writer);
                        WriteLineIndented(writer, $"Assert.True(JsonNode.DeepEquals(result, expected));");
                    }
                }
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

    private static string GetToonKeyFoldingEnumFromString(string? keyFoldingOption)
    {
        return keyFoldingOption switch
        {
            "off" => "ToonKeyFolding.Off",
            "safe" => "ToonKeyFolding.Safe",
            _ => "ToonKeyFolding.Off"
        };
    }

    private static string GetToonPathExpansionEnumFromString(string? expandPathsOption)
    {
        return expandPathsOption switch
        {
            "off" => "ToonPathExpansion.Off",
            "safe" => "ToonPathExpansion.Safe",
            _ => "ToonPathExpansion.Safe"
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

    private void WriteNamespace(StreamWriter writer, string category)
    {
        WriteLine(writer, $"namespace Toon.Format.Tests.{category.ToPascalCase()};");
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

    /// <summary>
    /// Normalizes line endings to Unix format (LF) for cross-platform compatibility.
    /// </summary>
    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n");
    }
}
