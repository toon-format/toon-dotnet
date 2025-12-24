using System.Text.Json;
using System.Text.Json.Nodes;
using Toon.Format;

namespace Toon.Format.Tests;

/// <summary>
/// Round-trip tests to verify encoding and decoding preserve data integrity.
/// </summary>
public class ToonRoundTripTests
{
    [Fact]
    public void RoundTrip_SimpleObject_PreservesData()
    {
        // Arrange
        var original = new { name = "Alice", age = 30, active = true };

        // Act
        var encoded = ToonEncoder.Encode(original);
        var decoded = ToonDecoder.Decode(encoded);

        // Assert
        Assert.NotNull(decoded);
        var obj = decoded.AsObject();
        Assert.Equal("Alice", obj["name"]?.GetValue<string>());
        Assert.Equal(30.0, obj["age"]?.GetValue<double>());
        Assert.True(obj["active"]?.GetValue<bool>());
    }

    [Fact]
    public void RoundTrip_Array_PreservesData()
    {
        // Arrange
        var original = new { numbers = new[] { 1, 2, 3, 4, 5 } };

        // Act
        var encoded = ToonEncoder.Encode(original);
        var decoded = ToonDecoder.Decode(encoded);

        // Assert
        Assert.NotNull(decoded);
        var numbers = decoded["numbers"]?.AsArray();
        Assert.NotNull(numbers);
        Assert.Equal(5, numbers.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal((double)(i + 1), numbers[i]?.GetValue<double>());
        }
    }

    [Fact]
    public void RoundTrip_ComplexStructure_PreservesData()
    {
        // Arrange
        var original = new
        {
            users = new[]
            {
                new { id = 1, name = "Alice", email = "alice@example.com" },
                new { id = 2, name = "Bob", email = "bob@example.com" }
            },
            metadata = new
            {
                total = 2,
                timestamp = "2025-01-01T00:00:00Z"
            }
        };

        // Act
        var encoded = ToonEncoder.Encode(original);
        var decoded = ToonDecoder.Decode(encoded);

        // Assert
        Assert.NotNull(decoded);
        var users = decoded["users"]?.AsArray();
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
        Assert.Equal("Alice", users[0]?["name"]?.GetValue<string>());

        var metadata = decoded["metadata"]?.AsObject();
        Assert.NotNull(metadata);
        Assert.Equal(2.0, metadata["total"]?.GetValue<double>());
    }
}
