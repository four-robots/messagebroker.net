# Mr-Version Automated Versioning Workflow

This document explains how DotGnatly uses Mr-Version for automated semantic versioning and repository tagging in the NuGet package workflow.

## Overview

The NuGet package workflow (`nuget-package.yml`) integrates with **Mr-Version** to:
- Automatically calculate semantic versions based on git history
- Apply versions to all NuGet packages
- Tag the repository with version tags after successful builds
- Ensure consistent versioning across all packages

## Mr-Version Configuration

The version configuration is stored in `mr-version.yml` at the repository root:

```yaml
baseVersion: "0.1.0"
prereleaseType: none  # Options: none, alpha, beta, rc
defaultIncrement: patch
```

### Configuration Properties

- **baseVersion**: The starting version for the project (e.g., "0.1.0", "1.0.0")
- **prereleaseType**: Type of prerelease (none, alpha, beta, rc)
- **defaultIncrement**: Default version increment (major, minor, patch)

## How It Works

### 1. Version Calculation

The workflow includes a `calculate-version` job that:
- Installs Mr-Version as a .NET global tool
- Analyzes git history to determine the next version
- Outputs version components:
  - `Version` - Full semantic version (e.g., "1.2.3")
  - `AssemblyVersion` - Assembly version (e.g., "1.2.0.0")
  - `FileVersion` - File version (e.g., "1.2.3.0")
  - `InformationalVersion` - Full version with metadata (e.g., "1.2.3+abc123")

### 2. Build with Version

The calculated version is passed to the build process:

```bash
dotnet build --configuration Release \
  /p:Version=$VERSION \
  /p:AssemblyVersion=$ASSEMBLY_VERSION \
  /p:FileVersion=$FILE_VERSION \
  /p:InformationalVersion=$INFORMATIONAL_VERSION
```

### 3. Package Creation

NuGet packages are created with the calculated version:
- `DotGnatly.Natives.{version}.nupkg`
- `DotGnatly.Core.{version}.nupkg`
- `DotGnatly.Nats.{version}.nupkg`

### 4. Repository Tagging

After successful package creation (and optionally publication), the workflow:
- Creates an annotated git tag (e.g., `v1.2.3`)
- Pushes the tag to the repository
- Uses retry logic for network resilience

## Workflow Triggers

The NuGet package workflow runs on:

### 1. Push to Main or Develop
```yaml
on:
  push:
    branches:
      - main
      - develop
```
- Calculates version
- Builds packages
- Creates artifacts
- Tags repository (if on main/develop)

### 2. Release Creation
```yaml
on:
  release:
    types: [published, created]
```
- Calculates version
- Builds packages
- Publishes to NuGet.org (full releases only)
- Tags repository

### 3. Manual Trigger
```yaml
on:
  workflow_dispatch:
    inputs:
      publish: boolean
      increment: choice [major, minor, patch]
```
- Optionally publish to NuGet.org
- Specify version increment type

## Version Increment Types

When manually triggering the workflow, you can specify the increment type:

- **major** (1.2.3 → 2.0.0): Breaking changes
- **minor** (1.2.3 → 1.3.0): New features, backward compatible
- **patch** (1.2.3 → 1.2.4): Bug fixes, backward compatible

## Directory.Build.props Integration

The `src/Directory.Build.props` file is configured to use Mr-Version:

```xml
<!-- Version is managed by Mr-Version (mr-version.yml) -->
<Version Condition="'$(Version)' == ''">0.1.0</Version>
```

This allows:
- Mr-Version to override the version during CI builds
- Local builds to use a fallback version (0.1.0)
- Consistent versioning across all projects

## Workflow Jobs

### Job 1: calculate-version
- **Purpose**: Calculate semantic version using Mr-Version
- **Runs on**: ubuntu-latest
- **Outputs**: version, assembly-version, file-version, informational-version

### Job 2: build-native-bindings
- **Purpose**: Build native NATS server bindings
- **Runs on**: ubuntu-latest, windows-latest (matrix)
- **Outputs**: Native binding artifacts (.dll, .so)

### Job 3: package
- **Purpose**: Create NuGet packages
- **Runs on**: ubuntu-latest
- **Depends on**: calculate-version, build-native-bindings
- **Outputs**: NuGet package artifacts
- **Optionally**: Publishes to NuGet.org

### Job 4: tag-version
- **Purpose**: Tag repository with version
- **Runs on**: ubuntu-latest
- **Depends on**: calculate-version, package
- **Conditions**: Only runs when:
  - Full release is published
  - Manual trigger with publish=true
  - Push to main or develop branch

## Tag Naming Convention

Tags follow the format: `v{version}`

Examples:
- `v0.1.0` - Initial version
- `v1.0.0` - Major release
- `v1.1.0` - Minor update
- `v1.1.1` - Patch release

## Permissions

The workflow requires:

```yaml
permissions:
  contents: write  # Required for creating and pushing tags
  packages: write  # Required for publishing to GitHub Packages
```

## Secrets Required

The workflow uses:

- `GITHUB_TOKEN` - Automatically provided by GitHub Actions
- `NUGET_API_KEY` - Required for publishing to NuGet.org (optional, only for publishing)

## Example Workflow Run

1. **Developer pushes to main branch**
   ```bash
   git push origin main
   ```

2. **Workflow executes:**
   - ✅ Calculate version → `1.2.3`
   - ✅ Build native bindings
   - ✅ Create packages:
     - DotGnatly.Natives.1.2.3.nupkg
     - DotGnatly.Core.1.2.3.nupkg
     - DotGnatly.Nats.1.2.3.nupkg
   - ✅ Upload artifacts
   - ✅ Tag repository → `v1.2.3`

3. **Result:**
   - Packages available as workflow artifacts
   - Repository tagged with `v1.2.3`
   - Version visible in GitHub releases/tags

## Manual Publishing

To manually publish packages to NuGet.org:

1. Go to **Actions** tab in GitHub
2. Select **NuGet Package** workflow
3. Click **Run workflow**
4. Select branch (usually `main`)
5. Check **Publish to NuGet.org**
6. Choose version increment (major/minor/patch)
7. Click **Run workflow**

## Troubleshooting

### Version Not Incrementing

**Issue**: Version stays at base version (0.1.0)

**Solution**:
- Ensure commits follow conventional commit format
- Check `mr-version.yml` configuration
- Verify Mr-Version installation in workflow logs

### Tag Already Exists

**Issue**: Workflow fails with "tag already exists"

**Solution**:
- The workflow checks for existing tags and skips if found
- Delete the existing tag if you need to recreate it:
  ```bash
  git tag -d v1.2.3
  git push origin :refs/tags/v1.2.3
  ```

### Package Version Mismatch

**Issue**: NuGet packages have different versions

**Solution**:
- All packages use the same calculated version
- Check workflow logs for version calculation step
- Ensure `Directory.Build.props` is correctly configured

## Best Practices

1. **Conventional Commits**: Use conventional commit messages for automatic version bumping
   ```
   feat: add new feature (→ minor bump)
   fix: fix bug (→ patch bump)
   BREAKING CHANGE: breaking change (→ major bump)
   ```

2. **Branch Protection**: Protect main/develop branches to ensure workflow runs

3. **Semantic Versioning**: Follow SemVer 2.0 principles
   - Major: Breaking changes
   - Minor: New features, backward compatible
   - Patch: Bug fixes, backward compatible

4. **Tag Management**: Don't manually create version tags; let the workflow handle it

5. **Release Process**:
   - Develop on feature branches
   - Merge to develop for testing
   - Merge to main for production releases
   - Create GitHub release for final publication

## Additional Resources

- [Mr-Version GitHub](https://github.com/Khitiara/MrVersion)
- [Semantic Versioning 2.0](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [GitHub Actions Documentation](https://docs.github.com/actions)
- [NuGet Packaging Guide](../NUGET_PACKAGING.md)

---

**Last Updated**: November 2024
**Workflow Version**: 1.0
