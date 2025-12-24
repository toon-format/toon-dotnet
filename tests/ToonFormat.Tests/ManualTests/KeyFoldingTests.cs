using Toon.Format;

namespace Toon.Format.Tests;

// TODO: Remove these tests once generated spec tests are in source control
// used to validate current key folding functionality aligns with spec
public class KeyFoldingTests
{
    [Fact]
    [Trait("Description", "encodes folded chain to primitive (safe mode)")]
    public void EncodesFoldedChainToPrimitiveSafeMode()
    {
        // Arrange
        var input =
            new
            {
                @a =
                new
                {
                    @b =
                    new
                    {
                        @c = 1,
                    }
,
                }
,
            }
            ;

        var expected =
"""
a.b.c: 1
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Safe
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Description", "encodes folded chain with inline array")]
    public void EncodesFoldedChainWithInlineArray()
    {
        // Arrange
        var input =
            new
            {
                @data =
                new
                {
                    @meta =
                    new
                    {
                        @items = new object[] {
                            @"x",
                            @"y",
                        }
,
                    }
,
                }
,
            }
            ;

        var expected =
"""
data.meta.items[2]: x,y
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Safe
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Description", "encodes folded chain with tabular array")]
    public void EncodesFoldedChainWithTabularArray()
    {
        // Arrange
        var input =
            new
            {
                @a =
                new
                {
                    @b =
                    new
                    {
                        @items = new object[] {
                            new
                            {
                                @id = 1,
                                @name = @"A",
                            }
                            ,
                            new
                            {
                                @id = 2,
                                @name = @"B",
                            }
                            ,
                        }
,
                    }
,
                }
,
            }
            ;

        var expected =
"""
a.b.items[2]{id,name}:
  1,A
  2,B
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Safe
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Description", "encodes partial folding with flattenDepth=2")]
    public void EncodesPartialFoldingWithFlattendepth2()
    {
        // Arrange
        var input =
            new
            {
                @a =
                new
                {
                    @b =
                    new
                    {
                        @c =
                        new
                        {
                            @d = 1,
                        }
,
                    }
,
                }
,
            }
            ;

        var expected =
"""
a.b:
  c:
    d: 1
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Safe,
            FlattenDepth = 2,
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Description", "encodes full chain with flattenDepth=Infinity (default)")]
    public void EncodesFullChainWithFlattendepthInfinityDefault()
    {
        // Arrange
        var input =
            new
            {
                @a =
                new
                {
                    @b =
                    new
                    {
                        @c =
                        new
                        {
                            @d = 1,
                        }
,
                    }
,
                }
,
            }
            ;

        var expected =
"""
a.b.c.d: 1
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Safe
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Description", "encodes standard nesting with flattenDepth=0 (no folding)")]
    public void EncodesStandardNestingWithFlattendepth0NoFolding()
    {
        // Arrange
        var input =
            new
            {
                @a =
                new
                {
                    @b =
                    new
                    {
                        @c = 1,
                    }
,
                }
,
            }
            ;

        var expected =
"""
a:
  b:
    c: 1
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Safe,
            FlattenDepth = 0,
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Description", "encodes standard nesting with flattenDepth=1 (no practical effect)")]
    public void EncodesStandardNestingWithFlattendepth1NoPracticalEffect()
    {
        // Arrange
        var input =
            new
            {
                @a =
                new
                {
                    @b =
                    new
                    {
                        @c = 1,
                    }
,
                }
,
            }
            ;

        var expected =
"""
a:
  b:
    c: 1
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Safe,
            FlattenDepth = 1,
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Description", "encodes standard nesting with keyFolding=off (baseline)")]
    public void EncodesStandardNestingWithKeyfoldingOffBaseline()
    {
        // Arrange
        var input =
            new
            {
                @a =
                new
                {
                    @b =
                    new
                    {
                        @c = 1,
                    }
,
                }
,
            }
            ;

        var expected =
"""
a:
  b:
    c: 1
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Off
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Description", "encodes folded chain ending with empty object")]
    public void EncodesFoldedChainEndingWithEmptyObject()
    {
        // Arrange
        var input =
            new
            {
                @a =
                new
                {
                    @b =
                    new
                    {
                        @c =
                        new
                        {
                        }
,
                    }
,
                }
,
            }
            ;

        var expected =
"""
a.b.c:
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Safe
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Description", "stops folding at array boundary (not single-key object)")]
    public void StopsFoldingAtArrayBoundaryNotSingleKeyObject()
    {
        // Arrange
        var input =
            new
            {
                @a =
                new
                {
                    @b = new object[] {
                        1,
                        2,
                    }
,
                }
,
            }
            ;

        var expected =
"""
a.b[2]: 1,2
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Safe
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Description", "encodes folded chains preserving sibling field order")]
    public void EncodesFoldedChainsPreservingSiblingFieldOrder()
    {
        // Arrange
        var input =
            new
            {
                @first =
                new
                {
                    @second =
                    new
                    {
                        @third = 1,
                    }
,
                }
,
                @simple = 2,
                @short =
                new
                {
                    @path = 3,
                }
,
            }
            ;

        var expected =
"""
first.second.third: 1
simple: 2
short.path: 3
""";

        // Act & Assert
        var options = new ToonEncodeOptions
        {
            Delimiter = ToonDelimiter.COMMA,
            Indent = 2,
            KeyFolding = ToonKeyFolding.Safe
        };

        var result = ToonEncoder.Encode(input, options);

        Assert.Equal(expected, result);
    }
}
