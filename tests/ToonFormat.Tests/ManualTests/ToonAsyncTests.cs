#nullable enable
using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Toon.Format;
using Xunit;

namespace Toon.Format.Tests;

/// <summary>
/// Tests for async encoding and decoding methods.
/// </summary>
public class ToonAsyncTests
{
    [Fact]
    public async Task EncodeToStreamAsync_WritesToStream()
    {
        // Arrange
        var data = new { id = 123 };
        using var stream = new MemoryStream();

        // Act
        await ToonEncoder.EncodeToStreamAsync(data, stream);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync();
        Assert.Contains("id:", result);
        Assert.Contains("123", result);
    }

    [Fact]
    public async Task EncodeToStreamAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var data = new { name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => ToonEncoder.EncodeToStreamAsync(data, null!));
    }

    [Fact]
    public async Task DecodeAsync_FromStream_ReturnsJsonNode()
    {
        // Arrange
        var toon = "message: Hello World";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(toon));

        // Act
        var result = await ToonDecoder.DecodeAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<JsonObject>(result);
        var obj = (JsonObject)result;
        Assert.Equal("Hello World", obj["message"]?.GetValue<string>());
    }

    [Fact]
    public async Task DecodeAsync_Generic_FromStream_DeserializesToType()
    {
        // Arrange
        var toon = "name: Charlie\nage: 35";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(toon));

        // Act
        var result = await ToonDecoder.DecodeAsync<TestPerson>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Charlie", result.name);
        Assert.Equal(35, result.age);
    }

    [Fact]
    public async Task DecodeAsync_FromStream_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => ToonDecoder.DecodeAsync((Stream)null!));
    }

    [Fact]
    public async Task AsyncStreamRoundTrip_PreservesData()
    {
        // Arrange
        var original = new TestPerson { name = "Eve", age = 32 };
        using var stream = new MemoryStream();

        // Act
        await ToonEncoder.EncodeToStreamAsync(original, stream);
        stream.Position = 0;
        var decoded = await ToonDecoder.DecodeAsync<TestPerson>(stream);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(original.name, decoded.name);
        Assert.Equal(original.age, decoded.age);
    }

    private class TestPerson
    {
        public string? name { get; set; }
        public int age { get; set; }
    }
}

