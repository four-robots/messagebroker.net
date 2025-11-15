# Claude.MD - MessageBroker.NET

This file provides context and guidance for AI assistants working with the MessageBroker.NET codebase.

## Project Overview

MessageBroker.NET is a .NET library that provides programmatic control over NATS server instances with enhanced runtime reconfiguration capabilities. Unlike the standard nats-csharp client library, this project provides **full server control** with hot configuration reload, validation, versioning, and rollback.

**Key Differentiator**: Server control with runtime reconfiguration, not just client operations.

## Architecture

### Technology Stack
- **.NET 9.0** - Target framework
- **C# 12** - Language version with nullable reference types enabled
- **Go bindings** - Native NATS server via P/Invoke
- **NATS Server v2.11.0** - Embedded messaging server
- **System.Text.Json** - JSON serialization
- **No external NuGet dependencies** - Core uses only System.* namespaces

### Layer Architecture

```
Application Layer (Consumer Code)
    ↓
MessageBroker.Core (Abstractions, Validation, Versioning, Events)
    ↓
MessageBroker.Nats (NATS Implementation, P/Invoke Bindings)
    ↓
Native Go Library (nats-bindings.dll/.so)
    ↓
NATS Server v2.11 (Embedded)
```

## Project Structure

```
MessageBroker.NET/
├── src/
│   ├── MessageBroker.Core/          # Core abstractions and models
│   │   ├── Abstractions/            # Interfaces (IBrokerController, IConfigurationValidator)
│   │   ├── Configuration/           # Config models with versioning
│   │   ├── Validation/              # Pre-apply validation system
│   │   └── Events/                  # Change notification events
│   ├── MessageBroker.Nats/          # NATS-specific implementation
│   │   ├── Bindings/                # P/Invoke layer (Windows/Linux)
│   │   ├── Implementation/          # NatsController
│   │   └── nats-bindings.dll/.so    # Native Go library (copied from native/)
│   └── MessageBroker.Examples/      # Interactive examples and tests
├── native/                          # Go source code for native bindings
│   ├── nats-bindings.go             # cgo bindings implementation
│   ├── go.mod                       # Go module dependencies
│   ├── build.sh                     # Linux/macOS build script
│   ├── build.ps1                    # Windows build script
│   ├── build-all.sh                 # Cross-platform build
│   └── README.md                    # Native bindings documentation
├── docs/                            # Comprehensive documentation (5,592 lines)
│   ├── GETTING_STARTED.md
│   ├── API_DESIGN.md
│   ├── ARCHITECTURE.md
│   └── ...
├── nats-csharp/                     # Original reference implementation
├── MessageBroker.NET.sln            # Solution file
├── README.md                        # Main project README
├── PROJECT_SUMMARY.md               # Detailed project summary
└── CLAUDE.md                        # This file
```

## Core Components

### MessageBroker.Core (18 files, ~1,500 LOC)

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

### MessageBroker.Nats (6 files)

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
dotnet build MessageBroker.NET.sln

# Build specific project
dotnet build src/MessageBroker.Core/MessageBroker.Core.csproj

# Build in Release mode
dotnet build MessageBroker.NET.sln -c Release
```

**Important**: The native bindings must be built before the .NET projects, as the .NET code depends on `nats-bindings.dll` (Windows) or `nats-bindings.so` (Linux).

### Running Examples

```bash
# Interactive menu
cd src/MessageBroker.Examples
dotnet run

# Automated tests
cd src/MessageBroker.Examples
dotnet run -- test
```

### Testing

The project uses a simple test framework in `MessageBroker.Examples/SimpleTest.cs`:
- 7 comprehensive test scenarios
- All tests currently passing
- Tests cover: basic config, hot reload, validation, rollback, events, info, shutdown

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

## Common Tasks

### Adding New Configuration Properties

1. Add property to `BrokerConfiguration` in `MessageBroker.Core/Configuration/BrokerConfiguration.cs`
2. Add validation rules in `MessageBroker.Core/Validation/ConfigurationValidator.cs`
3. Update mapping in `MessageBroker.Nats/ConfigurationMapper.cs`
4. Add fluent API extension in `MessageBroker.Nats/Extensions/NatsControllerExtensions.cs`
5. Add example in `MessageBroker.Examples/`
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
       github.com/nats-io/nats-server/v2 v2.11.0  // Update this
   )
   ```
2. Run `cd native && go get github.com/nats-io/nats-server/v2@v2.11.0 && go mod tidy`
3. Rebuild: `./build.sh` or `.\build.ps1`
4. Test thoroughly with `cd src/MessageBroker.Examples && dotnet run -- test`

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

## File Locations Reference

### Configuration Models
- `src/MessageBroker.Core/Configuration/BrokerConfiguration.cs` - Main config model
- `src/MessageBroker.Core/Configuration/ConfigurationVersion.cs` - Version tracking
- `src/MessageBroker.Core/Configuration/ConfigurationDiff.cs` - Change detection

### Core Interfaces
- `src/MessageBroker.Core/Abstractions/IBrokerController.cs` - Main interface
- `src/MessageBroker.Core/Abstractions/IConfigurationValidator.cs` - Validation interface

### Implementation
- `src/MessageBroker.Nats/Implementation/NatsController.cs` - Main implementation
- `src/MessageBroker.Nats/ConfigurationMapper.cs` - Config mapping
- `src/MessageBroker.Nats/Extensions/NatsControllerExtensions.cs` - Fluent API

### Examples
- `src/MessageBroker.Examples/Program.cs` - Interactive examples
- `src/MessageBroker.Examples/SimpleTest.cs` - Automated tests

## Documentation

Comprehensive documentation is in the `docs/` folder:
- **GETTING_STARTED.md** (997 lines) - Installation, first steps
- **API_DESIGN.md** (1,067 lines) - Complete API reference
- **ARCHITECTURE.md** (930 lines) - Deep dive into system design
- **DIAGRAMS.md** (631 lines) - Visual architecture diagrams
- **QUICK_REFERENCE.md** (569 lines) - Cheat sheet
- **INDEX.md** (514 lines) - Navigation guide

Total: 5,592 lines, 196 KB of documentation

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

### Current Branch
- Working on: `claude/create-claude-md-file-01EUfrdXA8MJBcemVJ6mkFUp`
- Main branch: (to be determined)

### Commit Guidelines
- Use descriptive commit messages
- Reference issue numbers when applicable
- Keep commits focused and atomic

### Pushing Changes
- Always use: `git push -u origin <branch-name>`
- Branch must start with 'claude/' and end with matching session ID
- Retry network failures up to 4 times with exponential backoff (2s, 4s, 8s, 16s)

## Build Status

- **Last Build**: SUCCESS
- **Warnings**: 0
- **Errors**: 0
- **Tests**: 7/7 passing

## Version Information

- **Current Version**: 1.0.0
- **NATS Server Version**: 2.10.x / 2.11.0
- **.NET Version**: 9.0
- **C# Version**: 12
- **Last Updated**: November 2024

## Quick Reference

### Essential Commands

```bash
# Build
dotnet build MessageBroker.NET.sln

# Run examples
cd src/MessageBroker.Examples && dotnet run

# Run tests
cd src/MessageBroker.Examples && dotnet run -- test

# Clean
dotnet clean MessageBroker.NET.sln
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

// Shutdown
await controller.ShutdownAsync();
```

## Support Resources

- **Documentation**: `docs/` folder (5,592 lines)
- **Examples**: `src/MessageBroker.Examples/`
- **Architecture**: `docs/ARCHITECTURE.md`
- **API Reference**: `docs/API_DESIGN.md`
- **NATS Community**: https://slack.nats.io

---

**This file was last updated**: 2025-11-14
**Project Status**: Active development, all tests passing, ready for use
