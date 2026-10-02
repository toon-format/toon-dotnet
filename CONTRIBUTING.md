# Contributing to toon-dotnet

## Development Setup

Building needs the .NET 10 SDK – the library targets `netstandard2.0` and `net10.0`. On Windows, tests also run on `net481`, which needs the .NET Framework 4.8.1 developer pack.

```bash
git clone --recurse-submodules https://github.com/toon-format/toon-dotnet.git
cd toon-dotnet
dotnet restore
dotnet build
dotnet test
```

`SpecFixtureTests` runs the fixtures from the `tests/spec` submodule, pinned to the spec tag this port targets. To move to a later spec, bump the submodule and update `tests/ToonFormat.Tests/known-failures.txt`.

## Coding Standards

- Nullable reference types are enabled; avoid `dynamic`.
- Format with `dotnet format`.

## Pull Requests

Add tests for every behavior change and use [Conventional Commits](https://www.conventionalcommits.org/) for commit messages. Changes to the format itself belong in [toon-format/spec](https://github.com/toon-format/spec).

## Maintainers

- [@ghost1face](https://github.com/ghost1face)
- [@239573049](https://github.com/239573049)

## License

By contributing, you agree that your contributions are licensed under the MIT License.
