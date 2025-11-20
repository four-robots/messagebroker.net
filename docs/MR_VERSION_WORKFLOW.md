# Mr-Version Automated Versioning Workflow

This document explains how DotGnatly uses **Mr-Version** (Mister.Version) for automated semantic versioning and repository tagging in the NuGet package workflow.

## Overview

The NuGet package workflow (`nuget-package.yml`) integrates with **Mr-Version** official GitHub Actions to:
- Automatically calculate semantic versions based on git history and conventional commits
- Apply versions to all NuGet packages during build
- Tag the repository with version tags after successful builds
- Ensure consistent versioning across all packages
- Support prerelease versioning (alpha, beta, rc)

## Mr-Version GitHub Actions

The workflow uses three official actions from the [mr-version organization](https://github.com/orgs/mr-version/repositories):

1. **`mr-version/setup@v1`** - Installs and configures the Mister.Version CLI tool
2. **`mr-version/calculate@v1`** - Calculates semantic versions from git history
3. **`mr-version/tag@v1`** - Creates and pushes version tags to the repository

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
- Uses `mr-version/setup@v1` to install the Mister.Version CLI tool
- Uses `mr-version/calculate@v1` to analyze git history and conventional commits
- Outputs version components:
  - `version` - Full semantic version (e.g., "1.2.3")
  - `major` - Major version number (e.g., "1")
  - `minor` - Minor version number (e.g., "2")
  - `patch` - Patch version number (e.g., "3")
  - `has-changes` - Boolean indicating if versions changed

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
- Uses `mr-version/tag@v1` to create version tags
- Creates global repository tags (e.g., `v1.2.3`)
- Optionally creates project-specific tags (e.g., `DotGnatly.Core/v1.2.3`)
- Automatically handles tag conflicts and provides detailed output

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
      prerelease-type: choice [none, alpha, beta, rc]
```
- Optionally publish to NuGet.org
- Specify prerelease type for versioning

## Prerelease Types

When manually triggering the workflow, you can specify the prerelease type:

- **none**: Full release version (e.g., "1.2.3")
- **alpha**: Alpha prerelease (e.g., "1.2.3-alpha.1")
- **beta**: Beta prerelease (e.g., "1.2.3-beta.1")
- **rc**: Release candidate (e.g., "1.2.3-rc.1")

## Conventional Commit Version Bumps

Mr-Version analyzes commit messages following [Conventional Commits](https://www.conventionalcommits.org/) to determine version bumps:

- **`feat:`** - New feature → **minor** version bump (1.2.3 → 1.3.0)
- **`fix:`** - Bug fix → **patch** version bump (1.2.3 → 1.2.4)
- **`feat!:`** or **`BREAKING CHANGE:`** - Breaking change → **major** version bump (1.2.3 → 2.0.0)

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
- **Actions Used**: `mr-version/setup@v1`, `mr-version/calculate@v1`
- **Outputs**: version, major, minor, patch, has-changes

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
- **Actions Used**: `mr-version/setup@v1`, `mr-version/tag@v1`
- **Depends on**: calculate-version, package
- **Conditions**: Only runs when:
  - Full release is published
  - Manual trigger with publish=true
  - Push to main or develop branch
- **Outputs**: tags-created, tags-count, global-tags-created, project-tags-created

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
5. Check **Publish to NuGet.org** (if publishing)
6. Choose **Prerelease type** (none, alpha, beta, rc)
7. Click **Run workflow**

## Troubleshooting

### Version Not Incrementing

**Issue**: Version stays at base version (0.1.0)

**Solution**:
- Ensure commits follow [Conventional Commits](https://www.conventionalcommits.org/) format:
  - `feat: add new feature` (minor bump)
  - `fix: fix bug` (patch bump)
  - `feat!: breaking change` or include `BREAKING CHANGE:` in commit body (major bump)
- Check `mr-version.yml` configuration
- Verify Mr-Version setup in workflow logs
- Use `fetch-depth: 0` in checkout to get full git history

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

## Workflow Configuration Example

Here's a simplified view of how the jobs work together:

```yaml
jobs:
  calculate-version:
    steps:
      - uses: mr-version/setup@v1
      - uses: mr-version/calculate@v1
        with:
          projects: 'src/**/*.csproj'
          prerelease-type: 'none'
          tag-prefix: 'v'

  build-native-bindings:
    # Build .dll and .so files in parallel

  package:
    needs: [calculate-version, build-native-bindings]
    steps:
      - run: dotnet build /p:Version=${{ needs.calculate-version.outputs.version }}
      - run: dotnet pack
      - run: dotnet nuget push  # (conditional)

  tag-version:
    needs: [calculate-version, package]
    steps:
      - uses: mr-version/setup@v1
      - uses: mr-version/tag@v1
        with:
          create-global-tags: true
          global-tag-strategy: 'all'
```

## Additional Resources

- [Mr-Version GitHub Organization](https://github.com/orgs/mr-version/repositories)
- [mr-version/setup Action](https://github.com/mr-version/setup)
- [mr-version/calculate Action](https://github.com/mr-version/calculate)
- [mr-version/tag Action](https://github.com/mr-version/tag)
- [Mister.Version CLI](https://github.com/mr-version/mister.version)
- [Semantic Versioning 2.0](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [GitHub Actions Documentation](https://docs.github.com/actions)
- [NuGet Packaging Guide](../NUGET_PACKAGING.md)

---

**Last Updated**: November 2024
**Workflow Version**: 1.0
