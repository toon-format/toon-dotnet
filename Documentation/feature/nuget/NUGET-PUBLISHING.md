# NuGet Publishing Guide

This document describes how to publish the ToonFormat library to NuGet.org.

## Prerequisites

1. A NuGet.org account.
2. An API Key from NuGet.org with "Push" permissions for the `ToonFormat` package.
3. For local publishing: .NET SDK 8.0 or later.

## Configuration

The package metadata is configured in `src/ToonFormat/ToonFormat.csproj`. Key properties include:
- `PackageId`: ToonFormat
- `Version`: Controlled via the `<Version>` tag or build properties.
- `Authors`: ToonFormat Contributors
- `Description`: A .NET implementation of the Toon data format.
- `RepositoryUrl`: Links to the GitHub repository.

## Publishing Methods

### 1. Automated Publishing (Recommended)

The project is configured with GitHub Actions to automatically publish to NuGet.org when a new release is created.

**Steps:**
1. Update the version number in `src/ToonFormat/ToonFormat.csproj`.
2. Commit and push the change.
3. Create a new Release in GitHub with a tag matching the version (e.g., `v1.0.0`).
4. The `publish-nuget.yml` workflow will trigger, build the package, run tests, and publish to NuGet.org.

**Setup Required:**
- Add a repository secret named `NUGET_API_KEY` containing your NuGet.org API key.

### 2. Local Publishing

You can build and publish the package locally using the provided PowerShell script.

**Build and Pack only:**
```powershell
.\build-nuget.ps1
```
This creates the `.nupkg` and `.snupkg` files in the `artifacts/` directory.

**Build, Pack, and Publish:**
```powershell
.\build-nuget.ps1 -Publish -ApiKey "YOUR_API_KEY"
```

**Parameters:**
- `-Publish`: Triggers the push to the NuGet source.
- `-ApiKey`: Your NuGet API key. Can also be set via `NUGET_API_KEY` environment variable.
- `-Source`: The NuGet source URL (default: `https://api.nuget.org/v3/index.json`). Useful for testing with local feeds or test.nuget.org.
- `-Configuration`: Build configuration (default: `Release`).

## Package Verification

Before publishing, you can verify the package content:
1. Run `.\build-nuget.ps1` to generate the package.
2. Use `NuGet Package Explorer` or inspect the `artifacts/*.nupkg` file as a zip archive.
3. Verify that:
   - The DLLs are in the correct `lib/` folders.
   - `README.md` and `LICENSE` are included.
   - Symbols are generated if expected.

## Troubleshooting

**Error: 403 Forbidden**
- Check that your API Key is valid and has not expired.
- Ensure the API Key has permissions for the `ToonFormat` package.

**Error: Package already exists**
- NuGet.org packages are immutable. You must increment the version number in `ToonFormat.csproj` before publishing a new version.
