using System.Text;

namespace Toon.Format.Tests;

/// <summary>
/// Covers the async stream overloads.
/// </summary>
public class ToonAsyncTests
{
    [Fact]
    public async Task EncodeToStreamAsync_WritesUtf8Toon()
    {
        using var stream = new MemoryStream();

        await ToonEncoder.EncodeToStreamAsync(new { id = 123 }, stream);

        Assert.Equal("id: 123", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task DecodeAsync_ReturnsJsonNode()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("message: Hello World"));

        var result = await ToonDecoder.DecodeAsync(stream);

        Assert.Equal("Hello World", result!["message"]!.GetValue<string>());
    }

    [Fact]
    public async Task DecodeAsyncT_DeserializesIntoType()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Charlie\nAge: 35"));

        var person = await ToonDecoder.DecodeAsync<Person>(stream);

        Assert.Equal("Charlie", person!.Name);
        Assert.Equal(35, person.Age);
    }

    [Fact]
    public async Task NullStream_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => ToonEncoder.EncodeToStreamAsync(new { id = 1 }, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ToonDecoder.DecodeAsync(null!));
    }

    private sealed class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}
