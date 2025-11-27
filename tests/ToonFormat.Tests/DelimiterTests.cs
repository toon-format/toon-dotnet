using System;
using System.Text.Json.Nodes;
using Toon.Format;
using Xunit;

namespace ToonFormat.Tests
{
    public class DelimiterTests
    {
        [Fact]
        public void Delimiter_Comma_Default()
        {
            var obj = new ToonObject
            {
                ["items"] = new ToonArray { 1, 2, 3 }
            };
            var encoded = ToonEncoder.Encode(obj);
            // Comma is default, no delimiter symbol in header
            Assert.Contains("items[3]: 1,2,3", encoded);
        }

        [Fact]
        public void Delimiter_Tab_Explicit()
        {
            var obj = new ToonObject
            {
                ["items"] = new ToonArray { 1, 2, 3 }
            };
            var options = new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB };
            var encoded = ToonEncoder.Encode(obj, options);
            Assert.Contains("[3\t]:", encoded);
            Assert.Contains("1\t2\t3", encoded);
        }

        [Fact]
        public void Delimiter_Pipe_Explicit()
        {
            var obj = new ToonObject
            {
                ["items"] = new ToonArray { 1, 2, 3 }
            };
            var options = new ToonEncodeOptions { Delimiter = ToonDelimiter.PIPE };
            var encoded = ToonEncoder.Encode(obj, options);
            Assert.Contains("[3|]:", encoded);
            Assert.Contains("1|2|3", encoded);
        }

        [Fact]
        public void Delimiter_Consistency_InHeader()
        {
            var obj = new ToonObject
            {
                ["users"] = new ToonArray
                {
                    new ToonObject { ["id"] = 1, ["name"] = "Alice" },
                    new ToonObject { ["id"] = 2, ["name"] = "Bob" }
                }
            };
            var options = new ToonEncodeOptions { Delimiter = ToonDelimiter.PIPE };
            var encoded = ToonEncoder.Encode(obj, options);
            // Header should use pipe in both bracket and brace
            Assert.Contains("[2|]{id|name}:", encoded);
            Assert.Contains("1|Alice", encoded);
        }

        [Fact]
        public void Delimiter_QuotingAwareness_Comma()
        {
            var obj = new ToonObject
            {
                ["items"] = new ToonArray { "a,b", "c" }
            };
            var encoded = ToonEncoder.Encode(obj);
            // String containing comma must be quoted
            Assert.Contains("\"a,b\"", encoded);
        }

        [Fact]
        public void Delimiter_QuotingAwareness_Pipe()
        {
            var obj = new ToonObject
            {
                ["items"] = new ToonArray { "a|b", "c" }
            };
            var options = new ToonEncodeOptions { Delimiter = ToonDelimiter.PIPE };
            var encoded = ToonEncoder.Encode(obj, options);
            // String containing pipe must be quoted
            Assert.Contains("\"a|b\"", encoded);
        }

        [Fact]
        public void Delimiter_Decoding_Comma()
        {
            var input = "items[3]: a, b, c";
            var decoded = ToonDecoder.Decode(input);
            Assert.NotNull(decoded);
            var items = decoded!["items"] as JsonArray;
            Assert.NotNull(items);
            Assert.Equal(3, items!.Count);
            Assert.Equal("a", items[0]!.ToString());
            Assert.Equal("b", items[1]!.ToString());
            Assert.Equal("c", items[2]!.ToString());
        }

        [Fact]
        public void Delimiter_Decoding_Tab()
        {
            var input = "items[3\t]: a\tb\tc";
            var decoded = ToonDecoder.Decode(input);
            Assert.NotNull(decoded);
            var items = decoded!["items"] as JsonArray;
            Assert.NotNull(items);
            Assert.Equal(3, items!.Count);
        }

        [Fact]
        public void Delimiter_Decoding_Pipe()
        {
            var input = "items[3|]: a|b|c";
            var decoded = ToonDecoder.Decode(input);
            Assert.NotNull(decoded);
            var items = decoded!["items"] as JsonArray;
            Assert.NotNull(items);
            Assert.Equal(3, items!.Count);
        }
    }
}
