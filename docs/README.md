# DotGnatly Documentation

Welcome to the DotGnatly documentation! DotGnatly is a .NET library that provides full control over NATS server instances with runtime reconfiguration capabilities.

## What is DotGnatly?

DotGnatly enables you to:
- **Start and control NATS servers** programmatically from .NET
- **Hot-reload configuration** without restart or downtime
- **Manage multi-tenant accounts** with isolation
- **Enable JetStream persistence** for event sourcing
- **Monitor server health** with built-in observability

## Quick Start (30 seconds)

```csharp
using DotGnatly.Nats;

// Create and start NATS server
using var controller = new NatsController();
var config = new BrokerConfiguration { Port = 4222 };
await controller.ConfigureAsync(config);

Console.WriteLine("Server running at nats://localhost:4222");
Console.ReadKey();
```

## Documentation Files

### New Users - Start Here
1. **[GETTING_STARTED.md](./GETTING_STARTED.md)** - Installation and tutorials (30 min)
2. **[API_DESIGN.md](./API_DESIGN.md)** - Complete API reference with examples (30 min)
3. **[QUICK_REFERENCE.md](./QUICK_REFERENCE.md)** - Bookmark for quick lookups

### Advanced Topics
- **[ARCHITECTURE.md](./ARCHITECTURE.md)** - System design and deployment patterns
- **[DEV_MONITORING.md](./DEV_MONITORING.md)** - Monitoring and observability (dev reference)

### Navigation
- **[INDEX.md](./INDEX.md)** - Complete documentation index with reading paths

## Key Features

### 1. Hot Configuration Reload (Zero Downtime)
```csharp
// Start with debug enabled
await controller.ConfigureAsync(new BrokerConfiguration { Debug = true });

// Later: switch to production mode without restart
await controller.ApplyChangesAsync(c => c.Debug = false);
// ZERO DOWNTIME - clients stay connected
```

### 2. Type-Safe Configuration
```csharp
var config = new BrokerConfiguration
{
    Port = 4222,
    Jetstream = true,
    JetstreamStoreDir = "/data/jetstream",
    Debug = false
};
await controller.ConfigureAsync(config);
```

### 3. JetStream Persistence
```csharp
var config = new BrokerConfiguration
{
    Jetstream = true,
    JetstreamStoreDir = "./data",
    JetstreamMaxMemory = 1024L * 1024 * 1024,  // 1GB
    JetstreamMaxStore = 10L * 1024 * 1024 * 1024  // 10GB
};
```

### 4. Multi-Tenant Accounts
```csharp
// Create isolated accounts per customer
var config = new AccountConfiguration
{
    Name = "customer1",
    MaxConnections = 100,
    MaxSubscriptions = 1000
};
await controller.CreateAccountAsync(config);
```

## Why DotGnatly vs nats-csharp?

| Feature | nats-csharp | DotGnatly |
|---------|-------------|-----------|
| Server Control | ❌ No | ✅ Yes |
| Hot Reload | ❌ No | ✅ Yes |
| Account Management | ❌ No | ✅ Yes |
| Embedded Server | ❌ No | ✅ Yes |
| Client Operations | ✅ Yes | ✅ Via nats-csharp |
| Configuration Changes | Requires Restart | Zero Downtime |

**nats-csharp** = Client library (connects to external server)
**DotGnatly** = Server control + Client library (full lifecycle management)

## Next Steps

1. **Install**: See [GETTING_STARTED.md](./GETTING_STARTED.md#installation)
2. **Learn**: Read [API_DESIGN.md](./API_DESIGN.md) for complete examples
3. **Reference**: Bookmark [QUICK_REFERENCE.md](./QUICK_REFERENCE.md)
4. **Explore**: Browse [INDEX.md](./INDEX.md) for all documentation

## Support

- **Documentation**: [INDEX.md](./INDEX.md)
- **GitHub Issues**: Report bugs and request features
- **GitHub Discussions**: Ask questions
- **NATS Community**: [slack.nats.io](https://slack.nats.io)

---

**Version**: 1.0.0 | **Updated**: November 2024 | **License**: Apache 2.0

Happy coding with DotGnatly! 🚀
