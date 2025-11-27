using System;
using Toon.Format;
using Xunit;

namespace ToonFormat.Tests
{
    public class NumberHandlingTests
    {
        [Fact]
        public void Number_Integer()
        {
            var obj = new ToonObject { ["value"] = 42 };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("value: 42", encoded);
        }

        [Fact]
        public void Number_Float()
        {
            var obj = new ToonObject { ["value"] = 3.14 };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("value: 3.14", encoded);
        }

        [Fact]
        public void Number_NegativeZero_Normalized()
        {
            var obj = new ToonObject { ["value"] = -0.0 };
            var encoded = ToonEncoder.Encode(obj);
            // -0 should be normalized to 0
            Assert.Contains("value: 0", encoded);
            Assert.DoesNotContain("value: -0", encoded);
        }

        [Fact]
        public void Number_NegativeInteger()
        {
            var obj = new ToonObject { ["value"] = -42 };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("value: -42", encoded);
        }

        [Fact]
        public void Number_Zero()
        {
            var obj = new ToonObject { ["value"] = 0 };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("value: 0", encoded);
        }

        [Fact]
        public void Number_LargeInteger()
        {
            var obj = new ToonObject { ["value"] = 9007199254740991L }; // Max safe integer
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("value: 9007199254740991", encoded);
        }

        [Fact]
        public void Number_SmallFloat()
        {
            var obj = new ToonObject { ["value"] = 0.0001 };
            var encoded = ToonEncoder.Encode(obj);
            Assert.Contains("value: 0.0001", encoded);
        }

        [Fact]
        public void Number_Decoding_Integer()
        {
            var input = "value: 42";
            var decoded = ToonDecoder.DecodeToonObject(input);
            var value = ((ToonPrimitive)decoded["value"]!).AsDouble();
            Assert.Equal(42.0, value);
        }

        [Fact]
        public void Number_Decoding_Float()
        {
            var input = "value: 3.14";
            var decoded = ToonDecoder.DecodeToonObject(input);
            var value = ((ToonPrimitive)decoded["value"]!).AsDouble();
            Assert.Equal(3.14, value);
        }

        [Fact]
        public void Number_Decoding_Negative()
        {
            var input = "value: -42";
            var decoded = ToonDecoder.DecodeToonObject(input);
            var value = ((ToonPrimitive)decoded["value"]!).AsDouble();
            Assert.Equal(-42.0, value);
        }
    }
}
