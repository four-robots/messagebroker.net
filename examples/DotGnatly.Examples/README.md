# DotGnatly Examples

This project contains comprehensive, runnable examples demonstrating all key features of DotGnatly, with a focus on the enhanced runtime reconfiguration capabilities compared to the original nats-csharp implementation.

## Quick Start

Run the interactive examples application:

```bash
cd src/DotGnatly.Examples
dotnet run
```

This will launch an interactive menu where you can explore each example.

## Examples Overview

### Basic Usage

#### 1. Basic Server Startup (`BasicUsage/BasicServerExample.cs`)
Demonstrates the fundamental server lifecycle:
- Creating a basic configuration
- Starting the broker
- Retrieving server information
- Graceful shutdown

**Key Concepts:**
- Server initialization
- Configuration basics
- Server info querying

### Hot Reload Features

#### 2. Hot Configuration Reload (`HotReload/ConfigurationReloadExample.cs`)
Showcases the power of runtime configuration changes:
- Start server with initial configuration
- Change port without restart
- Increase max payload on-the-fly
- Enable debug logging dynamically
- View version history

**Key Concepts:**
- Zero-downtime configuration updates
- Version tracking
- Configuration history

#### 3. Configuration Validation (`HotReload/ValidationExample.cs`)
Demonstrates how validation prevents invalid configurations:
- Attempt invalid port values (negative, out of range)
- Try invalid payload sizes
- Show validation errors
- Apply valid configuration

**Key Concepts:**
- Automatic validation
- Error handling
- Configuration safety

#### 4. Rollback Example (`HotReload/RollbackExample.cs`)
Shows how to recover from problematic configuration changes:
- Apply multiple configuration versions
- Simulate a problematic configuration
- Rollback to previous version
- Rollback to specific version by number
- View rollback history

**Key Concepts:**
- Version management
- Rollback capabilities
- Configuration recovery

### Advanced Features

#### 5. Change Notifications (`Advanced/ChangeNotificationExample.cs`)
Demonstrates the event-driven notification system:
- Subscribe to ConfigurationChanging event (before change)
- Subscribe to ConfigurationChanged event (after change)
- Cancel a configuration change via event handler
- Monitor all configuration activity

**Key Concepts:**
- Event-driven architecture
- Change monitoring
- Cancellation support
- Audit trails

#### 6. Fluent API Usage (`Advanced/FluentApiExample.cs`)
Compares traditional vs. fluent API approaches:
- Traditional step-by-step configuration
- Fluent chainable method calls
- Extension methods for common tasks
- Production-ready configuration chains

**Key Concepts:**
- Fluent interfaces
- Method chaining
- Clean, readable code
- Extension methods

#### 7. Complete Workflow (`Advanced/CompleteWorkflowExample.cs`)
A production-ready end-to-end example combining all features:
- Production configuration with JetStream
- Authentication and monitoring
- Event monitoring throughout lifecycle
- Multiple hot reloads
- Validation testing
- Configuration history review
- Graceful shutdown

**Key Concepts:**
- Production patterns
- Complete lifecycle management
- Integration of all features

## File Structure

```
DotGnatly.Examples/
├── Program.cs                           # Interactive menu application
├── MockBrokerController.cs              # Mock implementation for examples
├── BrokerControllerExtensions.cs        # Fluent API extension methods
├── ComparisonWithNatsSharp.md          # Feature comparison document
├── README.md                            # This file
│
├── BasicUsage/
│   └── BasicServerExample.cs            # Example 1: Basic server startup
│
├── HotReload/
│   ├── ConfigurationReloadExample.cs    # Example 2: Hot reload
│   ├── ValidationExample.cs             # Example 3: Validation
│   └── RollbackExample.cs               # Example 4: Rollback
│
└── Advanced/
    ├── ChangeNotificationExample.cs     # Example 5: Events
    ├── FluentApiExample.cs              # Example 6: Fluent API
    └── CompleteWorkflowExample.cs       # Example 7: Complete workflow
```

## Key Features Demonstrated

### Hot Configuration Reload
Unlike nats-csharp which requires server restarts, DotGnatly allows:
- Zero-downtime configuration changes
- Instant application of new settings
- No client disconnections

### Configuration Validation
Automatic validation ensures:
- Invalid configurations never applied
- Clear error messages
- Server stability maintained

### Version History & Rollback
Track all configuration changes:
- Complete version history
- Rollback to any previous version
- Change type tracking (Initial, Update, Rollback)

### Event-Driven Monitoring
React to configuration changes:
- Before-change events (with cancellation)
- After-change events (with diff details)
- Perfect for logging and auditing

### Fluent API
Write clean, readable configuration code:
- Chainable method calls
- IntelliSense-friendly
- Self-documenting

## Running Individual Examples

Each example is self-contained and can be run from the interactive menu. Simply:

1. Run `dotnet run`
2. Select the example number (1-7)
3. Follow the on-screen output
4. Press any key to return to the menu

## Understanding the Mock Implementation

The examples use `MockBrokerController` to simulate a real NATS broker without requiring actual server infrastructure. This mock implementation:
- Demonstrates the IBrokerController interface
- Simulates configuration changes
- Shows how events fire
- Maintains version history

In production, you would use the real `NatsBrokerController` implementation that actually manages NATS server processes.

## Comparison with nats-csharp

See [ComparisonWithNatsSharp.md](./ComparisonWithNatsSharp.md) for a detailed side-by-side comparison showing:
- Feature differences
- Code examples (before/after)
- When to use each approach
- Migration path

## Key Takeaways

1. **Zero Downtime**: Configuration changes apply instantly without server restarts
2. **Safety First**: Validation prevents invalid configurations from breaking your server
3. **Version Control**: Complete history with rollback capabilities
4. **Event-Driven**: Monitor and react to all configuration changes
5. **Developer Friendly**: Fluent API makes configuration code clean and readable

## Next Steps

After exploring these examples:

1. Read the [comparison document](./ComparisonWithNatsSharp.md)
2. Review the actual implementation in `DotGnatly.Core`
3. Try implementing a real broker controller for NATS
4. Integrate DotGnatly into your own applications

## Requirements

- .NET 9.0 or later
- DotGnatly.Core library
- DotGnatly.Nats library (for actual NATS integration)

## License

This example project is part of DotGnatly and follows the same license as the main project.
