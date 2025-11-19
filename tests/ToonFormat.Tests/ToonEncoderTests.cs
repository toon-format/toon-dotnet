using System.Text.Json;
using System.Text.Json.Nodes;
using Toon.Format;

namespace ToonFormat.Tests;

/// <summary>
/// Tests for encoding data to TOON format.
/// </summary>
public class ToonEncoderTests
{
    [Fact]
    public void Encode_SimpleObject_ReturnsValidToon()
    {
        // Arrange
        var data = new { name = "Alice", age = 30 };

        // Act
        var result = ToonEncoder.Encode(data);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("name:", result);
        Assert.Contains("age:", result);
    }

    [Fact]
    public void Encode_PrimitiveTypes_ReturnsValidToon()
    {
        // String
        var stringResult = ToonEncoder.Encode("hello");
        Assert.Equal("hello", stringResult);

        // Number
        var numberResult = ToonEncoder.Encode(42);
        Assert.Equal("42", numberResult);

        // Boolean
        var boolResult = ToonEncoder.Encode(true);
        Assert.Equal("true", boolResult);

        // Null
        var nullResult = ToonEncoder.Encode(null);
        Assert.Equal("null", nullResult);
    }

    [Fact]
    public void Encode_Array_ReturnsValidToon()
    {
        // Arrange
        var data = new { numbers = new[] { 1, 2, 3, 4, 5 } };

        // Act
        var result = ToonEncoder.Encode(data);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("numbers[", result);
    }

    [Fact]
    public void Encode_TabularArray_ReturnsValidToon()
    {
        // Arrange
        var employees = new[]
        {
            new { id = 1, name = "Alice", salary = 50000 },
            new { id = 2, name = "Bob", salary = 60000 },
            new { id = 3, name = "Charlie", salary = 55000 }
        };
        var data = new { employees };

        // Act
        var result = ToonEncoder.Encode(data);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("employees[", result);
        Assert.Contains("id", result);
        Assert.Contains("name", result);
        Assert.Contains("salary", result);
    }

    [Fact]
    public void Encode_WithCustomIndent_UsesCorrectIndentation()
    {
        // Arrange
        var data = new { outer = new { inner = "value" } };
        var options = new ToonEncodeOptions { Indent = 4 };

        // Act
        var result = ToonEncoder.Encode(data, options);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("outer:", result);
    }

    [Fact]
    public void Encode_WithCustomDelimiter_UsesCorrectDelimiter()
    {
        // Arrange
        var data = new { numbers = new[] { 1, 2, 3 } };
        var options = new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB };

        // Act
        var result = ToonEncoder.Encode(data, options);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("numbers[", result);
    }

    [Fact]
    public void Encode_WithLengthMarker_IncludesHashSymbol()
    {
        // Arrange
        var data = new { items = new[] { 1, 2, 3 } };
        var options = new ToonEncodeOptions { LengthMarker = true };

        // Act
        var result = ToonEncoder.Encode(data, options);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("[#", result);
    }

    [Fact]
    public void Encode_NestedStructures_ReturnsValidToon()
    {
        // Arrange
        var data = new
        {
            user = new
            {
                name = "Alice",
                address = new
                {
                    city = "New York",
                    zip = "10001"
                }
            }
        };

        // Act
        var result = ToonEncoder.Encode(data);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("user:", result);
        Assert.Contains("address:", result);
    }
}
