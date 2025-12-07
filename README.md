# TOON Format for .NET

[![NuGet version](https://img.shields.io/nuget/v/Toon.Format.svg)](https://www.nuget.org/packages/Toon.Format/)
[![.NET version](https://img.shields.io/badge/.NET-Standard%202.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)

**Token-Oriented Object Notation** is a compact, human-readable format designed for passing structured data to Large Language Models with significantly reduced token usage.

## Status

🚧 **This package is currently a namespace reservation.** Full implementation coming soon!

### Example

**JSON** (verbose):
```json
{
  "users": [
    { "id": 1, "name": "Alice", "role": "admin" },
    { "id": 2, "name": "Bob", "role": "user" }
  ]
}
```

**TOON** (compact):
```
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
```

## Resources

- [TOON Specification](https://github.com/toon-format/spec/blob/main/SPEC.md)
- [Main Repository](https://github.com/toon-format/toon)
- [Benchmarks & Performance](https://github.com/toon-format/toon#benchmarks)
- [Other Language Implementations](https://github.com/toon-format/toon#other-implementations)

## Future Usage

Once implemented, the package will provide:

```csharp
using Toon.Format;

var data = // your data structure
var toonString = ToonEncoder.Encode(data);
var decoded = ToonDecoder.Decode(toonString);
```

## Contributing

Interested in implementing TOON for .NET? Check out the [specification](https://github.com/toon-format/spec/blob/main/SPEC.md) and feel free to contribute!

## License

MIT License © 2025-PRESENT [Johann Schopplich](https://github.com/johannschopplich)
