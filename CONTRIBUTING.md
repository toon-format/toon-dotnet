# Contributing to toon-dotnet

Thank you for your interest in contributing to the official .NET implementation of TOON!

## Project Setup

This project uses the .NET SDK for building and testing.

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

## Development Workflow

1. **Fork the repository** and create a feature branch
2. **Make your changes** following the coding standards below
3. **Add tests** for any new functionality
4. **Ensure all tests pass** and coverage remains high
5. **Submit a pull request** with a clear description

## Coding Standards

### .NET Version Support

We target .NET 8.0 and .NET 9.0 for broad compatibility.

### Type Safety

- Use nullable reference types
- Avoid `dynamic` where possible
- Leverage C# language features appropriately

### Code Style

- We follow standard .NET conventions
- Use `dotnet format` for consistent formatting:
  ```bash
  dotnet format
  ```
- Consider enabling code analyzers in your IDE

### Testing

- All new features must include tests
- Maintain test coverage at **85%+ line coverage**
- Tests should cover edge cases and spec compliance
- Run the full test suite:
  ```bash
  dotnet test
  ```

Some tests are auto-generated to comply with the [TOON specification](https://github.com/toon-format/spec/blob/main/SPEC.md). To ensure tests are aligned with the spec, execute the `specgen.sh` or `specgen.ps1` script.

## SPEC Compliance

All implementations must comply with the [TOON specification](https://github.com/toon-format/spec/blob/main/SPEC.md).

Before submitting changes that affect encoding/decoding behavior:
1. Verify against the official SPEC.md
2. Add tests for the specific spec sections you're implementing
3. Document any spec version requirements

## Pull Request Guidelines

- **Title**: Use a clear, descriptive title
- **Description**: Explain what changes you made and why
- **Tests**: Include tests for your changes
- **Documentation**: Update README or documentation if needed
- **Commits**: Use clear commit messages ([Conventional Commits](https://www.conventionalcommits.org/) preferred)

Your pull request will use our standard template which guides you through the required information.

## Communication

- **GitHub Issues**: For bug reports and feature requests
- **GitHub Discussions**: For questions and general discussion
- **Pull Requests**: For code reviews and implementation discussion

## Maintainers

This is a collaborative project. Current maintainers:

- [@ghost1face](https://github.com/ghost1face)
- [@239573049](https://github.com/239573049)

All maintainers have equal and consensual decision-making power. For major architectural decisions, please open a discussion issue first.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
