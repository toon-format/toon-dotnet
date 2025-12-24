using Toon.Format.Internal;

namespace Toon.Format.Tests;

public class SpanExtensionsTests
{
#if !NET9_0_OR_GREATER
    [Fact]
    public void SpanExtensions_StartsWith_ReturnsTrue()
    {
        // Arrange
        ReadOnlySpan<char> span = "Hello, World!".AsSpan();
        char prefix = 'H';

        // Act
        bool result = SpanExtensions.StartsWith(span, prefix);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SpanExtensions_EndsWith_ReturnsTrue()
    {
        // Arrange
        ReadOnlySpan<char> span = "Hello, World!".AsSpan();
        char suffix = '!';

        // Act
        bool result = SpanExtensions.EndsWith(span, suffix);

        // Assert
        Assert.True(result);
    }
#endif

    [Fact]
    public void SpanExtensions_IndexOf_ReturnsCorrectIndex()
    {
        // 3rd l index is 10, starting from the 5th index we'll
        // omit the first two l characters
        // Arrange
        ReadOnlySpan<char> span = "Hello, World!".AsSpan();
        char value = 'l';

        // Act
        int index = SpanExtensions.IndexOf(span, value, 5);

        // Assert
        Assert.Equal(10, index);
    }
}