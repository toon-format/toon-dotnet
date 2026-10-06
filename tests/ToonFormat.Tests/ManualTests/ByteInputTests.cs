using System.Text;
using System.Text.Json.Nodes;

namespace Toon.Format.Tests;

/// <summary>
/// Covers the UTF-8 byte and stream overloads, which the string-based spec fixtures can't reach.
/// </summary>
public class ByteInputTests
{
    private static readonly byte[] IllFormed = [.. Encoding.UTF8.GetBytes("a: x"), 0xFF];

    [Fact]
    public async Task Decode_RejectsIllFormedUtf8InStrictMode()
    {
        Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(IllFormed));
        Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(new MemoryStream(IllFormed)));
        await Assert.ThrowsAsync<ToonFormatException>(() => ToonDecoder.DecodeAsync(new MemoryStream(IllFormed)));
    }

    [Fact]
    public void Decode_ReplacesIllFormedUtf8InNonStrictMode()
    {
        var options = new ToonDecodeOptions { Strict = false };

        Assert.Equal("x\uFFFD", ToonDecoder.Decode(IllFormed, options)!["a"]!.GetValue<string>());
        Assert.Equal("x\uFFFD", ToonDecoder.Decode(new MemoryStream(IllFormed), options)!["a"]!.GetValue<string>());
    }

    [Fact]
    public void Decode_RemovesOneLeadingByteOrderMark()
    {
        byte[] twoBoms = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("a: 1")];

        Assert.Equal("\uFEFFa", ((JsonObject)ToonDecoder.Decode(twoBoms)!).Single().Key);
        Assert.Equal("\uFEFFa", ((JsonObject)ToonDecoder.Decode(new MemoryStream(twoBoms))!).Single().Key);
    }
}
