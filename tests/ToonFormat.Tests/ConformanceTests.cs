using System;
using System.Text.Json.Nodes;
using Toon.Format;
using Xunit;

namespace ToonFormat.Tests
{
    public class ConformanceTests
    {
        [Fact]
        public void KeyFolding_Safe_FoldsKeys()
        {
            var options = new ToonEncodeOptions
            {
                KeyFolding = KeyFolding.Safe,
                FlattenDepth = int.MaxValue
            };

            var obj = new ToonObject
            {
                ["a"] = new ToonObject
                {
                    ["b"] = new ToonObject
                    {
                        ["c"] = 1
                    }
                }
            };

            var encoded = ToonEncoder.Encode(obj, options);
            Assert.Contains("a.b.c: 1", encoded);
        }

        [Fact]
        public void KeyFolding_Safe_RespectsDepth()
        {
            var options = new ToonEncodeOptions
            {
                KeyFolding = KeyFolding.Safe,
                FlattenDepth = 2
            };

            var obj = new ToonObject
            {
                ["a"] = new ToonObject
                {
                    ["b"] = new ToonObject
                    {
                        ["c"] = 1
                    }
                }
            };

            var encoded = ToonEncoder.Encode(obj, options);
            // Should fold a.b, but stop at c because adding c would make 3 segments?
            // Or FlattenDepth is max segments.
            // If depth 2, segments [a, b] is count 2.
            // So a.b: {c: 1}
            Assert.Contains("a.b:", encoded);
            Assert.DoesNotContain("a.b.c:", encoded);
        }

        [Fact]
        public void PathExpansion_Safe_ExpandsKeys()
        {
            var options = new ToonDecodeOptions
            {
                ExpandPaths = ExpandPaths.Safe
            };

            var input = "a.b.c: 1";
            var decoded = ToonDecoder.Decode(input, options);
            Assert.NotNull(decoded);

            var a = decoded!["a"];
            Assert.NotNull(a);
            var b = a!["b"];
            Assert.NotNull(b);
            Assert.Equal(1.0, (double)b!["c"]!);
        }

        [Fact]
        public void PathExpansion_Strict_ThrowsOnConflict()
        {
            var options = new ToonDecodeOptions
            {
                ExpandPaths = ExpandPaths.Safe,
                Strict = true
            };

            var input = @"
a: 1
a.b: 2
";
            Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(input, options));
        }

        [Fact]
        public void PathExpansion_NonStrict_OverwritesOnConflict()
        {
            var options = new ToonDecodeOptions
            {
                ExpandPaths = ExpandPaths.Safe,
                Strict = false
            };

            var input = @"
a: 1
a.b: 2
";
            var decoded = ToonDecoder.Decode(input, options);
            Assert.NotNull(decoded);
            var a = decoded!["a"];
            Assert.NotNull(a);
            Assert.Equal(2.0, (double)a!["b"]!);
        }

        [Fact]
        public void StrictMode_ThrowsOnHeaderDelimiterMismatch()
        {
            var options = new ToonDecodeOptions { Strict = true };
            // Bracket uses pipe, brace uses comma (implicit in a,b)
            // a,b is parsed as one key "a,b" because delimiter is pipe.
            // But "a,b" is invalid unquoted key.
            var input = "[1|]{a,b}: 1|2";

            Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(input, options));
        }

        [Fact]
        public void StrictMode_ThrowsOnInvalidUnquotedKeyInHeader()
        {
            var options = new ToonDecodeOptions { Strict = true };
            // Bracket uses comma, brace uses pipe inside key
            var input = "[1]{a|b}: 1";

            Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(input, options));
        }
    }
}
