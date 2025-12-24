#nullable enable
using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Toon.Format;
using Xunit;

namespace Toon.Format.Tests;

/// <summary>
/// Tests for async encoding and decoding methods.
/// </summary>
public class ToonAsyncTests
{
    #region EncodeAsync Tests

    [Fact]
    public async Task EncodeAsync_WithSimpleObject_ReturnsValidToon()
    {
        // Arrange
        var data = new { name = "Alice", age = 30 };

        // Act
        var result = await ToonEncoder.EncodeAsync(data);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("name:", result);
        Assert.Contains("Alice", result);
        Assert.Contains("age:", result);
        Assert.Contains("30", result);
    }

    [Fact]
    public async Task EncodeAsync_WithOptions_RespectsIndentation()
    {
        // Arrange
        var data = new { outer = new { inner = "value" } };
        var options = new ToonEncodeOptions { Indent = 4 };

        // Act
        var result = await ToonEncoder.EncodeAsync(data, options);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("outer:", result);
    }

    [Fact]
    public async Task EncodeAsync_WithCancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var data = new { name = "Test" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => ToonEncoder.EncodeAsync(data, cts.Token));
    }

    [Fact]
    public async Task EncodeToBytesAsync_WithSimpleObject_ReturnsUtf8Bytes()
    {
        // Arrange
        var data = new { message = "Hello" };

        // Act
        var result = await ToonEncoder.EncodeToBytesAsync(data);

        // Assert
        Assert.NotNull(result);
        var text = Encoding.UTF8.GetString(result);
        Assert.Contains("message:", text);
        Assert.Contains("Hello", text);
    }

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

    #endregion

    #region DecodeAsync Tests

    [Fact]
    public async Task DecodeAsync_WithValidToon_ReturnsJsonNode()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";

        // Act
        var result = await ToonDecoder.DecodeAsync(toon);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<JsonObject>(result);
        var obj = (JsonObject)result;
        Assert.Equal("Alice", obj["name"]?.GetValue<string>());
        Assert.Equal(30.0, obj["age"]?.GetValue<double>());
    }

    [Fact]
    public async Task DecodeAsync_Generic_DeserializesToType()
    {
        // Arrange
        var toon = "name: Bob\nage: 25";

        // Act
        var result = await ToonDecoder.DecodeAsync<TestPerson>(toon);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bob", result.name);
        Assert.Equal(25, result.age);
    }

    [Fact]
    public async Task DecodeAsync_WithCancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var toon = "name: Test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => ToonDecoder.DecodeAsync(toon, cts.Token));
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
    public async Task DecodeAsync_WithOptions_RespectsStrictMode()
    {
        // Arrange - array declares 5 items but only provides 3, strict mode should throw
        var toon = "numbers[5]: 1, 2, 3";
        var options = new ToonDecodeOptions { Strict = true };

        // Act & Assert
        await Assert.ThrowsAsync<ToonFormatException>(
            () => ToonDecoder.DecodeAsync(toon, options));
    }

    #endregion

    #region Round-Trip Async Tests

    [Fact]
    public async Task AsyncRoundTrip_PreservesData()
    {
        // Arrange
        var original = new TestPerson { name = "Diana", age = 28 };

        // Act
        var encoded = await ToonEncoder.EncodeAsync(original);
        var decoded = await ToonDecoder.DecodeAsync<TestPerson>(encoded);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(original.name, decoded.name);
        Assert.Equal(original.age, decoded.age);
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

    #endregion

    #region Test Helpers

    private class TestPerson
    {
        public string? name { get; set; }
        public int age { get; set; }
    }

    #endregion
}

