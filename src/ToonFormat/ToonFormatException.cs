using System.Text;

namespace Toon.Format;

/// <summary>
/// Thrown when input is not valid TOON or a value can't be encoded.
/// </summary>
public sealed class ToonFormatException : Exception
{
    /// <summary>The kind of rule the input broke.</summary>
    public ToonErrorKind Kind { get; }

    /// <summary>1-based number of the offending line, if known.</summary>
    public int? LineNumber { get; }

    /// <summary>Text of the offending line, if known.</summary>
    public string? SourceLine { get; }

    /// <summary>Creates the exception; <see cref="Exception.Message"/> adds the kind, line number, and source line to <paramref name="message"/>.</summary>
    public ToonFormatException(
        ToonErrorKind kind,
        string message,
        int? lineNumber = null,
        string? sourceLine = null,
        Exception? inner = null)
        : base(BuildMessage(kind, message, lineNumber, sourceLine), inner)
    {
        Detail = message;
        Kind = kind;
        LineNumber = lineNumber;
        SourceLine = sourceLine;
    }

    /// <summary>Creates a <see cref="ToonErrorKind.Syntax"/> error.</summary>
    public static ToonFormatException Syntax(string message, int? lineNumber = null, string? sourceLine = null, Exception? inner = null)
        => new(ToonErrorKind.Syntax, message, lineNumber, sourceLine, inner);

    /// <summary>Creates a <see cref="ToonErrorKind.Range"/> error.</summary>
    public static ToonFormatException Range(string message, int? lineNumber = null, string? sourceLine = null, Exception? inner = null)
        => new(ToonErrorKind.Range, message, lineNumber, sourceLine, inner);

    /// <summary>Creates a <see cref="ToonErrorKind.Validation"/> error.</summary>
    public static ToonFormatException Validation(string message, int? lineNumber = null, string? sourceLine = null, Exception? inner = null)
        => new(ToonErrorKind.Validation, message, lineNumber, sourceLine, inner);

    /// <summary>Creates a <see cref="ToonErrorKind.Indentation"/> error.</summary>
    public static ToonFormatException Indentation(string message, int? lineNumber = null, string? sourceLine = null, Exception? inner = null)
        => new(ToonErrorKind.Indentation, message, lineNumber, sourceLine, inner);

    /// <summary>The message without kind and position.</summary>
    internal string Detail { get; }

    /// <summary>Attaches the line a parse helper failed on, unless the error already names one.</summary>
    internal ToonFormatException AtLine(int lineNumber, string sourceLine)
        => LineNumber is not null ? this : new(Kind, Detail, lineNumber, sourceLine, InnerException);

    private static string BuildMessage(ToonErrorKind kind, string message, int? lineNumber, string? sourceLine)
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(kind).Append("] ").Append(message);

        if (lineNumber is not null)
            sb.Append(" (Line ").Append(lineNumber.Value).Append(')');

        if (!string.IsNullOrEmpty(sourceLine))
            sb.AppendLine().Append("  > ").Append(sourceLine);

        return sb.ToString();
    }
}

/// <summary>The kind of rule a <see cref="ToonFormatException"/> reports.</summary>
public enum ToonErrorKind
{
    /// <summary>The input breaks the TOON grammar.</summary>
    Syntax,
    /// <summary>A declared length doesn't match the rows, items, or entries that follow, or a row or entry row doesn't match the header's field count.</summary>
    Range,
    /// <summary>A structural rule is broken, such as a duplicate key, content after the root, or an unpaired surrogate.</summary>
    Validation,
    /// <summary>Indentation is off: not a multiple of the indent size, tabs in strict mode, or a skipped level.</summary>
    Indentation,
}
