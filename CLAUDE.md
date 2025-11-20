# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

# DotGnatly Project Guide

## Project Overview

DotGnatly is a .NET library that provides programmatic control over NATS server instances with enhanced runtime reconfiguration capabilities. Unlike the standard nats-csharp client library, this project provides **full server control** with hot configuration reload, validation, versioning, and rollback.

**Key Differentiator**: Server control with runtime reconfiguration, not just client operations.

## Architecture

### Technology Stack
- **.NET 8.0/9.0/10.0** - Multi-targeted framework support
- **C# Latest** - Language version with nullable reference types enabled
- **Go bindings** - Native NATS server via P/Invoke
- **NATS Server v2.12** - Embedded messaging server
- **System.Text.Json** - JSON serialization
- **NATS.Net** - Official NATS client library (for Extensions.JetStream only)
- **Mister.Version** - Automatic semantic versioning for monorepo
- **No external NuGet dependencies** - Core and Nats projects use only System.* namespaces

### Layer Architecture

```
Application Layer (Consumer Code)
    ↓
DotGnatly.Extensions.JetStream (Optional: Client-side JetStream operations)
    ↓
DotGnatly.Core (Abstractions, Validation, Versioning, Events)
    ↓
DotGnatly.Nats (NATS Implementation, P/Invoke Bindings)
    ↓
Native Go Library (nats-bindings.dll/.so)
    ↓
NATS Server v2.12 (Embedded)
```

## Project Structure

```
DotGnatly/
├── src/
│   ├── DotGnatly.Core/                  # Core abstractions and models
│   │   ├── Abstractions/                    # Interfaces (IBrokerController, IConfigurationValidator)
│   │   ├── Configuration/                   # Config models with versioning
│   │   ├── Validation/                      # Pre-apply validation system
│   │   └── Events/                          # Change notification events
│   ├── DotGnatly.Nats/                  # NATS-specific implementation
│   │   ├── Bindings/                        # P/Invoke layer (Windows/Linux)
│   │   └── Implementation/                  # NatsController
│   ├── DotGnatly.Natives/               # Native bindings package
│   │   ├── nats-bindings.dll/.so            # Native Go library (copied from native/)
│   │   └── README.md                        # Natives package documentation
│   ├── DotGnatly.Extensions.JetStream/  # JetStream client-side operations
│   │   └── (Uses NATS.Net for streams, consumers, KV stores)
│   ├── Directory.Build.props            # Shared build configuration
│   ├── Directory.Packages.props         # Central package management
│   └── mr-version.yml                   # Mister.Version configuration
├── examples/
│   └── DotGnatly.Examples/              # Interactive examples and tests
├── tests/
│   ├── DotGnatly.Core.Tests/            # Unit tests for Core
│   ├── DotGnatly.Nats.Tests/            # Unit tests for Nats
│   ├── DotGnatly.IntegrationTests/      # Integration tests
│   ├── DotGnatly.Extensions.JetStream.Tests/           # Unit tests for JetStream
│   └── DotGnatly.Extensions.JetStream.IntegrationTests/  # JetStream integration tests
├── native/                              # Go source code for native bindings
│   ├── nats-bindings.go                 # cgo bindings implementation
│   ├── nats-bindings_test.go            # Go unit tests
│   ├── go.mod                           # Go module dependencies
│   ├── build.sh                         # Linux/macOS build script
│   ├── build.ps1                        # Windows build script
│   └── README.md                        # Native bindings documentation
├── docs/                                # Comprehensive documentation
│   ├── GETTING_STARTED.md
│   ├── API_DESIGN.md
│   ├── ARCHITECTURE.md
│   └── ...
├── DotGnatly.sln                    # Solution file
├── README.md                            # Main project README
├── ROADMAP.md                           # Feature implementation roadmap
└── CLAUDE.md                            # This file
```

## Core Components

### DotGnatly.Core (18 files, ~1,500 LOC)

**Key Interfaces:**
- `IBrokerController` - Main broker control interface (ConfigureAsync, ApplyChangesAsync, etc.)
- `IConfigurationValidator` - Validation pipeline
- `IConfigurationStore` - Version storage and retrieval

**Configuration Models:**
- `BrokerConfiguration` - Enhanced config with metadata (ID, description, created date)
- `ConfigurationVersion` - Versioned snapshots with change tracking
- `ConfigurationDiff` - Automatic change detection via reflection
- `ConfigurationResult` - Operation results with success/failure info

**Validation System:**
- `ConfigurationValidator` - Comprehensive validation rules (ports, payloads, timeouts, JetStream)
- `ValidationRuleBuilder` - Fluent API for custom validation rules
- Pre-apply validation prevents invalid configurations

**Event System:**
- `ConfigurationChanging` - Pre-change event (cancelable)
- `ConfigurationChanged` - Post-change event with diff

### DotGnatly.Nats

**Main Implementation:**
- `NatsController` - Core IBrokerController implementation
  - Thread-safe with SemaphoreSlim
  - Validation integration
  - Version tracking
  - Event firing
  - Hot reload support

**Bindings Layer:**
- `INatsBindings` - Platform-agnostic P/Invoke interface
- `WindowsNatsBindings` / `LinuxNatsBindings` - Platform-specific implementations
- `NatsBindingsFactory` - Runtime platform detection
- `ConfigurationMapper` - Bidirectional mapping between Core and Bindings models

**Extensions:**
- `NatsControllerExtensions` - 16+ fluent API methods (WithPortAsync, EnableJetStreamAsync, etc.)

### DotGnatly.Extensions.JetStream

**Purpose**: Provides client-side JetStream operations using the official NATS.Net library

**Key Features:**
- Stream management (create, update, delete, list)
- Consumer management (create, update, delete, subscribe)
- Key-Value store operations
- Fluent API that extends `NatsController`
- Uses NATS.Net client library for robust client-side operations

**Dependencies:**
- `DotGnatly.Core` - Core abstractions
- `DotGnatly.Nats` - Server control
- `NATS.Net` - Official NATS client library

## Development Workflow

### Building the Project

```bash
# 1. Build native Go bindings first (required before .NET build)
cd native
./build.sh           # Linux/macOS
# or
.\build.ps1          # Windows (PowerShell)

# 2. Build entire solution
cd ..
dotnet build DotGnatly.sln

# Build specific project
dotnet build src/DotGnatly.Core/DotGnatly.Core.csproj

# Build in Release mode
dotnet build DotGnatly.sln -c Release
```

**Important**: The native bindings must be built before the .NET projects, as the .NET code depends on `nats-bindings.dll` (Windows) or `nats-bindings.so` (Linux).

**Versioning**: The project uses Mister.Version for automatic semantic versioning in a monorepo setup. Version configuration is in `src/mr-version.yml`.

### Running Examples

```bash
# Interactive menu (from repository root)
cd examples/DotGnatly.Examples
dotnet run

# Note: Examples were moved from src/ to examples/ directory
```

### Testing

The project uses xUnit for unit and integration tests:

**Unit Tests:**
- `DotGnatly.Core.Tests` - Core functionality tests
- `DotGnatly.Nats.Tests` - NATS controller and bindings tests (40+ monitoring tests)
- `DotGnatly.Extensions.JetStream.Tests` - JetStream extension tests

**Integration Tests:**
- `DotGnatly.IntegrationTests` - Full integration tests (11 monitoring tests)
- `DotGnatly.Extensions.JetStream.IntegrationTests` - JetStream integration tests

**Native Tests:**
- `native/nats-bindings_test.go` - Go unit tests for native bindings (30+ tests)

**Running Tests:**
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/DotGnatly.Core.Tests

# Run with verbose output
dotnet test -v detailed

# Run tests in parallel
dotnet test --parallel
```

### NuGet Packaging

The project is configured for NuGet packaging with multi-targeting support.

**Packages:**
- `DotGnatly.Core` - Core abstractions (no dependencies)
- `DotGnatly.Natives` - Native NATS server bindings (platform-specific libraries)
- `DotGnatly.Nats` - NATS implementation (depends on DotGnatly.Core and DotGnatly.Natives)
- `DotGnatly.Extensions.JetStream` - JetStream client extensions (depends on DotGnatly.Core, DotGnatly.Nats, and NATS.Net)

**Multi-Targeting:**
- Supports .NET 8.0, 9.0, and 10.0
- Native bindings included for Windows (x64) and Linux (x64)

**Creating Packages:**

```bash
# Quick method - use packaging scripts
./pack-nuget.sh        # Linux/macOS
.\pack-nuget.ps1       # Windows

# Manual method
cd native && ./build.sh && cd ..  # Build native bindings first
dotnet pack src/DotGnatly.Natives/DotGnatly.Natives.csproj -c Release -o ./nupkg
dotnet pack src/DotGnatly.Core/DotGnatly.Core.csproj -c Release -o ./nupkg
dotnet pack src/DotGnatly.Nats/DotGnatly.Nats.csproj -c Release -o ./nupkg
dotnet pack src/DotGnatly.Extensions.JetStream/DotGnatly.Extensions.JetStream.csproj -c Release -o ./nupkg
```

**Native Bindings Structure:**
The `DotGnatly.Natives` package uses runtime-specific native library deployment:
- `runtimes/win-x64/native/nats-bindings.dll` - Windows binding
- `runtimes/linux-x64/native/nats-bindings.so` - Linux binding

NuGet automatically deploys the correct native binary based on the target runtime.

**Independent Versioning:**
The `DotGnatly.Natives` package can be versioned and updated independently, allowing for:
- Hotfixes for native binding issues without releasing a new version of the main library
- Platform updates to add support for new architectures
- NATS server upgrades without changing the API surface

**Package Configuration:**
- Shared properties: `src/Directory.Build.props`
- Central package management: `src/Directory.Packages.props`
- Project-specific metadata: Individual `.csproj` files
- Version: Automatically calculated by Mister.Version based on `src/mr-version.yml`

**Testing Packages Locally:**
```bash
dotnet add package DotGnatly.Nats --version 0.1.0 --source ./nupkg
```

**Package Versioning:**
- Uses Mister.Version for automatic semantic versioning
- Base version: 0.1.0 (configured in `src/mr-version.yml`)
- Version auto-increments based on git history and conventional commits
- Each package can have independent versioning in monorepo setup

## Important Conventions

### Code Style
- **Nullable Reference Types**: Enabled throughout - use `?` for nullable types
- **Async/Await**: All I/O operations are async
- **Dispose Pattern**: Use `IDisposable` and `using` statements
- **Thread Safety**: Critical sections protected with `SemaphoreSlim`
- **Naming**: PascalCase for public members, camelCase for private fields

### Configuration Patterns

**Correct Usage:**
```csharp
// Create controller
using var controller = new NatsController();

// Configure with validation
var config = new BrokerConfiguration
{
    Port = 4222,
    Jetstream = true,
    Description = "Production server"
};
var result = await controller.ConfigureAsync(config);

// Hot reload with fluent API
await controller.ApplyChangesAsync(c =>
{
    c.Debug = true;
    c.Port = 4223;
});

// Subscribe to events
controller.ConfigurationChanging += (s, e) =>
{
    // Cancel if needed
    if (e.Proposed.Port > 5000)
    {
        e.CancelChange("Port too high");
    }
};

// Rollback if needed
await controller.RollbackAsync(toVersion: 2);
```

## Key Features

### 1. Zero-Downtime Hot Reload
- Update configuration without server restart
- Clients stay connected during changes
- Validation prevents invalid configurations
- 5-20ms reload time

### 2. Configuration Versioning
- Every change creates a new version
- Complete history available
- Rollback to any previous version
- Audit trail of changes

### 3. Pre-Apply Validation
- Port ranges (1-65535)
- Payload size constraints
- Timeout validation
- JetStream consistency checks
- Change impact warnings

### 4. Event-Driven Architecture
- Subscribe to configuration changes
- Cancel changes before they're applied
- Build reactive systems
- Integration with monitoring/logging

### 5. Fluent API
- Method chaining for clean code
- IntelliSense-friendly
- Extension methods for common operations

### 6. Comprehensive Monitoring & Observability
- **11 Monitoring Endpoints**: Varz, Connz, Subsz, Jsz, Routez, Leafz, Accountz, Gatewayz, AccountStatz, Raftz, and more
- **Real-time Metrics**: Connection counts, subscription tracking, memory usage, message rates
- **JetStream Monitoring**: Stream details, consumer stats, storage usage, account-level metrics
- **Cluster Monitoring**: Route health, leaf node connections, gateway statistics, Raft consensus state
- **Connection Management**: Client inspection, forced disconnection, detailed client information
- **Account Management**: Runtime account registration, account lookups, system account designation
- **Health Checks**: Server readiness probes, running status, JetStream enabled detection
- **Server Identity**: Server ID, name, version information

### 7. Runtime Control & Management
- **Dynamic Account Creation**: Register accounts at runtime without restart
- **Client Management**: Disconnect misbehaving clients, inspect client details
- **Health Probes**: Wait for server readiness, check running status
- **System Account**: Designate special accounts for server events and monitoring
- **Multi-Server Support**: Manage multiple server instances simultaneously

## Common Tasks

### Adding New Configuration Properties

1. Add property to `BrokerConfiguration` in `DotGnatly.Core/Configuration/BrokerConfiguration.cs`
2. Add validation rules in `DotGnatly.Core/Validation/ConfigurationValidator.cs`
3. Update mapping in `DotGnatly.Nats/ConfigurationMapper.cs`
4. Add fluent API extension in `DotGnatly.Nats/Extensions/NatsControllerExtensions.cs`
5. Add example in `DotGnatly.Examples/`
6. Update documentation in `docs/API_DESIGN.md`

### Adding Custom Validation Rules

Use the `ValidationRuleBuilder`:
```csharp
var validator = new ConfigurationValidator();
validator.AddRule(config =>
{
    if (config.Port == 666)
        return ValidationResult.Error("Port 666 is not allowed");
    return ValidationResult.Success();
});
```

### Building and Updating Native Bindings

The native bindings are located in the `native/` directory and must be built before running the .NET projects.

**Quick Build:**
```bash
cd native
./build.sh           # Linux/macOS - builds .so file
.\build.ps1          # Windows - builds .dll file
```

**Cross-Platform Build:**
```bash
cd native
./build-all.sh       # Builds both .so and .dll (requires cross-compilation setup)
```

**Upgrading NATS Server Version:**
1. Edit `native/go.mod` and update the version:
   ```go
   require (
       github.com/nats-io/nats-server/v2 v2.12.0  // Update this
   )
   ```
2. Run `cd native && go get github.com/nats-io/nats-server/v2@v2.12.0 && go mod tidy`
3. Rebuild: `./build.sh` or `.\build.ps1`
4. Test thoroughly with `cd examples/DotGnatly.Examples && dotnet run`

See **[native/README.md](../native/README.md)** for comprehensive build documentation.

### Troubleshooting Native Bindings

**Platform Detection Issues:**
- Check `NatsBindingsFactory.Create()` - auto-detects Windows/Linux
- Windows: Requires `nats-bindings.dll` in output directory
- Linux: Requires `nats-bindings.so` in output directory

**Build Issues:**
- "go: cannot find main module" → Run from `native/` directory
- "gcc not found" (Windows) → Install TDM-GCC or MinGW-w64
- "undefined: server.Server" → Run `go mod download && go mod tidy`

**Runtime Issues:**
- "Unable to load DLL/shared library" → Ensure bindings are in same directory as .exe
- Run build script to copy bindings to output directories
- On Linux, check dependencies: `ldd native/nats-bindings.so`

### Using Monitoring Endpoints

DotGnatly provides comprehensive server monitoring through 11+ endpoints:

**Connection Monitoring:**
```csharp
// Get all connections
var connz = await controller.GetConnzAsync();
var connections = JsonSerializer.Deserialize<ConnzResponse>(connz);

// Get client details
var clientInfo = await controller.GetClientInfoAsync(clientId);

// Disconnect a client
await controller.DisconnectClientAsync(clientId);
```

**Subscription Monitoring:**
```csharp
// Get all subscriptions
var subsz = await controller.GetSubszAsync();

// Filter by subject
var filtered = await controller.GetSubszAsync(subscriptionsFilter: "orders.*");
```

**JetStream Monitoring:**
```csharp
// Get JetStream stats
var jsz = await controller.GetJszAsync();

// Get account-specific JetStream info
var accountJsz = await controller.GetJszAsync(accountName: "tenant1");
```

**Server Health & Status:**
```csharp
// Wait for server to be ready
bool ready = await controller.WaitForReadyAsync(timeoutSeconds: 10);

// Check if server is running
bool running = await controller.IsServerRunningAsync();

// Check if JetStream is enabled
bool jsEnabled = await controller.IsJetStreamEnabledAsync();

// Get server identity
string serverId = await controller.GetServerIdAsync();
string serverName = await controller.GetServerNameAsync();
```

**Account Management:**
```csharp
// Register a new account at runtime
var accountInfo = await controller.RegisterAccountAsync("tenant1");

// Lookup account details
var account = await controller.LookupAccountAsync("tenant1");

// Set system account for server events
await controller.SetSystemAccountAsync("SYS");
```

**Advanced Monitoring:**
```csharp
// Full server variables and statistics
var varz = await controller.GetVarzAsync();

// Cluster routing information
var routez = await controller.GetRoutezAsync();

// Leaf node connections
var leafz = await controller.GetLeafzAsync();

// Account-level monitoring
var accountz = await controller.GetAccountzAsync();

// Per-account statistics
var accountStatz = await controller.GetAccountStatzAsync();

// Gateway connections (super-cluster)
var gatewayz = await controller.GetGatewayzAsync();

// Raft consensus state (JetStream clustering)
var raftz = await controller.GetRaftzAsync();
```

See **[docs/MONITORING.md](docs/MONITORING.md)** for comprehensive monitoring documentation.

## File Locations Reference

### Configuration Models
- `src/DotGnatly.Core/Configuration/BrokerConfiguration.cs` - Main config model
- `src/DotGnatly.Core/Configuration/ConfigurationVersion.cs` - Version tracking
- `src/DotGnatly.Core/Configuration/ConfigurationDiff.cs` - Change detection

### Core Interfaces
- `src/DotGnatly.Core/Abstractions/IBrokerController.cs` - Main interface
- `src/DotGnatly.Core/Abstractions/IConfigurationValidator.cs` - Validation interface

### Implementation
- `src/DotGnatly.Nats/Implementation/NatsController.cs` - Main implementation
- `src/DotGnatly.Nats/ConfigurationMapper.cs` - Config mapping
- `src/DotGnatly.Nats/Extensions/NatsControllerExtensions.cs` - Fluent API

### Examples
- `examples/DotGnatly.Examples/Program.cs` - Interactive examples

### Tests
- `tests/DotGnatly.Core.Tests/` - Core unit tests
- `tests/DotGnatly.Nats.Tests/` - NATS controller unit tests (40+ monitoring tests)
- `tests/DotGnatly.IntegrationTests/` - Integration tests (11 monitoring tests)
- `tests/DotGnatly.Extensions.JetStream.Tests/` - JetStream unit tests
- `tests/DotGnatly.Extensions.JetStream.IntegrationTests/` - JetStream integration tests
- `native/nats-bindings_test.go` - 30+ Go unit tests for native bindings

### Native Bindings
- `native/nats-bindings.go` - Go CGO bindings with monitoring endpoints
- `native/README.md` - Native bindings build documentation
- `native/README_TESTS.md` - Native bindings test documentation

## Documentation

Comprehensive documentation is in the `docs/` folder:
- **GETTING_STARTED.md** (997 lines) - Installation, first steps
- **API_DESIGN.md** (1,067 lines) - Complete API reference
- **ARCHITECTURE.md** (930 lines) - Deep dive into system design
- **MONITORING.md** (500+ lines) - Monitoring and observability guide
- **DIAGRAMS.md** (631 lines) - Visual architecture diagrams
- **QUICK_REFERENCE.md** (569 lines) - Cheat sheet
- **INDEX.md** (514 lines) - Navigation guide

Additional documentation:
- **TODO_NATS_FEATURES.md** (470 lines) - Feature roadmap and implementation status
- **MONITORING_IMPLEMENTATION_SUMMARY.md** - Technical implementation details
- **IMPLEMENTATION_SUMMARY.md** - Project implementation summary
- **TEACHER_GUIDE.md** - Educational guide for teaching with DotGnatly

Total: 6,500+ lines of documentation

## Important Notes for AI Assistants

### When Making Changes

1. **Always validate** - Run the validation system before applying config changes
2. **Maintain thread safety** - Use `SemaphoreSlim` for critical sections
3. **Fire events** - Ensure `ConfigurationChanging` and `ConfigurationChanged` events are fired
4. **Update docs** - Keep documentation in sync with code changes
5. **Run tests** - Execute `SimpleTest` to verify changes don't break existing functionality
6. **Check nullable annotations** - Project uses nullable reference types

### Code Quality Standards

- **Zero warnings** - Project currently builds with 0 warnings
- **Zero errors** - All tests passing
- **Async/await** - Use async throughout, don't mix sync/async
- **Resource disposal** - Always dispose `NatsController` and other IDisposable resources
- **Validation** - Never bypass validation unless explicitly needed for testing

### Security Considerations

- **Port validation** - Ensure ports are in valid range (1-65535)
- **Path validation** - Validate file paths for JetStream storage
- **Credential handling** - Never log passwords or credentials
- **TLS configuration** - Support TLS for production deployments

## Git Workflow

### Main Branch
- Main branch: `main`

### Commit Guidelines
- Use descriptive commit messages
- Reference issue numbers when applicable
- Keep commits focused and atomic
- For version bumping: Use conventional commits (feat:, fix:, chore:, etc.) as Mister.Version uses them for versioning

## Version Information

- **Base Version**: 0.1.0 (configured in `src/mr-version.yml`)
- **NATS Server Version**: 2.12
- **.NET Frameworks**: 8.0, 9.0, 10.0 (multi-targeted)
- **C# Version**: Latest
- **Last Updated**: November 2025

## Quick Reference

### Essential Commands

```bash
# Build (from repository root)
dotnet build DotGnatly.sln

# Run examples
cd examples/DotGnatly.Examples && dotnet run

# Run tests
dotnet test

# Run specific test project
dotnet test tests/DotGnatly.Core.Tests

# Clean
dotnet clean DotGnatly.sln

# Build native bindings (required before first .NET build)
cd native && ./build.sh  # Linux/macOS
cd native && .\build.ps1 # Windows
```

### Essential Code Patterns

```csharp
// Start server
using var controller = new NatsController();
var result = await controller.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

// Hot reload
await controller.ApplyChangesAsync(c => c.Debug = true);

// Event handling
controller.ConfigurationChanging += (s, e) => { /* validate */ };
controller.ConfigurationChanged += (s, e) => { /* react to change */ };

// Rollback
await controller.RollbackAsync(toVersion: 2);

// Monitoring
var connz = await controller.GetConnzAsync();  // Connection stats
var jsz = await controller.GetJszAsync();      // JetStream stats
var varz = await controller.GetVarzAsync();    // Server variables

// Health checks
bool ready = await controller.WaitForReadyAsync();
bool running = await controller.IsServerRunningAsync();

// Account management
var account = await controller.RegisterAccountAsync("tenant1");
var accountInfo = await controller.LookupAccountAsync("tenant1");

// Connection management
await controller.DisconnectClientAsync(clientId);

// Shutdown
await controller.ShutdownAsync();
```

## Support Resources

- **Documentation**: `docs/` folder
- **Examples**: `examples/DotGnatly.Examples/`
- **Architecture**: `docs/ARCHITECTURE.md`
- **API Reference**: `docs/API_DESIGN.md`
- **Feature Roadmap**: `ROADMAP.md`
- **NATS Community**: https://slack.nats.io

---

**This file was last updated**: 2025-11-20
