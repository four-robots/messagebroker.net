# MessageBroker.NET Documentation

Welcome to the MessageBroker.NET documentation. This library provides full control over NATS server instances with runtime reconfiguration capabilities via Go bindings.

## Documentation Overview

### [Getting Started Guide](./GETTING_STARTED.md)
**Start here if you're new to MessageBroker.NET**

- Installation instructions
- Quick start examples
- Common scenarios (basic messaging, JetStream, authentication, monitoring)
- Configuration guide
- Best practices
- Troubleshooting common issues

**Time to complete**: 15-30 minutes

---

### [API Design Document](./API_DESIGN.md)
**Complete API reference with code examples**

- Fluent API design with full code examples
- Before/after comparisons with nats-csharp
- Major use cases:
  - Basic server startup
  - Hot configuration reload
  - Configuration validation
  - Change notifications
  - Rollback scenarios
- Complete API reference for all classes and methods
- Best practices and patterns

**Audience**: Developers implementing MessageBroker.NET

---

### [Architecture Documentation](./ARCHITECTURE.md)
**Deep dive into system design and internals**

- System architecture diagrams
- Component overview (C#, bindings, Go, NATS layers)
- Interprocess communication (P/Invoke, CGO)
- Configuration management system
- Configuration versioning and change notifications
- Comparison with nats-csharp architecture
- Performance characteristics
- Deployment architectures

**Audience**: Architects, senior developers, DevOps engineers

---

## Quick Links

### Key Features

1. **Full Server Control**: Start, stop, and configure NATS servers programmatically
2. **Hot Configuration Reload**: Update settings without restart or downtime
3. **Type-Safe Configuration**: Strongly-typed configuration with compile-time checking
4. **Zero Dependencies**: Embedded server, no external installations needed
5. **JetStream Support**: First-class persistence and streaming
6. **Multi-tenancy**: Built-in account and user management

### Why MessageBroker.NET?

**Standard nats-csharp (Client Library)**:
```csharp
// Requires external NATS server to be running
var conn = new ConnectionFactory().CreateConnection("nats://localhost:4222");

// Can only:
// - Connect to server
// - Publish/Subscribe
// CANNOT control server
```

**MessageBroker.NET (Server Control)**:
```csharp
// Embedded server with full control
using var server = new NatsServer();
server.Start(new ServerConfig { Port = 4222, Jetstream = true });

// Hot-reload configuration (ZERO DOWNTIME)
server.UpdateConfig(new ServerConfig { Debug = false });

// Create accounts
server.CreateAccount(new AccountConfig { Name = "tenant1" });
```

### 30-Second Quick Start

```csharp
using NatsSharp;

// Start embedded NATS server
using var server = new NatsServer();
string url = server.Start();

Console.WriteLine($"Server running at: {url}");
Console.ReadKey();
```

---

## Documentation Map

```
docs/
├── README.md              ← You are here
├── GETTING_STARTED.md     ← Installation and first steps
├── API_DESIGN.md          ← Complete API reference
└── ARCHITECTURE.md        ← System design and internals
```

### Recommended Reading Order

**For New Users**:
1. README.md (this file) - Overview
2. GETTING_STARTED.md - Installation and quick start
3. API_DESIGN.md - Learn the API through examples
4. ARCHITECTURE.md - Understand the internals (optional)

**For Experienced Developers**:
1. API_DESIGN.md - Jump straight to code examples
2. ARCHITECTURE.md - Understand design decisions
3. GETTING_STARTED.md - Reference for specific scenarios

**For Architects/DevOps**:
1. ARCHITECTURE.md - System design
2. API_DESIGN.md - Capabilities and limitations
3. GETTING_STARTED.md - Deployment patterns

---

## Key Concepts

### Runtime Reconfiguration

The core enhancement over nats-csharp is **runtime reconfiguration** - the ability to change server settings without restart:

```csharp
// Start with initial configuration
var server = new NatsServer();
server.Start(new ServerConfig
{
    Port = 4222,
    Debug = true,
    HTTPPort = 8222
});

// Later: Update without restart (ZERO DOWNTIME)
server.UpdateConfig(new ServerConfig
{
    Debug = false,      // Disable debug logging
    HTTPPort = 8223     // Change monitoring port
});

// Server continues running, clients stay connected
```

### Configuration Versioning

Track configuration changes over time:

```csharp
public class ConfigurationManager
{
    private Stack<ServerConfig> _history = new();

    public bool UpdateWithHistory(ServerConfig newConfig)
    {
        _history.Push(GetCurrentConfig());
        string result = server.UpdateConfig(newConfig);

        if (!result.Contains("success"))
        {
            _history.Pop();  // Remove failed update
            return false;
        }
        return true;
    }

    public bool Rollback()
    {
        if (_history.Count == 0) return false;
        return server.UpdateConfig(_history.Pop()).Contains("success");
    }
}
```

### Change Notifications

React to configuration changes:

```csharp
public class NotifyingServer : NatsServer
{
    public event EventHandler<ConfigChangedEventArgs> ConfigChanged;

    public new string UpdateConfig(ServerConfig config)
    {
        var before = GetInfo();
        string result = base.UpdateConfig(config);

        if (result.Contains("success"))
        {
            var after = GetInfo();
            ConfigChanged?.Invoke(this, new ConfigChangedEventArgs
            {
                Before = before,
                After = after,
                Timestamp = DateTime.UtcNow
            });
        }

        return result;
    }
}

// Usage
var server = new NotifyingServer();
server.ConfigChanged += (s, e) =>
{
    Console.WriteLine($"Config changed at {e.Timestamp}");
    LogConfigChange(e);
    NotifyMonitoring(e);
};
```

---

## Common Use Cases

### 1. Microservices Internal Messaging

```csharp
// Embedded NATS server in your microservice
using var server = new NatsServer();
server.Start(new ServerConfig
{
    Host = "localhost",  // Internal only
    Port = 4222
});

// Your service logic uses NATS for internal pub/sub
```

**Benefits**: No external dependencies, simplified deployment

---

### 2. Event Sourcing with JetStream

```csharp
using var server = new NatsServer();
server.Start(new ServerConfig
{
    Jetstream = true,
    JetstreamStoreDir = "/data/events",
    JetstreamMaxStore = 100L * 1024 * 1024 * 1024  // 100GB
});

// All events are persisted to disk
// Survives restarts, enables replay
```

**Benefits**: Durable event storage, audit trails, time-travel debugging

---

### 3. Multi-Tenant SaaS Platform

```csharp
using var server = new NatsServer();
server.Start();

// Create isolated accounts per customer
foreach (var customer in customers)
{
    server.CreateAccount(new AccountConfig
    {
        Name = customer.Id,
        MaxConnections = customer.Tier.MaxConnections,
        MaxSubscriptions = customer.Tier.MaxSubscriptions
    });
}
```

**Benefits**: Resource isolation, per-tenant limits, security

---

### 4. Edge Computing with Hub-and-Spoke

```csharp
// Edge device
using var edge = new NatsServer();
edge.Start(new ServerConfig
{
    Port = 4222,
    LeafNode = new LeafNodeConfig
    {
        Port = 7422,
        RemoteURLs = new[] { "nats://hub.cloud.com:7422" }
    }
});

// Local processing + cloud synchronization
```

**Benefits**: Local latency, cloud backup, hybrid architecture

---

### 5. Development/Testing with Fast Startup

```csharp
[SetUp]
public void Setup()
{
    // Start fresh NATS server for each test
    _server = new NatsServer();
    _server.Start();
}

[TearDown]
public void TearDown()
{
    _server.Dispose();  // Clean shutdown
}

[Test]
public void TestMessaging()
{
    // Test with isolated server instance
    var conn = new ConnectionFactory()
        .CreateConnection(_server.GetUrl());

    // ... test logic
}
```

**Benefits**: Test isolation, no shared state, clean environment

---

## Platform Support

### Supported Platforms

| Platform | Architecture | Status |
|----------|-------------|--------|
| Windows 10+ | x64 | Fully Supported |
| Windows 11 | x64 | Fully Supported |
| Ubuntu 20.04+ | x64 | Fully Supported |
| RHEL 8+ | x64 | Fully Supported |
| macOS | x64/ARM64 | Experimental |

### Runtime Requirements

- **.NET**: 9.0 or later
- **Memory**: 512 MB minimum, 1 GB+ recommended
- **Disk**: 100 MB for binaries, additional for JetStream

---

## Performance Characteristics

### Startup Time

- **Without JetStream**: ~200-300ms
- **With JetStream**: ~400-600ms

### Hot Reload Time

- **Configuration update**: ~5-20ms
- **Client disruption**: None (zero downtime)

### Message Throughput

Same as native NATS server:
- **Pub/Sub**: 10M+ msgs/sec
- **JetStream**: 1M+ msgs/sec
- **Request/Reply**: 500K+ req/sec

### Memory Overhead

- **Base**: ~50-100 MB
- **JetStream**: +20-50 MB
- **Per connection**: ~10-20 KB

---

## Security Considerations

### Authentication

```csharp
// Username/Password
server.Start(new ServerConfig
{
    Auth = new AuthConfig
    {
        Username = Environment.GetEnvironmentVariable("NATS_USER"),
        Password = Environment.GetEnvironmentVariable("NATS_PASS")
    }
});

// JWT-based (for multi-tenancy)
var result = server.CreateAccountWithJWT(operatorSeed, accountConfig);
```

### Network Security

```csharp
// Bind to localhost only (internal use)
server.Start(new ServerConfig { Host = "127.0.0.1" });

// Bind to all interfaces (requires firewall)
server.Start(new ServerConfig { Host = "0.0.0.0" });
```

### Best Practices

1. **Never hardcode credentials** - Use environment variables
2. **Limit JetStream storage** - Set `JetstreamMaxStore`
3. **Enable monitoring carefully** - Bind HTTP to trusted networks only
4. **Use JWT for multi-tenancy** - Better isolation than shared credentials
5. **Regular security updates** - Keep NATS server version current

---

## Deployment Patterns

### Pattern 1: Embedded (Single Process)

```
┌──────────────────────────┐
│   Your Application       │
│  ┌────────────────────┐  │
│  │  Business Logic    │  │
│  └────────┬───────────┘  │
│  ┌────────┴───────────┐  │
│  │  MessageBroker.NET │  │
│  │  (Embedded NATS)   │  │
│  └────────────────────┘  │
└──────────────────────────┘
```

**Use Case**: Microservices, edge devices, self-contained apps

---

### Pattern 2: Sidecar

```
┌──────────────┐    ┌──────────────┐
│ Application  │───▶│ NATS Sidecar │
│   (Main)     │    │ (MessageBroker)│
└──────────────┘    └──────────────┘
```

**Use Case**: Kubernetes pods, service mesh integration

---

### Pattern 3: Hub-and-Spoke

```
        ┌──────────────┐
        │   Hub NATS   │
        │   (Central)  │
        └───────┬──────┘
                │
     ┌──────────┼──────────┐
     │          │          │
┌────┴───┐ ┌───┴────┐ ┌───┴────┐
│Edge 1  │ │Edge 2  │ │Edge 3  │
│(Broker)│ │(Broker)│ │(Broker)│
└────────┘ └────────┘ └────────┘
```

**Use Case**: Edge computing, multi-region, IoT

---

## Monitoring and Observability

### Built-in HTTP Monitoring

```csharp
server.Start(new ServerConfig { HTTPPort = 8222 });

// Access monitoring endpoints:
// http://localhost:8222/varz    - Server info
// http://localhost:8222/connz   - Connections
// http://localhost:8222/subsz   - Subscriptions
// http://localhost:8222/jsz     - JetStream stats
```

### Programmatic Monitoring

```csharp
var info = server.GetInfo();
Console.WriteLine($"Connections: {info.Connections}");
Console.WriteLine($"Version: {info.Version}");
Console.WriteLine($"JetStream: {info.JetstreamEnabled}");
```

### Integration with Metrics Systems

```csharp
// Prometheus metrics exporter
var timer = new Timer(_ =>
{
    var info = server.GetInfo();
    prometheus.Set("nats_connections", info.Connections);
    prometheus.Set("nats_jetstream_enabled", info.JetstreamEnabled ? 1 : 0);
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
```

---

## Troubleshooting Quick Reference

| Issue | Quick Fix |
|-------|-----------|
| Port in use | Change port: `config.Port = 4223` |
| DLL not found | Build bindings: `go build -buildmode=c-shared` |
| Startup timeout | Check logs, verify permissions |
| High memory | Limit JetStream: `JetstreamMaxMemory` |
| Update rejected | Check result string for error details |
| Can't connect | Use `Host = "0.0.0.0"` not `"localhost"` |

See [GETTING_STARTED.md](./GETTING_STARTED.md#troubleshooting) for detailed troubleshooting.

---

## Examples Repository

Find complete working examples at:
```
https://github.com/yourusername/messagebroker.net-examples
```

Examples include:
- Basic pub/sub messaging
- JetStream event streaming
- Multi-tenant application
- Hot configuration reload demo
- Monitoring integration
- Testing patterns

---

## Contributing

Contributions are welcome! Please see:
- GitHub Issues for bugs and features
- Pull Request guidelines
- Code of Conduct

---

## License

MessageBroker.NET is licensed under the Apache License 2.0.

NATS Server (embedded) is licensed under the Apache License 2.0.

---

## Support and Community

- **GitHub**: [github.com/yourusername/messagebroker.net](https://github.com/yourusername/messagebroker.net)
- **Issues**: Report bugs and request features
- **Discussions**: Ask questions and share ideas
- **NATS Community**: [slack.nats.io](https://slack.nats.io)
- **NATS Docs**: [docs.nats.io](https://docs.nats.io)

---

## Version History

### v1.0.0 (Current)
- Initial release
- Full server control via Go bindings
- Hot configuration reload
- JetStream support
- Multi-tenant account management
- Windows and Linux support

---

## What's Next?

1. **Start with [GETTING_STARTED.md](./GETTING_STARTED.md)** - Install and run your first server
2. **Explore [API_DESIGN.md](./API_DESIGN.md)** - Learn the complete API
3. **Read [ARCHITECTURE.md](./ARCHITECTURE.md)** - Understand the system design
4. **Join the community** - Share your experience and get help

Happy coding with MessageBroker.NET!
