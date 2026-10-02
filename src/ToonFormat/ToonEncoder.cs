#nullable enable
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Toon.Format;
using Toon.Format.Internal.Encode;

namespace Toon.Format;

/// <summary>
/// Encodes data structures into TOON format.
/// </summary>
public static class ToonEncoder
{
    /// <summary>
    /// Encodes the specified value into TOON format with default options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <returns>A TOON-formatted string representation of the value.</returns>
    public static string Encode<T>(T data)
    {
        return Encode(data, new ToonEncodeOptions());
    }

    /// <summary>
    /// Encodes the specified value into TOON format with custom options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <returns>A TOON-formatted string representation of the value.</returns>
    public static string Encode<T>(T data, ToonEncodeOptions? options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var normalized = Normalize.NormalizeValue(data);

        var resolvedOptions = new ResolvedEncodeOptions
        {
            Indent = options.Indent,
            Delimiter = Constants.ToDelimiterChar(options.Delimiter),
            KeyFolding = options.KeyFolding,
            FlattenDepth = options.FlattenDepth ?? int.MaxValue,
        };

        return Encoders.EncodeValue(normalized, resolvedOptions);
    }

    /// <summary>
    /// Encodes the specified value into UTF-8 bytes with default options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <returns>UTF-8 encoded TOON bytes.</returns>
    public static byte[] EncodeToBytes<T>(T data)
    {
        var text = Encode(data, new ToonEncodeOptions());
        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    /// Encodes the specified value into UTF-8 bytes with custom options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <returns>UTF-8 encoded TOON bytes.</returns>
    public static byte[] EncodeToBytes<T>(T data, ToonEncodeOptions? options)
    {
        var text = Encode(data, options);
        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    /// Encodes the specified value and writes UTF-8 bytes to the destination stream using default options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="destination">The destination stream to write to. The stream is not disposed.</param>
    public static void EncodeToStream<T>(T data, Stream destination)
    {
        EncodeToStream(data, destination, new ToonEncodeOptions());
    }

    /// <summary>
    /// Encodes the specified value and writes UTF-8 bytes to the destination stream using custom options (generic overload).
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="destination">The destination stream to write to. The stream is not disposed.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    public static void EncodeToStream<T>(T data, Stream destination, ToonEncodeOptions? options)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        var bytes = EncodeToBytes(data, options);
        destination.Write(bytes, 0, bytes.Length);
    }

    #region Async Methods

    /// <summary>
    /// Asynchronously encodes the specified value and writes UTF-8 bytes to the destination stream using default options.
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="destination">The destination stream to write to. The stream is not disposed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static Task EncodeToStreamAsync<T>(T data, Stream destination, CancellationToken cancellationToken = default)
    {
        return EncodeToStreamAsync(data, destination, new ToonEncodeOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously encodes the specified value and writes UTF-8 bytes to the destination stream using custom options.
    /// </summary>
    /// <typeparam name="T">Type of the value to encode.</typeparam>
    /// <param name="data">The value to encode.</param>
    /// <param name="destination">The destination stream to write to. The stream is not disposed.</param>
    /// <param name="options">Encoding options to customize the output format.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when destination or options is null.</exception>
    public static async Task EncodeToStreamAsync<T>(T data, Stream destination, ToonEncodeOptions? options, CancellationToken cancellationToken = default)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        cancellationToken.ThrowIfCancellationRequested();
        var bytes = EncodeToBytes(data, options);
        await destination.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
