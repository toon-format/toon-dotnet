using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Toon.Format;
using Xunit;

namespace Toon.Format.Tests.Encode;

[Trait("Category", "encode")]
public class ArraysObjectsManual
{
    [Fact]
    [Trait("Description", "deeply nested list items with tabular first field")]
    public void DeeplyNestedListItemsWithTabularFirstField()
    {
        // Arrange
        var input =
            new
            {
                @data = new object[] {
                    new
                    {
                        @items = new object[] {
                            new
                            {
                                @users = new object[] {
                                    new { @id = 1, @name = "Ada" },
                                    new { @id = 2, @name = "Bob" },
                                },
                                @status = "active"
                            }
                        }
                    }
                }
            };

        var expected =
"""
data[1]:
  - items[1]:
      - users[2]{id,name}:
          1,Ada
          2,Bob
        status: active
""";

        // Act & Assert
        var result = ToonEncoder.Encode(input);

        Assert.Equal(expected, result);
    }
}
