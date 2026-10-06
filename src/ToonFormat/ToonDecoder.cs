#nullable enable
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
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

        var resolvedOptions = new ResolvedDecodeOptions
        {
            IndentSize = options.IndentSize,
            Strict = options.Strict,
        };

        var scanResult = Scanner.ToParsedLines(toonString, resolvedOptions.IndentSize, resolvedOptions.Strict);

        if (scanResult.Lines.Count == 0)
        {
            return new JsonObject();
        }

        var cursor = new LineCursor(scanResult.Lines, scanResult.BlankLines);
        return Decoders.DecodeValueFromLines(cursor, resolvedOptions);
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
    /// Decodes UTF-8 TOON bytes.
    /// </summary>
    /// <exception cref="ToonFormatException">The input is not valid TOON.</exception>
    public static JsonNode? Decode(byte[] utf8Bytes, ToonDecodeOptions? options = null)
    {
        return Decode(GetString(utf8Bytes), options);
    }

    /// <summary>
    /// Decodes UTF-8 TOON bytes into <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="ToonFormatException">The input is not valid TOON.</exception>
    public static T? Decode<T>(byte[] utf8Bytes, ToonDecodeOptions? options = null)
    {
        return Decode<T>(GetString(utf8Bytes), options);
    }

    /// <summary>
    /// Decodes UTF-8 TOON read from <paramref name="stream"/> and leaves the stream open.
    /// </summary>
    /// <exception cref="ToonFormatException">The input is not valid TOON.</exception>
    public static JsonNode? Decode(Stream stream, ToonDecodeOptions? options = null)
    {
        using var reader = CreateReader(stream);
        return Decode(reader.ReadToEnd(), options);
    }

    /// <summary>
    /// Decodes UTF-8 TOON read from <paramref name="stream"/> into <typeparamref name="T"/> and leaves the stream open.
    /// </summary>
    /// <exception cref="ToonFormatException">The input is not valid TOON.</exception>
    public static T? Decode<T>(Stream stream, ToonDecodeOptions? options = null)
    {
        using var reader = CreateReader(stream);
        return Decode<T>(reader.ReadToEnd(), options);
    }

    /// <inheritdoc cref="Decode(Stream, ToonDecodeOptions?)"/>
    public static async Task<JsonNode?> DecodeAsync(Stream stream, ToonDecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Decode(await ReadToEndAsync(stream, cancellationToken).ConfigureAwait(false), options);
    }

    /// <inheritdoc cref="Decode{T}(Stream, ToonDecodeOptions?)"/>
    public static async Task<T?> DecodeAsync<T>(Stream stream, ToonDecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Decode<T>(await ReadToEndAsync(stream, cancellationToken).ConfigureAwait(false), options);
    }

    private static string GetString(byte[] utf8Bytes)
    {
        if (utf8Bytes == null)
            throw new ArgumentNullException(nameof(utf8Bytes));

        return Encoding.UTF8.GetString(utf8Bytes);
    }

    private static StreamReader CreateReader(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        return new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
    }

    private static async Task<string> ReadToEndAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = CreateReader(stream);
#if NETSTANDARD2_0
        return await reader.ReadToEndAsync().ConfigureAwait(false);
#else
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#endif
    }
}
