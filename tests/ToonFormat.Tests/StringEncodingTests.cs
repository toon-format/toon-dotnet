using System;
using Toon.Format;
using Xunit;

namespace ToonFormat.Tests
{
    public class StringEncodingTests
    {
        [Fact]
        public void Escaping_EncodesBackslash()
        {
            var obj = new ToonObject { ["key"] = "path\\to\\file" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"path\\\\to\\\\file\"", encoded);
        }

        [Fact]
        public void Escaping_EncodesQuote()
        {
            var obj = new ToonObject { ["key"] = "say \"hello\"" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"say \\\"hello\\\"\"", encoded);
        }

        [Fact]
        public void Escaping_EncodesNewline()
        {
            var obj = new ToonObject { ["key"] = "line1\nline2" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"line1\\nline2\"", encoded);
        }

        [Fact]
        public void Escaping_EncodesCarriageReturn()
        {
            var obj = new ToonObject { ["key"] = "line1\rline2" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"line1\\rline2\"", encoded);
        }

        [Fact]
        public void Escaping_EncodesTab()
        {
            var obj = new ToonObject { ["key"] = "col1\tcol2" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"col1\\tcol2\"", encoded);
        }

        [Fact]
        public void Quoting_EmptyString()
        {
            var obj = new ToonObject { ["key"] = "" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"\"", encoded);
        }

        [Fact]
        public void Quoting_LeadingWhitespace()
        {
            var obj = new ToonObject { ["key"] = " value" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \" value\"", encoded);
        }

        [Fact]
        public void Quoting_TrailingWhitespace()
        {
            var obj = new ToonObject { ["key"] = "value " };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"value \"", encoded);
        }

        [Fact]
        public void Quoting_ReservedWord_True()
        {
            var obj = new ToonObject { ["key"] = "true" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"true\"", encoded);
        }

        [Fact]
        public void Quoting_ReservedWord_False()
        {
            var obj = new ToonObject { ["key"] = "false" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"false\"", encoded);
        }

        [Fact]
        public void Quoting_ReservedWord_Null()
        {
            var obj = new ToonObject { ["key"] = "null" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"null\"", encoded);
        }

        [Fact]
        public void Quoting_NumericLike_Integer()
        {
            var obj = new ToonObject { ["key"] = "42" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"42\"", encoded);
        }

        [Fact]
        public void Quoting_NumericLike_Float()
        {
            var obj = new ToonObject { ["key"] = "3.14" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"3.14\"", encoded);
        }

        [Fact]
        public void Quoting_ContainsColon()
        {
            var obj = new ToonObject { ["key"] = "http://example.com" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"http://example.com\"", encoded);
        }

        [Fact]
        public void Quoting_StartsWithHyphen()
        {
            var obj = new ToonObject { ["key"] = "-value" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"-value\"", encoded);
        }

        [Fact]
        public void Quoting_SingleHyphen()
        {
            var obj = new ToonObject { ["key"] = "-" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("key: \"-\"", encoded);
        }

        [Fact]
        public void KeyEncoding_ValidUnquotedKey()
        {
            var obj = new ToonObject { ["valid_key123"] = "value" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("valid_key123: value", encoded);
        }

        [Fact]
        public void KeyEncoding_InvalidUnquotedKey_RequiresQuotes()
        {
            var obj = new ToonObject { ["my-key"] = "value" };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("\"my-key\": value", encoded);
        }

        [Fact]
        public void KeyEncoding_DottedKey_LiteralKey()
        {
            var obj = new ToonObject { ["user.name"] = "Alice" };
            var encoded = ToonEncoder.Encode(obj);
            // Dotted keys are valid literal keys
            Assert.Contains("user.name: Alice", encoded);
        }

        [Fact]
        public void Decoding_UnescapesBackslash()
        {
            var input = "key: \"path\\\\to\\\\file\"";
            var decoded = ToonDecoder.DecodeToonObject(input);
            Assert.Equal("path\\to\\file", decoded["key"]!.ToString());
        }

        [Fact]
        public void Decoding_UnescapesQuote()
        {
            var input = "key: \"say \\\"hello\\\"\"";
            var decoded = ToonDecoder.DecodeToonObject(input);
            Assert.Equal("say \"hello\"", decoded["key"]!.ToString());
        }

        [Fact]
        public void Decoding_UnescapesNewline()
        {
            var input = "key: \"line1\\nline2\"";
            var decoded = ToonDecoder.DecodeToonObject(input);
            Assert.Equal("line1\nline2", decoded["key"]!.ToString());
        }

        [Fact]
        public void Decoding_UnescapesTab()
        {
            var input = "key: \"col1\\tcol2\"";
            var decoded = ToonDecoder.DecodeToonObject(input);
            Assert.Equal("col1\tcol2", decoded["key"]!.ToString());
        }
    }
}
