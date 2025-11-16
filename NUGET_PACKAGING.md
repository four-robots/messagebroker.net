# NuGet Packaging Guide for DotGnatly

This document explains the NuGet packaging setup for DotGnatly and how to create, verify, and publish packages.

## Overview

DotGnatly is packaged as three NuGet packages:

1. **DotGnatly.Core** - Core abstractions and models (no dependencies)
2. **DotGnatly.Natives** - Native NATS server bindings (platform-specific libraries)
3. **DotGnatly.Nats** - NATS implementation (depends on DotGnatly.Core and DotGnatly.Natives)

## Multi-Targeting Support

Both packages support multiple .NET versions:
- **.NET 8.0** (`net8.0`)
- **.NET 9.0** (`net9.0`)
- **.NET 10.0** (`net10.0`)

This ensures maximum compatibility across different .NET versions.

## Package Structure

### DotGnatly.Core
```
DotGnatly.Core.1.0.0.nupkg
├── lib/
│   ├── net8.0/
│   │   └── DotGnatly.Core.dll
│   ├── net9.0/
│   │   └── DotGnatly.Core.dll
│   └── net10.0/
│       └── DotGnatly.Core.dll
└── README.md
```

### DotGnatly.Natives (NEW)
```
DotGnatly.Natives.1.0.0.nupkg
├── runtimes/
│   ├── win-x64/
│   │   └── native/
│   │       └── nats-bindings.dll  (Windows native binary)
│   └── linux-x64/
│       └── native/
│           └── nats-bindings.so   (Linux native binary)
└── README.md
```

This package contains only the native bindings and can be versioned independently from the main library.

### DotGnatly.Nats
```
DotGnatly.Nats.1.0.0.nupkg
├── lib/
│   ├── net8.0/
│   │   └── DotGnatly.Nats.dll
│   ├── net9.0/
│   │   └── DotGnatly.Nats.dll
│   └── net10.0/
│       └── DotGnatly.Nats.dll
├── dependencies/
│   ├── DotGnatly.Core (>= 1.0.0)
│   └── DotGnatly.Natives (>= 1.0.0)
└── README.md
```

Note: Native bindings are now provided by the DotGnatly.Natives package dependency.

## Native Bindings

The native NATS server bindings are packaged in the **DotGnatly.Natives** package using the **runtime-specific package structure**:

- **Windows (x64)**: `runtimes/win-x64/native/nats-bindings.dll`
- **Linux (x64)**: `runtimes/linux-x64/native/nats-bindings.so`

NuGet automatically deploys the correct native library based on the target runtime identifier (RID).

### How It Works

When you reference `DotGnatly.Nats` in your project, NuGet will:

1. Restore the `DotGnatly.Natives` package dependency
2. Copy the appropriate assembly from `lib/netX.0/` based on your target framework
3. Copy the appropriate native binary from `runtimes/{rid}/native/` (from DotGnatly.Natives) based on your runtime (Windows/Linux)
4. Place the native binary in your output directory alongside your executable

This happens automatically - no manual configuration required!

### Independent Versioning Benefits

The separate `DotGnatly.Natives` package enables:

- **Hotfixes**: Quickly patch native binding issues without releasing a new version of the main library
- **Platform Updates**: Add support for new platforms (macOS, ARM architectures) independently
- **NATS Upgrades**: Update to newer NATS server versions without changing the API surface
- **Selective Updates**: Users can pin specific native binding versions if needed

## Building Native Binaries

Before creating NuGet packages, you need to build the native bindings for both platforms.

### Linux (requires Go 1.21+, gcc)

```bash
cd native
./build.sh
```

This creates `nats-bindings.so` which is then copied to `src/DotGnatly.Natives/`

### Windows (requires Go 1.21+, TDM-GCC or MinGW-w64)

```powershell
cd native
.\build.ps1
```

This creates `nats-bindings.dll` which is then copied to `src/DotGnatly.Natives/`

### Cross-Platform Build

If you have cross-compilation set up:

```bash
cd native
./build-all.sh
```

This builds both `.dll` and `.so` files.

## Creating NuGet Packages

### Quick Start

**Linux/macOS:**
```bash
./pack-nuget.sh
```

**Windows:**
```powershell
.\pack-nuget.ps1
```

These scripts will:
1. Build native bindings
2. Clean previous builds
3. Restore dependencies
4. Build the solution for all target frameworks
5. Create NuGet packages in `./nupkg/` directory

### Manual Process

If you prefer to do it manually:

```bash
# 1. Build native bindings
cd native && ./build.sh && cd ..

# 2. Copy bindings to DotGnatly.Natives
cp native/nats-bindings.dll src/DotGnatly.Natives/  # Windows
cp native/nats-bindings.so src/DotGnatly.Natives/   # Linux

# 3. Build solution
dotnet build DotGnatly.sln -c Release

# 4. Create packages (in dependency order)
mkdir -p nupkg
dotnet pack src/DotGnatly.Natives/DotGnatly.Natives.csproj -c Release -o ./nupkg
dotnet pack src/DotGnatly.Core/DotGnatly.Core.csproj -c Release -o ./nupkg
dotnet pack src/DotGnatly.Nats/DotGnatly.Nats.csproj -c Release -o ./nupkg
```

## Verifying Package Contents

After creating packages, verify they contain the correct files:

```bash
# Verify DotGnatly.Natives package
unzip -l nupkg/DotGnatly.Natives.1.0.0.nupkg

# Verify DotGnatly.Nats package
unzip -l nupkg/DotGnatly.Nats.1.0.0.nupkg

# Or use NuGet Package Explorer (Windows GUI tool)
# Download from: https://github.com/NuGetPackageExplorer/NuGetPackageExplorer
```

**DotGnatly.Natives - What to check:**

✅ Native bindings in `runtimes/win-x64/native/nats-bindings.dll`
✅ Native bindings in `runtimes/linux-x64/native/nats-bindings.so`
✅ README.md in package root
✅ Correct version number
✅ Package metadata (authors, description, tags, license)

**DotGnatly.Nats - What to check:**

✅ All three target frameworks present (`lib/net8.0`, `lib/net9.0`, `lib/net10.0`)
✅ Dependency on DotGnatly.Core
✅ Dependency on DotGnatly.Natives
✅ README.md in package root
✅ Correct version number (1.0.0)
✅ Package metadata (authors, description, tags, license)

## Testing Packages Locally

Before publishing, test packages locally:

```bash
# Create a test project
mkdir test-package && cd test-package
dotnet new console

# Add package from local source
dotnet add package DotGnatly.Nats --version 1.0.0 --source ../nupkg

# Verify native bindings are copied
dotnet build
ls bin/Debug/net9.0/  # Should see nats-bindings.dll or .so
```

**Test code:**

```csharp
using DotGnatly.Nats;
using DotGnatly.Core.Configuration;

using var controller = new NatsController();
var config = new BrokerConfiguration { Port = 4222 };
var result = await controller.ConfigureAsync(config);

if (result.Success)
{
    Console.WriteLine($"NATS server started on port {config.Port}");
    await controller.ShutdownAsync();
}
```

## Publishing to NuGet.org

### Prerequisites

1. Create account at https://www.nuget.org
2. Generate an API key from https://www.nuget.org/account/apikeys
3. Keep API key secure (never commit to source control)

### Publishing

```bash
# Publish in dependency order
# 1. Publish DotGnatly.Natives first (no dependencies)
dotnet nuget push nupkg/DotGnatly.Natives.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json

# 2. Publish DotGnatly.Core (no dependencies)
dotnet nuget push nupkg/DotGnatly.Core.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json

# 3. Publish DotGnatly.Nats last (depends on Core and Natives)
dotnet nuget push nupkg/DotGnatly.Nats.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

**Important:** Publish packages in dependency order:
1. `DotGnatly.Natives` (no dependencies)
2. `DotGnatly.Core` (no dependencies)
3. `DotGnatly.Nats` (depends on both Core and Natives)

### Publishing Symbols

Symbol packages (`.snupkg`) are automatically created and should be published alongside main packages:

```bash
dotnet nuget push nupkg/DotGnatly.Core.1.0.0.snupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

This enables source debugging and better stack traces for consumers.

## Version Management

Version is managed in `src/Directory.Build.props`:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
</PropertyGroup>
```

To update version for all packages, change it once in `Directory.Build.props`.

### Semantic Versioning

Follow [SemVer 2.0](https://semver.org/):

- **Major** (1.x.x): Breaking changes
- **Minor** (x.1.x): New features, backward compatible
- **Patch** (x.x.1): Bug fixes, backward compatible

Examples:
- `1.0.0` → `1.0.1`: Bug fix release
- `1.0.0` → `1.1.0`: Added new configuration properties
- `1.0.0` → `2.0.0`: Changed IBrokerController interface (breaking)

## Package Metadata

Shared metadata is in `src/Directory.Build.props`:

```xml
<PropertyGroup>
  <Authors>DotGnatly Contributors</Authors>
  <Company>DotGnatly</Company>
  <Copyright>Copyright (c) 2024 DotGnatly Contributors</Copyright>
  <PackageProjectUrl>https://github.com/four-robots/dotgnatly</PackageProjectUrl>
  <RepositoryUrl>https://github.com/four-robots/dotgnatly</RepositoryUrl>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
</PropertyGroup>
```

Project-specific metadata (like `Description` and `PackageTags`) is in each `.csproj` file.

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Publish NuGet

on:
  push:
    tags:
      - 'v*'  # Trigger on version tags (v1.0.0, v1.1.0, etc.)

jobs:
  build-and-publish:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: |
          8.0.x
          9.0.x

    - name: Setup Go
      uses: actions/setup-go@v4
      with:
        go-version: '1.21'

    - name: Build native bindings
      run: |
        cd native
        ./build-all.sh

    - name: Build solution
      run: dotnet build DotGnatly.sln -c Release

    - name: Create packages
      run: |
        dotnet pack src/DotGnatly.Core/DotGnatly.Core.csproj -c Release -o ./nupkg
        dotnet pack src/DotGnatly.Nats/DotGnatly.Nats.csproj -c Release -o ./nupkg

    - name: Publish to NuGet
      run: |
        dotnet nuget push nupkg/*.nupkg \
          --api-key ${{ secrets.NUGET_API_KEY }} \
          --source https://api.nuget.org/v3/index.json
```

## Troubleshooting

### Native binding not found at runtime

**Symptom:** `DllNotFoundException: Unable to load DLL 'nats-bindings'`

**Solutions:**
1. Verify package includes native bindings: `unzip -l nupkg/DotGnatly.Nats.1.0.0.nupkg | grep native`
2. Check output directory after build: `ls bin/Debug/net9.0/` should show `nats-bindings.dll` or `.so`
3. Ensure you're targeting a specific RID: `dotnet publish -r win-x64` or `dotnet publish -r linux-x64`

### Multi-targeting build errors

**Symptom:** Build fails with "The type or namespace name could not be found"

**Solutions:**
1. Ensure all target frameworks are installed: `dotnet --list-sdks`
2. Install missing SDKs from https://dot.net
3. Update Visual Studio to latest version (includes SDK)

### Package missing target framework

**Symptom:** Package only contains one target framework

**Solutions:**
1. Check `.csproj` has `<TargetFrameworks>` (plural) not `<TargetFramework>` (singular)
2. Clean and rebuild: `dotnet clean && dotnet build`
3. Verify with: `unzip -l nupkg/DotGnatly.Core.1.0.0.nupkg | grep "lib/"`

## Additional Resources

- [NuGet Documentation](https://docs.microsoft.com/nuget/)
- [.NET Multi-Targeting](https://docs.microsoft.com/dotnet/standard/frameworks)
- [Runtime Identifier (RID) Catalog](https://docs.microsoft.com/dotnet/core/rid-catalog)
- [NuGet Package Explorer](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer)
- [DotGnatly Documentation](./docs/INDEX.md)

## Support

For issues related to packaging:
1. Check this guide first
2. Review existing GitHub issues
3. Create a new issue with:
   - Output of `dotnet --info`
   - Package creation steps you followed
   - Error messages or unexpected behavior

---

**Last Updated:** November 2024
**Version:** 1.0.0
