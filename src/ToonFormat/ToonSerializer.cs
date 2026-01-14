using System;
using System.IO;
using System.Text.Json.Nodes;
using Toon.Format;

namespace ToonFormat;

/// <summary>
/// Provides a concise API similar to System.Text.Json.JsonSerializer for converting between TOON format and objects/JSON.
/// </summary>
public static class ToonSerializer
{
    #region Serialize (Object to TOON)

    /// <summary>
    /// Serializes the specified value to a TOON-formatted string.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>A TOON-formatted string representation of the value.</returns>
    public static string Serialize<T>(T value)
    {
        return ToonEncoder.Encode(value);
    }

    /// <summary>
    /// Serializes the specified value to a TOON-formatted string with custom options.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Options to control serialization behavior.</param>
    /// <returns>A TOON-formatted string representation of the value.</returns>
    public static string Serialize<T>(T value, ToonEncodeOptions? options)
    {
        return ToonEncoder.Encode(value, options ?? new ToonEncodeOptions());
    }

    /// <summary>
    /// Serializes the specified value to a UTF-8 encoded TOON byte array.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>A UTF-8 encoded byte array containing the TOON representation.</returns>
    public static byte[] SerializeToUtf8Bytes<T>(T value)
    {
        return ToonEncoder.EncodeToBytes(value);
    }

    /// <summary>
    /// Serializes the specified value to a UTF-8 encoded TOON byte array with custom options.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Options to control serialization behavior.</param>
    /// <returns>A UTF-8 encoded byte array containing the TOON representation.</returns>
    public static byte[] SerializeToUtf8Bytes<T>(T value, ToonEncodeOptions? options)
    {
        return ToonEncoder.EncodeToBytes(value, options ?? new ToonEncodeOptions());
    }

    /// <summary>
    /// Serializes the specified value and writes it to the specified stream.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="stream">The stream to write the TOON representation to.</param>
    /// <param name="value">The value to serialize.</param>
    public static void Serialize<T>(Stream stream, T value)
    {
        ToonEncoder.EncodeToStream(value, stream);
    }

    /// <summary>
    /// Serializes the specified value and writes it to the specified stream with custom options.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="stream">The stream to write the TOON representation to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Options to control serialization behavior.</param>
    public static void Serialize<T>(Stream stream, T value, ToonEncodeOptions? options)
    {
        ToonEncoder.EncodeToStream(value, stream, options ?? new ToonEncodeOptions());
    }

    #endregion

    #region Deserialize (TOON to Object)

    /// <summary>
    /// Deserializes the TOON-formatted string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="toon">The TOON-formatted string to deserialize.</param>
    /// <returns>The deserialized value of type T.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toon is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static T? Deserialize<T>(string toon)
    {
        return ToonDecoder.Decode<T>(toon);
    }

    /// <summary>
    /// Deserializes the TOON-formatted string to the specified type with custom options.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="toon">The TOON-formatted string to deserialize.</param>
    /// <param name="options">Options to control deserialization behavior.</param>
    /// <returns>The deserialized value of type T.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toon or options is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static T? Deserialize<T>(string toon, ToonDecodeOptions? options)
    {
        return ToonDecoder.Decode<T>(toon, options ?? new ToonDecodeOptions());
    }

    /// <summary>
    /// Deserializes the UTF-8 encoded TOON byte array to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Toon">The UTF-8 encoded TOON byte array to deserialize.</param>
    /// <returns>The deserialized value of type T.</returns>
    /// <exception cref="ArgumentNullException">Thrown when utf8Toon is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Toon)
    {
        return ToonDecoder.Decode<T>(utf8Toon);
    }

    /// <summary>
    /// Deserializes the UTF-8 encoded TOON byte array to the specified type with custom options.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Toon">The UTF-8 encoded TOON byte array to deserialize.</param>
    /// <param name="options">Options to control deserialization behavior.</param>
    /// <returns>The deserialized value of type T.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Toon, ToonDecodeOptions? options)
    {
        return ToonDecoder.Decode<T>(utf8Toon, options ?? new ToonDecodeOptions());
    }

    /// <summary>
    /// Deserializes the TOON data from the specified stream to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream containing TOON data to deserialize.</param>
    /// <returns>The deserialized value of type T.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static T? Deserialize<T>(Stream stream)
    {
        return ToonDecoder.Decode<T>(stream);
    }

    /// <summary>
    /// Deserializes the TOON data from the specified stream to the specified type with custom options.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream containing TOON data to deserialize.</param>
    /// <param name="options">Options to control deserialization behavior.</param>
    /// <returns>The deserialized value of type T.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream or options is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static T? Deserialize<T>(Stream stream, ToonDecodeOptions? options)
    {
        return ToonDecoder.Decode<T>(stream, options ?? new ToonDecodeOptions());
    }

    #endregion

    #region JsonNode Conversion

    /// <summary>
    /// Deserializes the TOON-formatted string to a JsonNode.
    /// </summary>
    /// <param name="toon">The TOON-formatted string to deserialize.</param>
    /// <returns>The deserialized JsonNode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toon is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static JsonNode? DeserializeToJsonNode(string toon)
    {
        return ToonDecoder.Decode(toon);
    }

    /// <summary>
    /// Deserializes the TOON-formatted string to a JsonNode with custom options.
    /// </summary>
    /// <param name="toon">The TOON-formatted string to deserialize.</param>
    /// <param name="options">Options to control deserialization behavior.</param>
    /// <returns>The deserialized JsonNode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toon or options is null.</exception>
    /// <exception cref="ToonFormatException">Thrown when the TOON format is invalid.</exception>
    public static JsonNode? DeserializeToJsonNode(string toon, ToonDecodeOptions? options)
    {
        return ToonDecoder.Decode(toon, options ?? new ToonDecodeOptions());
    }

    #endregion
}