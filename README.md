# MessageBroker.NET

Full control over NATS servers with runtime reconfiguration via Go bindings.

## What is MessageBroker.NET?

MessageBroker.NET is a .NET library that provides programmatic control over NATS server instances, enabling:

- **Full Server Control**: Start, stop, and configure NATS servers from C# code
- **Hot Configuration Reload**: Update settings without restart or downtime
- **Embedded Server**: No external dependencies, runs in-process
- **Type-Safe Configuration**: Strongly-typed configuration with compile-time checking
- **JetStream Support**: First-class persistence and streaming
- **Multi-tenancy**: Built-in account and user management

## Key Differentiator

Unlike standard nats-csharp (client library only), MessageBroker.NET provides **server control** with **runtime reconfiguration**:

```csharp
// Standard nats-csharp - Client only
var conn = new ConnectionFactory().CreateConnection("nats://localhost:4222");
// Requires external NATS server, cannot control or reconfigure

// MessageBroker.NET - Full server control
using var server = new NatsServer();
server.Start(new ServerConfig { Port = 4222, Jetstream = true });

// Hot reload configuration (ZERO DOWNTIME)
server.UpdateConfig(new ServerConfig { Debug = false });
```

## 30-Second Quick Start

```csharp
using NatsSharp;

using var server = new NatsServer();
string url = server.Start();

Console.WriteLine($"NATS Server running at: {url}");
Console.ReadKey();
```

## Documentation

Comprehensive documentation is available in the **[docs](./docs)** folder:

### Quick Links

- **[Getting Started](./docs/GETTING_STARTED.md)** - Installation and first steps
- **[API Design](./docs/API_DESIGN.md)** - Complete API reference with examples
- **[Architecture](./docs/ARCHITECTURE.md)** - System design and internals
- **[Quick Reference](./docs/QUICK_REFERENCE.md)** - Cheat sheet and patterns
- **[Diagrams](./docs/DIAGRAMS.md)** - Visual architecture diagrams
- **[Documentation Index](./docs/INDEX.md)** - Complete documentation index

### Documentation Stats

- **8 files**: Complete coverage from basics to advanced
- **5,600+ lines**: Detailed, comprehensive documentation
- **125+ examples**: Practical, runnable code examples
- **All use cases**: Installation, configuration, deployment, troubleshooting

## Features

### Runtime Configuration Management

```csharp
// Start with initial config
server.Start(new ServerConfig
{
    Debug = true,
    HTTPPort = 8222
});

// Later: Hot reload without restart
server.UpdateConfig(new ServerConfig
{
    Debug = false,      // Disable debug logging
    HTTPPort = 8223     // Change monitoring port
});
// Zero downtime, clients stay connected
```

### JetStream Persistence

```csharp
server.Start(new ServerConfig
{
    Jetstream = true,
    JetstreamStoreDir = "./data/jetstream",
    JetstreamMaxMemory = 1024L * 1024 * 1024,  // 1GB
    JetstreamMaxStore = 10L * 1024 * 1024 * 1024  // 10GB
});
```

### Multi-Tenant Accounts

```csharp
server.Start();

var account = server.CreateAccount(new AccountConfig
{
    Name = "tenant1",
    MaxConnections = 100,
    MaxSubscriptions = 1000
});
```

### Server Monitoring

```csharp
server.Start(new ServerConfig
{
    HTTPPort = 8222,
    HTTPHost = "0.0.0.0"
});
// Access monitoring at http://localhost:8222/varz

var info = server.GetInfo();
Console.WriteLine($"Connections: {info.Connections}");
Console.WriteLine($"Version: {info.Version}");
```

## Installation

### NuGet Package (Recommended)

```bash
dotnet add package MessageBroker.NET
```

### Build from Source

```bash
# Clone repository
git clone https://github.com/four-robots/messagebroker.net.git
cd messagebroker.net

# Build native Go bindings first
cd native
./build.sh           # Linux/macOS
# or
.\build.ps1          # Windows (PowerShell)

# Build .NET library
cd ..
dotnet build MessageBroker.NET.sln

# Run examples
cd src/MessageBroker.Examples
dotnet run
```

See **[native/README.md](./native/README.md)** for detailed build instructions and cross-platform builds.

## Platform Support

- **Windows**: 10+ (x64)
- **Linux**: Ubuntu 20.04+, RHEL 8+ (x64)
- **.NET**: 9.0 or later

## Architecture

```
┌─────────────────────────────────────────┐
│         C# Application                   │
│  ┌────────────────────────────────────┐ │
│  │   NatsServer Wrapper (Type-safe)   │ │
│  └─────────────┬──────────────────────┘ │
│                │ P/Invoke                │
│  ┌─────────────┴──────────────────────┐ │
│  │  Platform Bindings (DLL/SO)        │ │
│  └─────────────┬──────────────────────┘ │
└────────────────┼────────────────────────┘
                 │ CGO Bridge
┌────────────────┴────────────────────────┐
│     Go Bindings Layer                    │
│  ┌──────────────────────────────────┐  │
│  │  NATS Server (Embedded)          │  │
│  │  github.com/nats-io/nats-server  │  │
│  └──────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

See **[ARCHITECTURE.md](./docs/ARCHITECTURE.md)** for detailed architecture documentation.

## Use Cases

1. **Microservices Internal Messaging**: Embedded NATS for service-to-service communication
2. **Event Sourcing**: JetStream persistence for event streams
3. **Multi-Tenant SaaS**: Isolated accounts per customer
4. **Edge Computing**: Hub-and-spoke with leaf nodes
5. **Development/Testing**: Fast, isolated NATS instances per test

## Comparison with nats-csharp

| Feature | nats-csharp | MessageBroker.NET |
|---------|-------------|-------------------|
| Client Operations | Yes | Yes (via nats-csharp) |
| Server Control | No | **Yes** |
| Hot Configuration Reload | No | **Yes** |
| Account Management | No | Yes |
| Embedded Server | No | Yes |
| External Dependencies | NATS server required | None |

See **[API_DESIGN.md](./docs/API_DESIGN.md)** for detailed comparison.

## Examples

### Production Server

```csharp
var config = new ServerConfig
{
    Host = "0.0.0.0",
    Port = 4222,

    // Performance
    MaxPayload = 8 * 1024 * 1024,
    PingInterval = 120,

    // JetStream
    Jetstream = true,
    JetstreamStoreDir = "/var/lib/nats/jetstream",
    JetstreamMaxMemory = 4L * 1024 * 1024 * 1024,

    // Monitoring
    HTTPPort = 8222,

    // Security
    Auth = new AuthConfig
    {
        Username = Environment.GetEnvironmentVariable("NATS_USER"),
        Password = Environment.GetEnvironmentVariable("NATS_PASS")
    }
};

server.Start(config);
```

More examples in **[GETTING_STARTED.md](./docs/GETTING_STARTED.md)**.

## Performance

- **Startup**: 200-600ms (depends on JetStream)
- **Hot Reload**: 5-20ms (zero client disconnections)
- **Message Throughput**: Same as native NATS (10M+ msgs/sec)
- **Memory**: ~50-100MB base + JetStream overhead

## Contributing

Contributions welcome! Please:

1. Read the documentation in **[docs/](./docs)**
2. Fork the repository
3. Create a feature branch
4. Submit a pull request

## License

MessageBroker.NET is licensed under the Apache License 2.0.

NATS Server (embedded) is licensed under the Apache License 2.0.

## Support

- **Documentation**: **[docs/](./docs)**
- **Issues**: GitHub Issues
- **Discussions**: GitHub Discussions
- **NATS Community**: [slack.nats.io](https://slack.nats.io)

## Credits

Built on top of:
- [NATS Server](https://github.com/nats-io/nats-server) - High-performance messaging system
- [nats-csharp](https://github.com/nats-io/nats.net) - .NET client library (optional)

## Version

**Current Version**: 1.0.0
**NATS Server Version**: 2.10.x
**Last Updated**: November 2024

---

**Get Started**: See **[GETTING_STARTED.md](./docs/GETTING_STARTED.md)**

**Full Documentation**: See **[docs/README.md](./docs/README.md)**
