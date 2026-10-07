using System.Text;
using System.Text.Json.Nodes;

namespace Toon.Format.Tests;

/// <summary>
/// Covers the UTF-8 byte and stream overloads, which the string-based spec fixtures can't reach.
/// </summary>
public class ByteInputTests
{
    private static readonly byte[] IllFormed = [.. Encoding.UTF8.GetBytes("a: x"), 0xFF];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Decode_RejectsIllFormedUtf8(bool strict)
    {
        var options = new ToonDecodeOptions { Strict = strict };

        Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(IllFormed, options));
        Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(new MemoryStream(IllFormed), options));
        await Assert.ThrowsAsync<ToonFormatException>(() => ToonDecoder.DecodeAsync(new MemoryStream(IllFormed), options));
    }

    [Fact]
    public void Decode_RemovesOneLeadingByteOrderMark()
    {
        byte[] twoBoms = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("a: 1")];

        Assert.Equal("\uFEFFa", ((JsonObject)ToonDecoder.Decode(twoBoms)!).Single().Key);
        Assert.Equal("\uFEFFa", ((JsonObject)ToonDecoder.Decode(new MemoryStream(twoBoms))!).Single().Key);
    }
}
