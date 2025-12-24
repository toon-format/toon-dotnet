# TOON Format for .NET

[![NuGet version](https://img.shields.io/nuget/v/Toon.Format.svg)](https://www.nuget.org/packages/Toon.Format/)
[![.NET version](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)

**Token-Oriented Object Notation** is a compact, human-readable encoding of the JSON data model that minimizes tokens and makes structure easy for models to follow. Combines YAML-like indentation with CSV-like tabular arrays. Fully compatible with the [official TOON specification v3.0](https://github.com/toon-format/spec).

**Key Features:** Minimal syntax • TOON Encoding and Decoding • Tabular arrays for uniform data • Path expansion • Strict mode validation • .NET 8.0, 9.0 and 10.0 • 520+ tests with 99.7% spec coverage.

## Quick Start

```csharp
using Toon.Format;

var data = new
{
    users = new[]
    {
        new { id = 1, name = "Alice", role = "admin" },
        new { id = 2, name = "Bob", role = "user" }
    }
};

Console.WriteLine(ToonEncoder.Encode(data));
```

**Output:**

```
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
```

**Compared to JSON (30-60% token reduction):**

```json
{
  "users": [
    { "id": 1, "name": "Alice", "role": "admin" },
    { "id": 2, "name": "Bob", "role": "user" }
  ]
}
```

## Installation

```bash
dotnet add package Toon.Format
```

## Type Conversions

.NET-specific types are automatically normalized for LLM-safe output:

| Input Type | Output |
| --- | --- |
| Number (finite) | Decimal form; `-0` → `0`; no scientific notation |
| Number (`NaN`, `±Infinity`) | `null` |
| `decimal`, `double`, `float` | Decimal number |
| `DateTime`, `DateTimeOffset` | ISO 8601 string in quotes |
| `Guid` | String in quotes |
| `IDictionary<,>`, `Dictionary<,>` | Object with string keys |
| `IEnumerable<>`, arrays | Arrays |
| Nullable types | Unwrapped value or `null` |

## API

### `ToonEncoder.Encode(object value): string`

### `ToonEncoder.Encode(object value, ToonEncodeOptions options): string`

Converts any .NET object to TOON format.

**Parameters:**

- `value` – Any .NET object (class, record, dictionary, list, or primitive). Non-serializable values are converted to `null`. DateTime types are converted to ISO strings.
- `options` – Optional encoding options:
  - `Indent` – Number of spaces per indentation level (default: `2`)
  - `Delimiter` – Delimiter for array values: `ToonDelimiter.COMMA` (default), `TAB`, or `PIPE`
  - `KeyFolding` – Collapse nested single-key objects: `ToonKeyFolding.Off` or `Safe` (default: `Off`)

**Returns:**

A TOON-formatted string with no trailing newline or spaces.

**Example:**

```csharp
using Toon.Format;

record Item(string Sku, int Qty, double Price);
record Data(List<Item> Items);

var item1 = new Item("A1", 2, 9.99);
var item2 = new Item("B2", 1, 14.5);
var data = new Data(new List<Item> { item1, item2 });

Console.WriteLine(ToonEncoder.Encode(data));
```

**Output:**

```
Items[2]{Sku,Qty,Price}:
  A1,2,9.99
  B2,1,14.5
```

#### Delimiter Options

Alternative delimiters can provide additional token savings:

**Tab Delimiter:**

```csharp
var options = new ToonEncodeOptions
{
    Delimiter = ToonDelimiter.TAB
};
Console.WriteLine(ToonEncoder.Encode(data, options));
```

**Output:**

```
Items[2	]{Sku	Qty	Price}:
  A1	2	9.99
  B2	1	14.5
```

**Pipe Delimiter:**

```csharp
var options = new ToonEncodeOptions
{
    Delimiter = ToonDelimiter.PIPE
};
Console.WriteLine(ToonEncoder.Encode(data, options));
```

**Output:**

```
Items[2|]{Sku|Qty|Price}:
  A1|2|9.99
  B2|1|14.5
```

#### Key Folding

Collapse nested single-key objects for more compact output:

```csharp
var data = new { user = new { profile = new { name = "Alice" } } };

var options = new ToonEncodeOptions
{
    KeyFolding = ToonKeyFolding.Safe
};
Console.WriteLine(ToonEncoder.Encode(data, options));
// Output: user.profile.name: Alice
```

### `ToonDecoder.Decode(string toon): JsonNode`

### `ToonDecoder.Decode(string toon, ToonDecodeOptions options): JsonNode`

### `ToonDecoder.Decode<T>(string toon): T`

### `ToonDecoder.Decode<T>(string toon, ToonDecodeOptions options): T`

Converts TOON-formatted strings back to .NET objects.

**Parameters:**

- `toon` – TOON-formatted input string
- `options` – Optional decoding options:
  - `Indent` – Number of spaces per indentation level (default: `2`)
  - `Strict` – Enable validation mode (default: `true`). When `true`, throws `ToonFormatException` on invalid input.
  - `ExpandPaths` – Expand dotted keys: `ToonPathExpansion.Off` (default) or `ToonPathExpansion.Safe`

**Returns:**

For generic overloads: Returns a `JsonNode` (JsonObject, JsonArray, or JsonValue) or deserialized type `T`.

**Example:**

```csharp
using Toon.Format;

string toon = """
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
""";

// Decode to JsonNode
var result = ToonDecoder.Decode(toon);

// Decode to specific type
var users = ToonDecoder.Decode<List<User>>(toon);
```

#### Path Expansion

Expand dotted keys into nested objects:

```csharp
string toon = "a.b.c: 1";

var options = new ToonDecodeOptions
{
    ExpandPaths = ToonPathExpansion.Safe
};

var result = ToonDecoder.Decode(toon, options);
// Result: { "a": { "b": { "c": 1 } } }
```

#### Round-Trip Conversion

```csharp
using Toon.Format;

// Original data
var data = new
{
    id = 123,
    name = "Ada",
    tags = new[] { "dev", "admin" }
};

// Encode to TOON
string toon = ToonEncoder.Encode(data);

// Decode back to objects
var decoded = ToonDecoder.Decode(toon);

// Or decode to specific type
var typed = ToonDecoder.Decode<MyType>(toon);
```

For more examples and options, see the [tests](./tests/ToonFormat.Tests/).

## Project Status

**This project is 100% compliant with TOON Specification v3.0**

This implementation:
- Passes 370+ specification tests (100% coverage)
- Supports all TOON v3.0 features
- Handles all edge cases and strict mode validations
- Fully documented with XML comments
- Production-ready for .NET 8.0, .NET 9.0 and .NET 10.0

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## Documentation

- [📘 TOON Specification v3.0](https://github.com/toon-format/spec/blob/main/SPEC.md) - Official specification
- [🔧 API Tests](./tests/ToonFormat.Tests/) - Comprehensive test suite with examples
- [📋 Project Plan](SPEC_V3_PROJECT_PLAN.md) - Implementation details and compliance checklist
- [🤝 Contributing](CONTRIBUTING.md) - Contribution guidelines
- [🏠 Main Repository](https://github.com/toon-format/toon) - TOON format home
- [📊 Benchmarks](https://github.com/toon-format/toon#benchmarks) - Performance comparisons
- [🌐 Other Implementations](https://github.com/toon-format/toon#other-implementations) - TypeScript, Java, Python, etc.

## Contributing

Interested in contributing? Check out the [specification](https://github.com/toon-format/spec/blob/main/SPEC.md) and [contribution guidelines](CONTRIBUTING.md)!

## License

MIT License © 2025-PRESENT [Johann Schopplich](https://github.com/johannschopplich)
