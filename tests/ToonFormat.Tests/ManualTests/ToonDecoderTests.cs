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
}
