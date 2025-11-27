# TOON Format for .NET

[![NuGet version](https://img.shields.io/nuget/v/Toon.Format.svg)](https://www.nuget.org/packages/Toon.Format/)
[![.NET version](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)

**Token-Oriented Object Notation** is a compact, human-readable format designed for passing structured data to Large Language Models with significantly reduced token usage.

## Why TOON?

- **60-80% Token Reduction** for tabular data compared to JSON
- **Human-Readable** structure without sacrificing machine parseability
- **Type-Safe** with full .NET API support
- **Spec Compliant** implementation of [TOON v3.0](./SPEC.md)
- **Production Ready** with strict validation and comprehensive test coverage

### Example

**JSON** (verbose, 78 tokens):
```json
{
  "users": [
    { "id": 1, "name": "Alice", "role": "admin" },
    { "id": 2, "name": "Bob", "role": "user" }
  ]
}
```

**TOON** (compact, 24 tokens):
```
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
```

## Features

- 🎯 **Tabular Arrays** - Compact representation of uniform object arrays
- 🔄 **Path Expansion** - Expand dotted keys into nested objects (`a.b.c: 1` → `{a: {b: {c: 1}}}`)
- 🔑 **Key Folding** - Collapse nested objects into dotted notation
- ✔️ **Strict Mode** - Comprehensive validation for production use
- 🎨 **Multiple Delimiters** - Support for comma, tab, and pipe delimiters
- 📦 **Zero Dependencies** - Built on `System.Text.Json`
- 📚 **IntelliSense Support** - Full XML documentation with examples
- 🧪 **90+ Tests** - Comprehensive test coverage for spec compliance

## Installation

```bash
dotnet add package Toon.Format
```

## Quick Start

```csharp
using Toon.Format;

// Create data using the fluent ToonValue API
var data = new ToonObject
{
    ["name"] = "Alice",
    ["age"] = 30,
    ["tags"] = new ToonArray { "admin", "developer" }
};

// Encode to TOON format
string toon = ToonEncoder.Encode(data);
// Output:
// name: Alice
// age: 30
// tags[2]: admin,developer

// Decode back to objects
ToonObject decoded = ToonDecoder.DecodeToonObject(toon);
Console.WriteLine(decoded["name"]); // Alice
```

## API Reference

### ToonValue Type Hierarchy

TOON provides a strongly-typed API for building and manipulating data:

```csharp
// Base type for all TOON values
ToonValue
├── ToonObject    // Dictionary-like: { key: value, ... }
├── ToonArray     // List-like: [item1, item2, ...]
└── ToonPrimitive // string | number | boolean | null
```

#### ToonObject - Key-Value Dictionaries

```csharp
var user = new ToonObject
{
    ["id"] = 123,
    ["name"] = "Alice",
    ["email"] = "alice@example.com",
    ["settings"] = new ToonObject
    {
        ["theme"] = "dark",
        ["notifications"] = true
    }
};

// Access values
string name = user["name"].GetValue<string>();
ToonObject settings = (ToonObject)user["settings"];

// Iterate
foreach (var kvp in user)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

#### ToonArray - Ordered Collections

```csharp
// Primitive array (inline format)
var tags = new ToonArray { "admin", "ops", "dev" };
// Encodes as: tags[3]: admin,ops,dev

// Array of objects (tabular format)
var users = new ToonArray
{
    new ToonObject { ["id"] = 1, ["name"] = "Alice", ["active"] = true },
    new ToonObject { ["id"] = 2, ["name"] = "Bob", ["active"] = false }
};
// Encodes as:
// users[2]{id,name,active}:
//   1,Alice,true
//   2,Bob,false

// Mixed array (expanded format)
var mixed = new ToonArray
{
    42,
    "text",
    new ToonObject { ["key"] = "value" }
};
// Encodes as:
// mixed[3]:
//   - 42
//   - text
//   - key: value
```

#### ToonPrimitive - Basic Values

```csharp
// Implicit conversions
ToonValue str = "Hello";
ToonValue num = 42;
ToonValue dbl = 3.14;
ToonValue flag = true;
ToonValue nothing = new ToonPrimitive(); // null

// Type-safe retrieval
var primitive = new ToonPrimitive("test");
string value = primitive.GetValue<string>();      // "test"
int? asInt = primitive.GetValue<int?>();          // null (type mismatch)
```

## Encoder Options

Control encoding behavior with `ToonEncodeOptions`:

```csharp
var options = new ToonEncodeOptions
{
    // Indentation (default: 2 spaces)
    Indent = 2,

    // Delimiter for arrays and tabular data
    Delimiter = ToonDelimiter.COMMA,  // COMMA | TAB | PIPE

    // Use [#N] length markers instead of [N]
    LengthMarker = false,

    // Key folding mode
    KeyFolding = KeyFolding.Safe,     // Off | Safe

    // Maximum nesting depth for key folding
    FlattenDepth = int.MaxValue
};

string encoded = ToonEncoder.Encode(data, options);
```

### Key Folding

Collapse nested single-key objects into dotted notation:

```csharp
var options = new ToonEncodeOptions { KeyFolding = KeyFolding.Safe };

var data = new ToonObject
{
    ["server"] = new ToonObject
    {
        ["config"] = new ToonObject
        {
            ["port"] = 8080
        }
    }
};

string encoded = ToonEncoder.Encode(data, options);
// Output: server.config.port: 8080
```

**With flatten depth control:**

```csharp
var options = new ToonEncodeOptions
{
    KeyFolding = KeyFolding.Safe,
    FlattenDepth = 2  // Only fold 2 levels
};

var data = new ToonObject
{
    ["a"] = new ToonObject
    {
        ["b"] = new ToonObject
        {
            ["c"] = new ToonObject
            {
                ["d"] = 1
            }
        }
    }
};

string encoded = ToonEncoder.Encode(data, options);
// Output:
// a.b:
//   c:
//     d: 1
```

## Decoder Options

Control decoding behavior with `ToonDecodeOptions`:

```csharp
var options = new ToonDecodeOptions
{
    // Expected indentation size (default: 2)
    Indent = 2,

    // Enable strict validation (default: true)
    Strict = true,

    // Path expansion mode
    ExpandPaths = ExpandPaths.Safe  // Off | Safe
};

JsonNode decoded = ToonDecoder.Decode(toonString, options);
```

### Path Expansion

Expand dotted keys into nested objects:

```csharp
var options = new ToonDecodeOptions { ExpandPaths = ExpandPaths.Safe };

var toon = @"
server.host: localhost
server.port: 8080
server.ssl.enabled: true
";

ToonObject decoded = ToonDecoder.DecodeToonObject(toon, options);
// Result:
// {
//   "server": {
//     "host": "localhost",
//     "port": 8080,
//     "ssl": {
//       "enabled": true
//     }
//   }
// }
```

**Deep merge behavior:**

```csharp
var toon = @"
user.profile.name: Alice
user.profile.email: alice@example.com
user.settings.theme: dark
";

// Multiple dotted paths merge into nested structure
var decoded = ToonDecoder.DecodeToonObject(toon, options);
// Result: nested object with user.profile and user.settings
```

### Strict Mode

Enable comprehensive validation (enabled by default):

```csharp
var options = new ToonDecodeOptions { Strict = true };

// Validates:
// ✓ Array lengths match declared counts
// ✓ Tabular row counts match headers
// ✓ Tabular row widths match field counts
// ✓ No blank lines inside arrays/tables
// ✓ Indentation is exact multiples of indent size
// ✓ No tabs for indentation (tabs allowed as delimiter)
// ✓ Delimiter consistency between headers and data
// ✓ Valid escape sequences only (\\, \", \n, \r, \t)
// ✓ Path expansion conflicts (when ExpandPaths is enabled)

try
{
    var decoded = ToonDecoder.Decode(toonString, options);
}
catch (ToonFormatException ex)
{
    Console.WriteLine($"Validation error: {ex.Message}");
}
```

## Working with Delimiters

TOON supports three delimiters: comma (default), tab, and pipe.

### Comma Delimiter (Default)

```csharp
var data = new ToonObject
{
    ["items"] = new ToonArray
    {
        new ToonObject { ["id"] = 1, ["name"] = "Item A" },
        new ToonObject { ["id"] = 2, ["name"] = "Item B" }
    }
};

string encoded = ToonEncoder.Encode(data);
// Output:
// items[2]{id,name}:
//   1,Item A
//   2,Item B
```

### Tab Delimiter

Useful for data containing commas:

```csharp
var options = new ToonEncodeOptions { Delimiter = ToonDelimiter.TAB };

var data = new ToonObject
{
    ["products"] = new ToonArray
    {
        new ToonObject { ["sku"] = "A1", ["desc"] = "Widget, blue" },
        new ToonObject { ["sku"] = "B2", ["desc"] = "Gadget, red" }
    }
};

string encoded = ToonEncoder.Encode(data, options);
// Output (tab-delimited):
// products[2	]{sku	desc}:
//   A1	Widget, blue
//   B2	Gadget, red
```

### Pipe Delimiter

Alternative for data with commas and tabs:

```csharp
var options = new ToonEncodeOptions { Delimiter = ToonDelimiter.PIPE };

string encoded = ToonEncoder.Encode(data, options);
// Output:
// products[2|]{sku|desc}:
//   A1|Widget, blue
//   B2|Gadget, red
```

## Advanced Examples

### Encoding Complex Nested Structures

```csharp
var company = new ToonObject
{
    ["name"] = "Acme Corp",
    ["founded"] = 2020,
    ["departments"] = new ToonArray
    {
        new ToonObject
        {
            ["name"] = "Engineering",
            ["employees"] = new ToonArray
            {
                new ToonObject { ["id"] = 1, ["name"] = "Alice", ["role"] = "Developer" },
                new ToonObject { ["id"] = 2, ["name"] = "Bob", ["role"] = "Designer" }
            }
        },
        new ToonObject
        {
            ["name"] = "Sales",
            ["employees"] = new ToonArray
            {
                new ToonObject { ["id"] = 3, ["name"] = "Charlie", ["role"] = "Manager" }
            }
        }
    }
};

string toon = ToonEncoder.Encode(company);
// Output:
// name: Acme Corp
// founded: 2020
// departments[2]:
//   - name: Engineering
//     employees[2]{id,name,role}:
//       1,Alice,Developer
//       2,Bob,Designer
//   - name: Sales
//     employees[1]{id,name,role}:
//       3,Charlie,Manager
```

### Round-Trip Conversion

```csharp
// Start with JSON
string json = @"{
  ""users"": [
    { ""id"": 1, ""name"": ""Alice"" },
    { ""id"": 2, ""name"": ""Bob"" }
  ]
}";

// Convert JSON → TOON
var jsonNode = JsonNode.Parse(json);
string toon = ToonEncoder.Encode(jsonNode);

// Convert TOON → JSON
var decoded = ToonDecoder.Decode(toon);
string jsonResult = decoded.ToJsonString(new JsonSerializerOptions
{
    WriteIndented = true
});

// jsonResult matches original structure
```

### Working with System.Text.Json Integration

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;

// From JSON string
var jsonNode = JsonNode.Parse(@"{""name"": ""Alice"", ""age"": 30}");
string toon = ToonEncoder.Encode(jsonNode);

// To JSON string
var decoded = ToonDecoder.Decode(toon);
string json = decoded.ToJsonString();

// From .NET objects
var person = new { Name = "Alice", Age = 30 };
var serialized = JsonSerializer.SerializeToNode(person);
string toonFromObject = ToonEncoder.Encode(serialized);
```

## Testing & Quality

The implementation includes comprehensive test coverage:

- **90+ Unit Tests** covering all spec sections
- **Conformance Tests** validating spec compliance
- **Edge Case Tests** for boundary conditions
- **Zero Warnings** in release builds
- **SPEC v3.0 Validation** for all encoding/decoding operations

Run tests:
```bash
dotnet test
```

Run with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Documentation

All public APIs include comprehensive XML documentation with usage examples. IntelliSense will show:

- Method signatures and parameter descriptions
- Code examples for common scenarios
- Links to SPEC sections for detailed behavior

Example IntelliSense for `ToonObject`:
```csharp
/// <summary>
/// Represents a TOON object (key-value dictionary).
/// </summary>
/// <example>
/// <code>
/// var user = new ToonObject
/// {
///     ["id"] = 123,
///     ["name"] = "Alice"
/// };
/// </code>
/// </example>
```

## Resources

- **[TOON Specification v3.0](./SPEC.md)** - Complete specification (included in repo)
- **[Main Repository](https://github.com/toon-format/toon)** - Reference TypeScript implementation
- **[Benchmarks](https://github.com/toon-format/toon#benchmarks)** - Performance comparisons
- **[Other Implementations](https://github.com/toon-format/toon#other-implementations)** - Go, Python, Rust, etc.
- **[Contributing Guide](./CONTRIBUTING.md)** - How to contribute

## Contributing

Contributions are welcome! This project follows standard .NET conventions and TOON Specification v3.0.

Before contributing:
1. Read [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines
2. Review [SPEC.md](./SPEC.md) for specification details
3. Check [.docs/CONTRIBUTING_SPEC_ALIGNMENT.md](./.docs/CONTRIBUTING_SPEC_ALIGNMENT.md) for alignment status
4. Ensure all tests pass: `dotnet test`
5. Verify formatting: `dotnet format --verify-no-changes`

### Development Setup

```bash
# Clone the repository
git clone https://github.com/toon-format/toon-dotnet.git
cd toon-dotnet

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## License

MIT License © 2025-PRESENT [Johann Schopplich](https://github.com/johannschopplich)

See [LICENSE](./LICENSE) for details.