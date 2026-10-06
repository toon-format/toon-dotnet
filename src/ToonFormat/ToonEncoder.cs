#nullable enable
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Toon.Format.Internal.Encode;

namespace Toon.Format;

/// <summary>
/// Encodes .NET values to TOON.
/// </summary>
public static class ToonEncoder
{
    /// <summary>
    /// Encodes <paramref name="data"/> as a TOON string.
    /// </summary>
    public static string Encode<T>(T data, ToonEncodeOptions? options = null)
    {
        options ??= new ToonEncodeOptions();

        var resolvedOptions = new ResolvedEncodeOptions
        {
            IndentSize = options.IndentSize,
            Delimiter = Constants.ToDelimiterChar(options.Delimiter),
        };

        return Encoders.EncodeValue(Normalize.NormalizeValue(data), resolvedOptions);
    }

    /// <summary>
    /// Encodes <paramref name="data"/> as UTF-8 TOON bytes.
    /// </summary>
    public static byte[] EncodeToBytes<T>(T data, ToonEncodeOptions? options = null)
    {
        return Encoding.UTF8.GetBytes(Encode(data, options));
    }

    /// <summary>
    /// Writes <paramref name="data"/> as UTF-8 TOON to <paramref name="destination"/> and leaves the stream open.
    /// </summary>
    public static void EncodeToStream<T>(T data, Stream destination, ToonEncodeOptions? options = null)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        var bytes = EncodeToBytes(data, options);
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Writes <paramref name="data"/> as UTF-8 TOON to <paramref name="destination"/> and leaves the stream open.
    /// </summary>
    public static async Task EncodeToStreamAsync<T>(T data, Stream destination, ToonEncodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        var bytes = EncodeToBytes(data, options);
        await destination.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }
}
