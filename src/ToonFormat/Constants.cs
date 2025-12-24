using System;

namespace Toon.Format
{
    /// <summary>
    /// TOON format constants for structural characters, literals, and delimiters.
    /// </summary>
    public static class Constants
    {
        /// <summary>The list item marker character (-).</summary>
        public const char LIST_ITEM_MARKER = '-';

        /// <summary>The list item prefix string ("- ").</summary>
        public const string LIST_ITEM_PREFIX = "- ";

        // #region Structural characters
        /// <summary>Comma delimiter character (,).</summary>
        public const char COMMA = ',';
        /// <summary>Colon separator character (:).</summary>
        public const char COLON = ':';
        /// <summary>Space character.</summary>
        public const char SPACE = ' ';
        /// <summary>Pipe delimiter character (|).</summary>
        public const char PIPE = '|';
        /// <summary>Hash/pound character (#).</summary>
        public const char HASH = '#';
        /// <summary>Dot/period character (.).</summary>
        public const char DOT = '.';
        // #endregion

        // #region Brackets and braces
        /// <summary>Opening square bracket ([).</summary>
        public const char OPEN_BRACKET = '[';
        /// <summary>Closing square bracket (]).</summary>
        public const char CLOSE_BRACKET = ']';
        /// <summary>Opening curly brace ({).</summary>
        public const char OPEN_BRACE = '{';
        /// <summary>Closing curly brace (}).</summary>
        public const char CLOSE_BRACE = '}';
        // #endregion

        // #region Literals
        /// <summary>Null literal string ("null").</summary>
        public const string NULL_LITERAL = "null";
        /// <summary>True literal string ("true").</summary>
        public const string TRUE_LITERAL = "true";
        /// <summary>False literal string ("false").</summary>
        public const string FALSE_LITERAL = "false";
        // #endregion

        // #region Escape/control characters
        /// <summary>Backslash escape character (\).</summary>
        public const char BACKSLASH = '\\';
        /// <summary>Double quote character (").</summary>
        public const char DOUBLE_QUOTE = '"';
        /// <summary>Newline character (\n).</summary>
        public const char NEWLINE = '\n';
        /// <summary>Carriage return character (\r).</summary>
        public const char CARRIAGE_RETURN = '\r';
        /// <summary>Tab character (\t).</summary>
        public const char TAB = '\t';

        // #region Delimiter defaults and mapping
        /// <summary>Default delimiter enum value (COMMA).</summary>
        public const ToonDelimiter DEFAULT_DELIMITER_ENUM = ToonDelimiter.COMMA;

        /// <summary>Default delimiter character (comma).</summary>
        public const char DEFAULT_DELIMITER_CHAR = COMMA;

        /// <summary>Maps delimiter enum values to their specific characters.</summary>
        public static char ToDelimiterChar(ToonDelimiter delimiter) => delimiter switch
        {
            ToonDelimiter.COMMA => COMMA,
            ToonDelimiter.TAB => TAB,
            ToonDelimiter.PIPE => PIPE,
            _ => COMMA
        };

        /// <summary>Maps delimiter characters to enum; unknown characters fall back to comma.</summary>
        public static ToonDelimiter FromDelimiterChar(char delimiter) => delimiter switch
        {
            COMMA => ToonDelimiter.COMMA,
            TAB => ToonDelimiter.TAB,
            PIPE => ToonDelimiter.PIPE,
            _ => ToonDelimiter.COMMA
        };

        /// <summary>Returns whether the character is a supported delimiter.</summary>
        public static bool IsDelimiterChar(char c) => c == COMMA || c == TAB || c == PIPE;

        /// <summary>Returns whether the character is a whitespace character (space or tab).</summary>
        public static bool IsWhitespace(char c) => c == SPACE || c == TAB;

        /// <summary>Returns whether the character is a structural character.</summary>
        public static bool IsStructural(char c)
            => c == COLON || c == OPEN_BRACKET || c == CLOSE_BRACKET || c == OPEN_BRACE || c == CLOSE_BRACE;
        // #endregion
    }

    /// <summary>
    /// TOON's unified options configuration, styled to align with System.Text.Json. Used to control indentation,
    /// delimiters, strict mode, length markers, and underlying JSON behavior.
    /// </summary>
    public enum ToonDelimiter
    {
        /// <summary>Comma ,</summary>
        COMMA,

        /// <summary>Tab \t</summary>
        TAB,

        /// <summary>Pipe |</summary>
        PIPE
    }

    /// <summary>
    /// Key folding options
    /// </summary>
    public enum ToonKeyFolding
    {
        /// <summary>Key folding disabled</summary>
        Off,

        /// <summary>Nested objects with single keys are collapsed into dotted paths</summary>
        Safe
    }

    /// <summary>
    /// Path expansion options
    /// </summary>
    public enum ToonPathExpansion
    {
        /// <summary>Path expansion disabled</summary>
        Off,

        /// <summary>Keys containing dots are expanded into nested structures</summary>
        Safe
    }

}
