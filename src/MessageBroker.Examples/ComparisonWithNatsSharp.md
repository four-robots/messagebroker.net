# MessageBroker.NET vs nats-csharp: Feature Comparison

This document provides a side-by-side comparison of MessageBroker.NET and the original nats-csharp implementation, highlighting the enhanced runtime configuration capabilities.

## Overview

**MessageBroker.NET** builds upon the nats-csharp bindings to provide enterprise-grade configuration management features that are essential for production environments. While nats-csharp provides basic server control, MessageBroker.NET adds sophisticated configuration lifecycle management.

## Key Enhancements

### 1. Hot Configuration Reload

#### Original nats-csharp Approach
```csharp
// Starting a server with nats-csharp
var config = new ServerConfig
{
    Host = "localhost",
    Port = 4222,
    Debug = false
};

var configJson = JsonSerializer.Serialize(config);
var result = NatsBindings.StartServer(configJson);

// To change configuration, you must:
// 1. Stop the server
// 2. Create new configuration
// 3. Restart the server
NatsBindings.ShutdownServer();

config.Debug = true; // Change setting
var newConfigJson = JsonSerializer.Serialize(config);
result = NatsBindings.StartServer(newConfigJson); // Restart required
```

**Limitations:**
- Requires server restart for any configuration change
- Downtime during configuration updates
- No validation before applying changes
- No rollback capability
- No change tracking

#### MessageBroker.NET Approach
```csharp
// Starting a server with MessageBroker.NET
var config = new BrokerConfiguration
{
    Host = "localhost",
    Port = 4222,
    Debug = false,
    Description = "Initial configuration"
};

var broker = new NatsBrokerController(config);
await broker.StartAsync();

// Hot reload - change configuration WITHOUT restarting
var result = await broker.ApplyChangesAsync(config =>
{
    config.Debug = true; // Change applied immediately
    config.Description = "Debug mode enabled";
});

if (result.Success)
{
    // Configuration applied without downtime!
    Console.WriteLine($"Applied version {result.Version.Version}");
    Console.WriteLine($"Changes: {result.Diff.Changes.Count} properties modified");
}
```

**Advantages:**
- Zero downtime configuration updates
- Instant changes without server restart
- Validation before applying
- Automatic version tracking
- Detailed change diff

---

### 2. Configuration Validation

#### Original nats-csharp Approach
```csharp
var config = new ServerConfig
{
    Port = -1, // Invalid port!
    MaxPayload = 0 // Invalid payload!
};

var configJson = JsonSerializer.Serialize(config);
var result = NatsBindings.StartServer(configJson);

// Server might fail to start or crash
// No validation - you find out when it breaks
```

**Limitations:**
- No validation before applying configuration
- Server crashes or fails with cryptic errors
- Manual validation required
- No feedback on what's wrong

#### MessageBroker.NET Approach
```csharp
var broker = new NatsBrokerController();

var result = await broker.ApplyChangesAsync(config =>
{
    config.Port = -1; // Invalid port!
    config.MaxPayload = 0; // Invalid payload!
});

if (!result.Success)
{
    // Configuration rejected BEFORE being applied
    Console.WriteLine($"Validation failed: {result.ErrorMessage}");
    // Server continues running with previous valid configuration
}
```

**Advantages:**
- Automatic validation before applying changes
- Clear error messages
- Server remains stable
- Invalid configurations never applied
- Custom validation rules supported

---

### 3. Version History and Rollback

#### Original nats-csharp Approach
```csharp
// No version tracking in nats-csharp
// To "rollback", you must:
// 1. Manually track previous configurations yourself
// 2. Stop the server
// 3. Restart with old configuration

var previousConfig = /* you must save this manually */;
NatsBindings.ShutdownServer();
var configJson = JsonSerializer.Serialize(previousConfig);
NatsBindings.StartServer(configJson);
```

**Limitations:**
- No built-in version tracking
- Manual configuration history management
- Requires server restart to rollback
- No diff between versions
- Easy to lose configuration history

#### MessageBroker.NET Approach
```csharp
var broker = new NatsBrokerController();

// Apply multiple configuration changes
await broker.ApplyChangesAsync(config => config.Port = 4223); // Version 2
await broker.ApplyChangesAsync(config => config.Debug = true); // Version 3
await broker.ApplyChangesAsync(config => config.MaxPayload = 10_000_000); // Version 4

// Oops, version 4 is causing issues!
// Rollback to previous version
var result = await broker.RollbackAsync();

// Or rollback to specific version
result = await broker.RollbackAsync(toVersion: 2);

// View complete history
var history = await broker.GetHistoryAsync();
foreach (var version in history)
{
    Console.WriteLine($"Version {version.Version}: {version.Configuration.Description}");
    Console.WriteLine($"Applied: {version.AppliedAt}");
}
```

**Advantages:**
- Automatic version tracking
- Instant rollback without restart
- Complete configuration history
- Rollback to any previous version
- Change type tracking (Initial, Update, Rollback)

---

### 4. Change Notifications and Events

#### Original nats-csharp Approach
```csharp
// No event system in nats-csharp
// You cannot react to configuration changes
// No way to monitor or log changes
// No way to cancel changes
```

**Limitations:**
- No event notifications
- Cannot monitor configuration changes
- Cannot react to or cancel changes
- No audit trail

#### MessageBroker.NET Approach
```csharp
var broker = new NatsBrokerController();

// Subscribe to configuration change events
broker.ConfigurationChanging += (sender, e) =>
{
    // Fired BEFORE change is applied
    Console.WriteLine($"About to change from {e.OldConfiguration.Port} to {e.NewConfiguration.Port}");

    // Cancel the change if needed
    if (e.NewConfiguration.Port > 9000)
    {
        e.Cancel = true; // Stop the change
    }
};

broker.ConfigurationChanged += (sender, e) =>
{
    // Fired AFTER change is applied
    Console.WriteLine($"Configuration changed to version {e.NewVersion}");
    Console.WriteLine($"Changes: {e.Diff.Changes.Count}");

    // Log to monitoring system
    // Update documentation
    // Notify other systems
};

// Apply configuration - events will fire
await broker.ApplyChangesAsync(config => config.Port = 4223);
```

**Advantages:**
- Event-driven change notifications
- Monitor all configuration changes
- Cancel changes before they apply
- Integration with logging/monitoring
- Audit trail capability

---

### 5. Fluent API

#### Original nats-csharp Approach
```csharp
// No fluent API - verbose configuration
var config = new ServerConfig();
config.Port = 4222;
config.MaxPayload = 5_242_880;
config.Debug = true;
config.Jetstream = true;
config.JetstreamStoreDir = "./data";

var configJson = JsonSerializer.Serialize(config);
NatsBindings.StartServer(configJson);
```

#### MessageBroker.NET Approach
```csharp
// Fluent API - chainable, readable configuration
var broker = new NatsBrokerController();

await broker
    .WithPortAsync(4222)
    .Result
    .WithMaxPayloadAsync(5_242_880)
    .Result
    .WithDebugAsync(true)
    .Result
    .WithJetStreamAsync("./data")
    .Result
    .WithMonitoringAsync(8222)
    .Result
    .WithAuthenticationAsync("admin", "password");

// Clean, readable, and self-documenting
```

**Advantages:**
- Chainable method calls
- More readable code
- IntelliSense-friendly
- Self-documenting
- Less boilerplate

---

## Feature Comparison Table

| Feature | nats-csharp | MessageBroker.NET |
|---------|-------------|-------------------|
| Basic server start/stop | ✓ | ✓ |
| Configuration reload | ✗ (requires restart) | ✓ (hot reload) |
| Configuration validation | ✗ | ✓ |
| Version history | ✗ | ✓ |
| Rollback capability | ✗ | ✓ |
| Change notifications | ✗ | ✓ |
| Configuration diff | ✗ | ✓ |
| Event-driven API | ✗ | ✓ |
| Fluent API | ✗ | ✓ |
| Zero-downtime updates | ✗ | ✓ |
| Audit trail | ✗ | ✓ |
| Custom validation rules | ✗ | ✓ |

---

## When to Use Each

### Use nats-csharp when:
- You need simple, basic NATS server control
- Configuration rarely changes
- Downtime during configuration changes is acceptable
- You're building a proof-of-concept or simple application

### Use MessageBroker.NET when:
- You need production-grade configuration management
- Zero downtime is required
- You need configuration validation and safety
- You want version history and rollback capabilities
- You need to monitor and react to configuration changes
- You're building enterprise applications
- You need audit trails and compliance features

---

## Migration Path

Migrating from nats-csharp to MessageBroker.NET is straightforward:

### Before (nats-csharp)
```csharp
var config = new ServerConfig
{
    Host = "localhost",
    Port = 4222,
    Jetstream = true
};

var configJson = JsonSerializer.Serialize(config);
var result = NatsBindings.StartServer(configJson);

// ... use server ...

NatsBindings.ShutdownServer();
```

### After (MessageBroker.NET)
```csharp
var config = new BrokerConfiguration
{
    Host = "localhost",
    Port = 4222,
    Jetstream = true,
    Description = "Production configuration"
};

var broker = new NatsBrokerController(config);
await broker.StartAsync();

// ... use server with hot reload capabilities ...

await broker.ShutdownAsync();
```

The API is similar, with the addition of async/await and enhanced features.

---

## Conclusion

MessageBroker.NET provides a production-ready wrapper around nats-csharp that adds essential configuration lifecycle management features. While nats-csharp is perfect for simple use cases, MessageBroker.NET is designed for enterprise applications that require:

- Zero downtime
- Configuration safety and validation
- Change tracking and audit trails
- Version history and rollback
- Event-driven monitoring

Both libraries have their place, and MessageBroker.NET builds upon nats-csharp without replacing it, adding a sophisticated configuration management layer on top.
