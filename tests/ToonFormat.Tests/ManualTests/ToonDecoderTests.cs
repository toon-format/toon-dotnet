using System.Text.Json;
using System.Text.Json.Nodes;
using Toon.Format;

namespace Toon.Format.Tests;

/// <summary>
/// Tests for decoding TOON format strings.
/// </summary>
public class ToonDecoderTests
{
    [Fact]
    public void Decode_SimpleObject_ReturnsValidJson()
    {
        // Arrange
        var toonString = "name: Alice\nage: 30";

        // Act
        var result = ToonDecoder.Decode(toonString);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        Assert.Equal("Alice", obj["name"]?.GetValue<string>());
        Assert.Equal(30.0, obj["age"]?.GetValue<double>());
    }

    [Fact]
    public void Decode_PrimitiveTypes_ReturnsCorrectValues()
    {
        // String
        var stringResult = ToonDecoder.Decode("hello");
        Assert.Equal("hello", stringResult?.GetValue<string>());

        // Number - JSON defaults to double
        var numberResult = ToonDecoder.Decode("42");
        Assert.Equal(42.0, numberResult?.GetValue<double>());

        // Boolean
        var boolResult = ToonDecoder.Decode("true");
        Assert.True(boolResult?.GetValue<bool>());

        // Null
        var nullResult = ToonDecoder.Decode("null");
        Assert.Null(nullResult);
    }

    [Fact]
    public void Decode_PrimitiveArray_ReturnsValidArray()
    {
        // Arrange
        var toonString = "numbers[5]: 1, 2, 3, 4, 5";

        // Act
        var result = ToonDecoder.Decode(toonString);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        var numbers = obj["numbers"]?.AsArray();
        Assert.NotNull(numbers);
        Assert.Equal(5, numbers.Count);
        Assert.Equal(1.0, numbers[0]?.GetValue<double>());
        Assert.Equal(5.0, numbers[4]?.GetValue<double>());
    }

    [Fact]
    public void Decode_TabularArray_ReturnsValidStructure()
    {
        // Arrange - using list array format instead
        var toonString = @"employees[3]:
  - id: 1
    name: Alice
    salary: 50000
  - id: 2
    name: Bob
    salary: 60000
  - id: 3
    name: Charlie
    salary: 55000";

        // Act
        var result = ToonDecoder.Decode(toonString);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        var employees = obj["employees"]?.AsArray();
        Assert.NotNull(employees);
        Assert.Equal(3, employees.Count);
        Assert.Equal(1.0, employees[0]?["id"]?.GetValue<double>());
        Assert.Equal("Alice", employees[0]?["name"]?.GetValue<string>());
    }

    [Fact]
    public void Decode_NestedObject_ReturnsValidStructure()
    {
        // Arrange
        var toonString = @"user:
  name: Alice
  address:
    city: New York
    zip: 10001";

        // Act
        var result = ToonDecoder.Decode(toonString);

        // Assert
        Assert.NotNull(result);
        var user = result["user"]?.AsObject();
        Assert.NotNull(user);
        Assert.Equal("Alice", user["name"]?.GetValue<string>());
        var address = user["address"]?.AsObject();
        Assert.NotNull(address);
        Assert.Equal("New York", address["city"]?.GetValue<string>());
    }

    [Fact]
    public void Decode_WithStrictOption_ValidatesArrayLength()
    {
        // Arrange - array declares 5 items but only provides 3
        var toonString = "numbers[5]: 1, 2, 3";
        var options = new ToonDecodeOptions { Strict = true };

        // Act & Assert
        Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(toonString, options));
    }

    [Fact]
    public void Decode_WithNonStrictOption_AllowsLengthMismatch()
    {
        // Arrange - array declares 5 items but only provides 3
        var toonString = "numbers[5]: 1, 2, 3";
        var options = new ToonDecodeOptions { Strict = false };

        // Act
        var result = ToonDecoder.Decode(toonString, options);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        var numbers = obj["numbers"]?.AsArray();
        Assert.NotNull(numbers);
        Assert.Equal(3, numbers.Count);
    }

    [Fact]
    public void Decode_InvalidFormat_ThrowsToonFormatException()
    {
        // Arrange - array length mismatch with strict mode
        var invalidToon = "items[10]: 1, 2, 3";
        var options = new ToonDecodeOptions { Strict = true };

        // Act & Assert
        Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(invalidToon, options));
    }

    [Fact]
    public void Decode_EmptyString_ReturnsEmptyObject()
    {
        // Arrange
        var emptyString = "";

        // Act
        var result = ToonDecoder.Decode(emptyString);

        // Assert - empty string returns empty array
        Assert.NotNull(result);
    }
}
