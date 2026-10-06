using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Toon.Format.Internal.Decode;

namespace Toon.Format;

/// <summary>
/// Decodes TOON into <see cref="JsonNode"/> values or typed objects.
/// </summary>
public static class ToonDecoder
{
    /// <summary>
    /// Decodes a TOON string; an empty document decodes to an empty object.
    /// </summary>
    /// <exception cref="ToonFormatException">The input is not valid TOON.</exception>
    public static JsonNode? Decode(string toonString, ToonDecodeOptions? options = null)
    {
        if (toonString == null)
            throw new ArgumentNullException(nameof(toonString));

        options ??= new ToonDecodeOptions();
        if (options.IndentSize < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "IndentSize must be at least 1");

        var cursor = Scanner.Scan(toonString, options.IndentSize, options.Strict);
        return new DocumentDecoder(cursor, options.Strict).DecodeDocument();
    }

    /// <summary>
    /// Decodes a TOON string and deserializes the result into <typeparamref name="T"/> through <c>System.Text.Json</c>.
    /// </summary>
    /// <exception cref="ToonFormatException">The input is not valid TOON.</exception>
    public static T? Decode<T>(string toonString, ToonDecodeOptions? options = null)
    {
        var node = Decode(toonString, options);
        if (node is null)
            return default;

        if (typeof(JsonNode).IsAssignableFrom(typeof(T)))
        {
            return (T?)(object?)node;
        }

        return JsonSerializer.Deserialize<T>(node.ToJsonString());
    }

    /// <summary>
    /// Decodes UTF-8 TOON bytes; in strict mode, ill-formed UTF-8 throws instead of decoding to U+FFFD.
    /// </summary>
    /// <exception cref="ToonFormatException">The input is not valid TOON.</exception>
    public static JsonNode? Decode(byte[] utf8Bytes, ToonDecodeOptions? options = null)
    {
        return Decode(GetString(utf8Bytes, options), options);
    }

    /// <summary>
    /// Decodes UTF-8 TOON bytes into <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="ToonFormatException">The input is not valid TOON.</exception>
    public static T? Decode<T>(byte[] utf8Bytes, ToonDecodeOptions? options = null)
    {
        return Decode<T>(GetString(utf8Bytes, options), options);
    }

    /// <summary>
    /// Decodes UTF-8 TOON read from <paramref name="stream"/> and leaves the stream open.
    /// </summary>
    /// <exception cref="ToonFormatException">The input is not valid TOON.</exception>
    public static JsonNode? Decode(Stream stream, ToonDecodeOptions? options = null)
    {
        return Decode(ReadToEnd(stream, options), options);
    }

    /// <summary>
    /// Decodes UTF-8 TOON read from <paramref name="stream"/> into <typeparamref name="T"/> and leaves the stream open.
    /// </summary>
    /// <exception cref="ToonFormatException">The input is not valid TOON.</exception>
    public static T? Decode<T>(Stream stream, ToonDecodeOptions? options = null)
    {
        return Decode<T>(ReadToEnd(stream, options), options);
    }

    /// <inheritdoc cref="Decode(Stream, ToonDecodeOptions?)"/>
    public static async Task<JsonNode?> DecodeAsync(Stream stream, ToonDecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Decode(await ReadToEndAsync(stream, options, cancellationToken).ConfigureAwait(false), options);
    }

    /// <inheritdoc cref="Decode{T}(Stream, ToonDecodeOptions?)"/>
    public static async Task<T?> DecodeAsync<T>(Stream stream, ToonDecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Decode<T>(await ReadToEndAsync(stream, options, cancellationToken).ConfigureAwait(false), options);
    }

    // Neither encoding has a preamble, so a byte-order mark reaches the scanner, which removes exactly one.
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly UTF8Encoding LenientUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    private static string GetString(byte[] utf8Bytes, ToonDecodeOptions? options)
    {
        if (utf8Bytes == null)
            throw new ArgumentNullException(nameof(utf8Bytes));

        return ReadUtf8(() => EncodingFor(options).GetString(utf8Bytes));
    }

    private static string ReadToEnd(Stream stream, ToonDecodeOptions? options)
    {
        using var reader = CreateReader(stream, options);
        return ReadUtf8(reader.ReadToEnd);
    }

    private static async Task<string> ReadToEndAsync(Stream stream, ToonDecodeOptions? options, CancellationToken cancellationToken)
    {
        using var reader = CreateReader(stream, options);
        try
        {
#if NETSTANDARD2_0
            return await reader.ReadToEndAsync().ConfigureAwait(false);
#else
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#endif
        }
        catch (DecoderFallbackException ex)
        {
            throw IllFormedUtf8(ex);
        }
    }

    private static StreamReader CreateReader(Stream stream, ToonDecodeOptions? options)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        return new StreamReader(stream, EncodingFor(options), detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
    }

    private static UTF8Encoding EncodingFor(ToonDecodeOptions? options) => options?.Strict ?? true ? StrictUtf8 : LenientUtf8;

    private static string ReadUtf8(Func<string> read)
    {
        try
        {
            return read();
        }
        catch (DecoderFallbackException ex)
        {
            throw IllFormedUtf8(ex);
        }
    }

    private static ToonFormatException IllFormedUtf8(DecoderFallbackException ex) =>
        ToonFormatException.Syntax("Input is not well-formed UTF-8", inner: ex);
}
