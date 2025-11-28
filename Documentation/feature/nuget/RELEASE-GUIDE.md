# Release Guide

This guide outlines the process for creating and publishing a new release of ToonFormat.

## Release Workflow

### 1. Preparation
1. **Update Version**:
   - Open `src/ToonFormat/ToonFormat.csproj`.
   - Update the `<Version>` tag to the new version number (following Semantic Versioning).
   - Update `<AssemblyVersion>` and `<FileVersion>` if necessary.

2. **Update Changelog**:
   - Update `CHANGELOG.md` (if present) or prepare release notes listing new features, bug fixes, and breaking changes.

3. **Run Tests**:
   - Execute all tests locally to ensure stability.
   - Run `.\build-nuget.ps1` to verify the package builds correctly.

### 2. Create Release
1. **Commit and Push**:
   - Commit the version bump: `git commit -m "Bump version to x.y.z"`
   - Push to the `main` branch.

2. **Tag and Release on GitHub**:
   - Go to the GitHub repository "Releases" section.
   - Click "Draft a new release".
   - **Tag version**: Create a new tag (e.g., `v1.0.0`).
   - **Target**: `main`.
   - **Release title**: `v1.0.0`.
   - **Description**: Paste the release notes/changelog.
   - Click "Publish release".

### 3. Verification
1. **Monitor CI/CD**:
   - Go to the "Actions" tab in GitHub.
   - Watch the "Publish NuGet Package" workflow.
   - Ensure all steps (Build, Test, Pack, Publish) complete successfully.

2. **Verify on NuGet.org**:
   - Visit the package page on NuGet.org.
   - Confirm the new version is listed and available for download (indexing may take a few minutes).

## Branch Strategy
- **main**: Contains the latest stable code. Releases are tagged from here.
- **develop** (optional): Integration branch for ongoing development.
- **feature/**: Feature branches for specific tasks.

## Quality Checklist
- [ ] All tests pass locally and in CI.
- [ ] Code follows style guidelines.
- [ ] Documentation is updated.
- [ ] Version number is incremented correctly.
- [ ] Release notes are accurate.
