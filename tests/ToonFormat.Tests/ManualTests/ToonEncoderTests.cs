using System.Text.Json;
using System.Text.Json.Nodes;
using Toon.Format;

namespace Toon.Format.Tests;

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

    [Fact]
    public void Encode_ArrayOfPrimitives_Issue7()
    {
        var data = new[]
        {
            new
            {
                ZCHATJID = "18324448539@s.whatsapp.net",
                ZMATCHEDTEXT = "http://www.\\u0138roger.com/anniversary",
                ZINDEX = "0",
                Z_OPT = 2,
                Z_PK = 2,
                ZSENDERJID = "18324448539@s.whatsapp.net",
                ZOWNSTHUMBNAIL = "0",
                ZTYPE = "0",
                Z_ENT = "10",
                ZMESSAGE = "696",
                ZSECTIONID = "2017-11",
                ZDATE = "531829799",
                ZCONTENT1 = "http://www.xn--roger-t5a.com/anniversary"
            }
        };

        var expected = """
                       [1]{ZCHATJID,ZMATCHEDTEXT,ZINDEX,Z_OPT,Z_PK,ZSENDERJID,ZOWNSTHUMBNAIL,ZTYPE,Z_ENT,ZMESSAGE,ZSECTIONID,ZDATE,ZCONTENT1}:
                         18324448539@s.whatsapp.net,"http://www.\\u0138roger.com/anniversary","0",2,2,18324448539@s.whatsapp.net,"0","0","10","696",2017-11,"531829799","http://www.xn--roger-t5a.com/anniversary"
                       """;

        var result = ToonEncoder.Encode(data);

        Assert.Equal(expected, result);
    }
}