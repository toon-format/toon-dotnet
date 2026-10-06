# TOON for .NET

[![SPEC v4.3](https://img.shields.io/badge/spec-v4.3-lightgrey)](https://github.com/toon-format/spec/blob/v4.3.0/SPEC.md)
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
| `IndentSize` | `2` | Spaces per indentation level (encode and decode) |
| `Delimiter` | `ToonDelimiter.COMMA` | Array delimiter: `COMMA`, `TAB`, or `PIPE` (encode) |
| `Strict` | `true` | Enforces the strict-mode errors – count mismatches, blank lines inside arrays, duplicate keys, tab indentation, ill-formed UTF-8 (decode) |

## Specification

Targets [TOON spec v4.3](https://github.com/toon-format/spec/blob/v4.3.0/SPEC.md), and the test suite runs the spec's conformance fixtures.

- **Integers in `long` range decode to `long`, every other number to `double`** – integers beyond `long` range lose precision and a token beyond `double` range (e.g. `1e999`) decodes as a string ([§4](https://github.com/toon-format/spec/blob/v4.3.0/SPEC.md#4-decoding-interpretation-reference-decoder))
- **Every .NET numeric type encodes as a number** – integers in `long` range stay exact, `ulong` values above `long.MaxValue` and `decimal` values round to the nearest `double`, `float` keeps its shortest digits (`0.1f` encodes as `0.1`), `NaN` and `±Infinity` become `null`, `JsonNode` and `JsonElement` values encode as the JSON they hold, dictionaries become objects with string keys, other enumerables become arrays, and any other value encodes as `System.Text.Json` serializes it, honoring its attributes and converters so `Decode<T>` reads it back – a `DateTime` becomes an ISO 8601 string, an enum its number ([§3](https://github.com/toon-format/spec/blob/v4.3.0/SPEC.md#3-encoding-normalization-reference-encoder))
- **Tabs in indentation are a strict-mode error** – in non-strict mode each leading tab counts as one indentation level ([§12](https://github.com/toon-format/spec/blob/v4.3.0/SPEC.md#12-indentation-and-whitespace))

### Migrating from spec v3.0

Spec v4 dropped key folding and path expansion, along with the `KeyFolding`, `FlattenDepth`, and `ExpandPaths` options, and reads a line whose first non-space character is `#` as a comment. Decode documents written with folded keys or `#`-leading lines using a v3.0 build (with `ExpandPaths = ToonPathExpansion.Safe` for folded keys), then encode the result again with this one.

## Resources

- **Specification:** [SPEC.md](https://github.com/toon-format/spec/blob/main/SPEC.md) – Normative rules and conformance checklists
- **Format Overview:** [toonformat.dev](https://toonformat.dev/guide/format-overview) – Every form with examples
- **Other Implementations:** [toonformat.dev](https://toonformat.dev/ecosystem/implementations) – TOON in other languages

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) for the development setup and pull request guidelines.

## License

[MIT](./LICENSE) License © 2025-PRESENT Daniel Destouche and [Johann Schopplich](https://github.com/johannschopplich)
