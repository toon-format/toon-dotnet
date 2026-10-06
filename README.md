# TOON for .NET

[![SPEC v3.0](https://img.shields.io/badge/spec-v3.0-lightgrey)](https://github.com/toon-format/spec/blob/v3.0.0/SPEC.md)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)

Encodes .NET values to [TOON (Token-Oriented Object Notation)](https://github.com/toon-format/toon) and decodes TOON back. TOON is a compact, indentation-based encoding of the JSON data model for LLM input.

## Installation

```bash
dotnet add package Toon.Format
```

`Toon.Format` is not on NuGet yet ([#29](https://github.com/toon-format/toon-dotnet/issues/29)) – until the first release, reference `src/ToonFormat` from a clone of this repository.

## Usage

```csharp
using Toon.Format;

var data = new
{
    users = new[]
    {
        new { id = 1, name = "Ada", role = "admin" },
        new { id = 2, name = "Bob", role = "user" }
    }
};

string toon = ToonEncoder.Encode(data);
// users[2]{id,name,role}:
//   1,Ada,admin
//   2,Bob,user

var node = ToonDecoder.Decode(toon);
// {"users":[{"id":1,"name":"Ada","role":"admin"},{"id":2,"name":"Bob","role":"user"}]}
```

`ToonDecoder.Decode` returns a `JsonNode`; `ToonDecoder.Decode<T>` deserializes into `T` through `System.Text.Json`. Pass a `ToonEncodeOptions` or `ToonDecodeOptions` as the second argument:

| Option | Default | Description |
| ------ | ------- | ----------- |
| `Indent` | `2` | Spaces per indentation level (encode and decode) |
| `Delimiter` | `ToonDelimiter.COMMA` | Array delimiter: `COMMA`, `TAB`, or `PIPE` (encode) |
| `Strict` | `true` | Throw `ToonFormatException` on count mismatches and invalid input (decode) |
| `ExpandPaths` | `ToonPathExpansion.Off` | `Safe` expands dotted keys into nested objects (decode) |

## Specification

Targets [TOON spec v3.0](https://github.com/toon-format/spec/blob/v3.0.0/SPEC.md), and the test suite runs the spec's conformance fixtures.

- **Numbers decode to `double`** – a token beyond `double` range stays a string and integers beyond 2^53 lose precision ([§4](https://github.com/toon-format/spec/blob/v3.0.0/SPEC.md#4-decoding-interpretation-reference-decoder))
- **`int`, `long`, and `double` encode as numbers** – `NaN` and `±Infinity` become `null`, `DateTime` and `DateTimeOffset` become ISO 8601 strings, dictionaries become objects with string keys, other enumerables become arrays, and public properties of other objects become fields ([§3](https://github.com/toon-format/spec/blob/v3.0.0/SPEC.md#3-encoding-normalization-reference-encoder))

## Resources

- **Specification:** [SPEC.md](https://github.com/toon-format/spec/blob/main/SPEC.md) – Normative rules and conformance checklists
- **Format Overview:** [toonformat.dev](https://toonformat.dev/guide/format-overview) – Every form with examples
- **Other Implementations:** [toonformat.dev](https://toonformat.dev/ecosystem/implementations) – TOON in other languages

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) for the development setup and pull request guidelines.

## License

[MIT](./LICENSE) License © 2025-PRESENT Daniel Destouche and [Johann Schopplich](https://github.com/johannschopplich)
