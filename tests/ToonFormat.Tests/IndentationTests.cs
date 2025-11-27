using System;
using Toon.Format;
using Xunit;

namespace ToonFormat.Tests
{
    public class IndentationTests
    {
        [Fact]
        public void Indentation_Default2Spaces()
        {
            var obj = new ToonObject
            {
                ["parent"] = new ToonObject
                {
                    ["child"] = "value"
                }
            };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("parent:\n  child: value", encoded);
        }

        [Fact]
        public void Indentation_Custom4Spaces()
        {
            var obj = new ToonObject
            {
                ["parent"] = new ToonObject
                {
                    ["child"] = "value"
                }
            };
            var options = new ToonEncodeOptions { Indent = 4 };
            var encoded = ToonEncoder.Encode(obj, options);
            Assert.Contains("parent:\n    child: value", encoded);
        }

        [Fact]
        public void Indentation_StrictMode_ExactMultiple()
        {
            var input = @"parent:
  child: value";
            var options = new ToonDecodeOptions { Strict = true, Indent = 2 };
            var decoded = ToonDecoder.Decode(input, options);
            Assert.NotNull(decoded);
        }

        [Fact]
        public void Indentation_StrictMode_InvalidMultiple()
        {
            var input = @"parent:
   child: value"; // 3 spaces instead of 2
            var options = new ToonDecodeOptions { Strict = true, Indent = 2 };
            Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(input, options));
        }

        [Fact]
        public void Indentation_StrictMode_TabsRejected()
        {
            var input = "parent:\n\tchild: value"; // Tab for indentation
            var options = new ToonDecodeOptions { Strict = true };
            Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(input, options));
        }

        [Fact]
        public void Indentation_NonStrict_Tolerant()
        {
            var input = @"parent:
   child: value"; // 3 spaces
            var options = new ToonDecodeOptions { Strict = false };
            var decoded = ToonDecoder.Decode(input, options);
            Assert.NotNull(decoded);
        }

        [Fact]
        public void Whitespace_NoTrailingSpaces()
        {
            var obj = new ToonObject { ["key"] = "value" };
            var encoded = ToonEncoder.Encode(obj);
            // No line should end with a space
            Assert.DoesNotMatch(@" \n", encoded);
            Assert.DoesNotMatch(@" $", encoded);
        }

        [Fact]
        public void Whitespace_NoTrailingNewline()
        {
            var obj = new ToonObject { ["key"] = "value" };
            var encoded = ToonEncoder.Encode(obj);
            // Should not end with newline
            Assert.False(encoded.EndsWith("\n"));
        }

        [Fact]
        public void Indentation_NestedObjects()
        {
            var obj = new ToonObject
            {
                ["level1"] = new ToonObject
                {
                    ["level2"] = new ToonObject
                    {
                        ["level3"] = "value"
                    }
                }
            };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("level1:\n  level2:\n    level3: value", encoded);
        }
    }
}
