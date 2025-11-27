using System;
using System.Text.Json.Nodes;
using Toon.Format;
using Xunit;

namespace ToonFormat.Tests
{
    public class ArrayEncodingTests
    {
        [Fact]
        public void PrimitiveArray_Inline_Comma()
        {
            var obj = new ToonObject
            {
                ["numbers"] = new ToonArray { 1, 2, 3 }
            };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("numbers[3]: 1,2,3", encoded);
        }

        [Fact]
        public void PrimitiveArray_Inline_Tab()
        {
            var obj = new ToonObject
            {
                ["numbers"] = new ToonArray { 1, 2, 3 }
            };
            var options = new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB };
            var encoded = ToonEncoder.Encode(obj, options);
            Assert.Contains("numbers[3\t]: 1\t2\t3", encoded);
        }

        [Fact]
        public void PrimitiveArray_Inline_Pipe()
        {
            var obj = new ToonObject
            {
                ["numbers"] = new ToonArray { 1, 2, 3 }
            };
            var options = new ToonEncodeOptions { Delimiter = ToonDelimiter.PIPE };
            var encoded = ToonEncoder.Encode(obj, options);
            Assert.Contains("numbers[3|]: 1|2|3", encoded);
        }

        [Fact]
        public void PrimitiveArray_Empty()
        {
            var obj = new ToonObject
            {
                ["empty"] = new ToonArray()
            };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("empty[0]:", encoded);
        }

        [Fact]
        public void ArrayOfArrays_Primitives()
        {
            var obj = new ToonObject
            {
                ["matrix"] = new ToonArray
                {
                    new ToonArray { 1, 2 },
                    new ToonArray { 3, 4 }
                }
            };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("matrix[2]:", encoded);
            Assert.Contains("- [2]: 1,2", encoded);
            Assert.Contains("- [2]: 3,4", encoded);
        }

        [Fact]
        public void ArrayOfArrays_Empty()
        {
            var obj = new ToonObject
            {
                ["matrix"] = new ToonArray
                {
                    new ToonArray(),
                    new ToonArray { 1 }
                }
            };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("matrix[2]:", encoded);
            Assert.Contains("- [0]:", encoded);
            Assert.Contains("- [1]: 1", encoded);
        }

        [Fact]
        public void TabularArray_UniformObjects()
        {
            var obj = new ToonObject
            {
                ["users"] = new ToonArray
                {
                    new ToonObject { ["id"] = 1, ["name"] = "Alice" },
                    new ToonObject { ["id"] = 2, ["name"] = "Bob" }
                }
            };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("users[2]{id,name}:", encoded);
            Assert.Contains("1,Alice", encoded);
            Assert.Contains("2,Bob", encoded);
        }

        [Fact]
        public void TabularArray_EmptyObjects()
        {
            var obj = new ToonObject
            {
                ["items"] = new ToonArray
                {
                    new ToonObject(),
                    new ToonObject()
                }
            };
            var encoded = ToonEncoder.Encode(obj);
            // Empty objects should use list form
            Assert.Contains("items[2]:", encoded);
        }

        [Fact]
        public void MixedArray_NonUniform()
        {
            var obj = new ToonObject
            {
                ["mixed"] = new ToonArray { 1, "text", true }
            };
            var encoded = ToonEncoder.Encode(obj);
            // Mixed arrays with primitives use inline form
            Assert.Contains("mixed[3]: 1,text,true", encoded);
        }

        [Fact]
        public void Array_StrictMode_CountMismatch_Decoder()
        {
            var input = "numbers[3]: 1, 2"; // Only 2 values, header says 3
            var options = new ToonDecodeOptions { Strict = true };
            Assert.Throws<ToonFormatException>(() => ToonDecoder.Decode(input, options));
        }

        [Fact]
        public void Array_NonStrict_CountMismatch_Tolerant()
        {
            var input = "numbers[3]: 1, 2"; // Only 2 values
            var options = new ToonDecodeOptions { Strict = false };
            var decoded = ToonDecoder.Decode(input, options);
            Assert.NotNull(decoded);
            // Non-strict mode should tolerate count mismatch
        }

        [Fact]
        public void Array_WithQuotedValues()
        {
            var obj = new ToonObject
            {
                ["items"] = new ToonArray { "hello, world", "test" }
            };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("items[2]: \"hello, world\",test", encoded);
        }
    }
}
