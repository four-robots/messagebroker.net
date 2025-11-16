# DotGnatly Quick Reference

## Installation

```bash
dotnet add package DotGnatly
```

## 30-Second Quick Start

```csharp
using DotGnatly.Nats;

using var controller = new NatsController();
string url = controller.Start();
Console.WriteLine($"Server: {url}");
Console.ReadKey();
```

---

## Common Patterns

### Basic Server

```csharp
using var controller = new NatsController();
controller.Start(new BrokerConfiguration
{
    Host = "0.0.0.0",
    Port = 4222
});
```

### JetStream Enabled

```csharp
controller.Start(new BrokerConfiguration
{
    Jetstream = true,
    JetstreamStoreDir = "./data/jetstream",
    JetstreamMaxMemory = 1024L * 1024 * 1024,  // 1GB
    JetstreamMaxStore = 10L * 1024 * 1024 * 1024  // 10GB
});
```

### With Authentication

```csharp
controller.Start(new BrokerConfiguration
{
    Auth = new AuthConfig
    {
        Username = "admin",
        Password = "password"
    }
});
```

### With Monitoring

```csharp
controller.Start(new BrokerConfiguration
{
    HTTPPort = 8222,
    HTTPHost = "0.0.0.0"
});
// Access: http://localhost:8222/varz
```

### Hot Configuration Reload

```csharp
// Start
controller.Start(new BrokerConfiguration { Debug = true });

// Later: Update without restart
controller.UpdateConfig(new BrokerConfiguration { Debug = false });
// ZERO DOWNTIME
```

### Multi-Tenant Accounts

```csharp
controller.Start();

var account = controller.CreateAccount(new AccountConfig
{
    Name = "tenant1",
    MaxConnections = 100,
    MaxSubscriptions = 1000
});
```

---

## Configuration Reference

### BrokerConfiguration Properties

```csharp
var config = new BrokerConfiguration
{
    // Network
    Host = "0.0.0.0",              // Bind address
    Port = 4222,                    // Client port

    // Limits
    MaxPayload = 1048576,           // 1MB max message
    MaxControlLine = 4096,          // Max protocol line
    PingInterval = 120,             // Seconds between pings
    MaxPingsOut = 2,                // Max unanswered pings
    WriteDeadline = 2,              // Write timeout (seconds)

    // JetStream
    Jetstream = true,               // Enable JetStream
    JetstreamStoreDir = "./js",     // Storage directory
    JetstreamMaxMemory = -1,        // Memory limit (-1 = unlimited)
    JetstreamMaxStore = -1,         // Disk limit (-1 = unlimited)

    // Monitoring
    HTTPPort = 8222,                // HTTP monitoring port
    HTTPHost = "0.0.0.0",          // HTTP bind address
    HTTPSPort = 0,                  // HTTPS port (0 = disabled)

    // Logging
    Debug = false,                  // Debug logging
    Trace = false,                  // Trace logging

    // Security
    Auth = new AuthConfig
    {
        Username = "admin",
        Password = "password",
        Token = "token123"
    },

    // Leaf Nodes
    LeafNode = new LeafNodeConfig
    {
        Port = 7422,
        RemoteURLs = new[] { "nats://hub:7422" }
    }
};
```

---

## API Methods

### Server Lifecycle

```csharp
// Start server
string url = controller.Start(config);
string url = controller.Start("localhost", 4222);
string url = controller.StartWithJetStream("localhost", 4222, "./js");
string url = controller.StartFromConfigFile("./nats.conf");

// Shutdown
controller.Shutdown();
controller.Dispose();  // Also calls Shutdown()
```

### Configuration Management

```csharp
// Hot reload
string result = controller.Reload();
string result = controller.ReloadFromFile("./nats.conf");
string result = controller.UpdateConfig(newConfig);
```

### Monitoring

```csharp
// Get URL
string url = controller.GetUrl();

// Get server info
ServerInfo info = controller.GetInfo();
Console.WriteLine($"ID: {info.Id}");
Console.WriteLine($"Version: {info.Version}");
Console.WriteLine($"Connections: {info.Connections}");
Console.WriteLine($"JetStream: {info.JetstreamEnabled}");
```

### Account Management

```csharp
// Create account
AccountInfo acc = controller.CreateAccount(new AccountConfig
{
    Name = "myaccount",
    MaxConnections = 100
});

// Create JWT account
JWTAccountResult jwt = controller.CreateAccountWithJWT(
    operatorSeed,
    accountConfig
);
Console.WriteLine($"JWT: {jwt.JWT}");
Console.WriteLine($"Seed: {jwt.Seed}");
Console.WriteLine($"PubKey: {jwt.PublicKey}");
```

---

## HTTP Monitoring Endpoints

When `HTTPPort` is configured:

| Endpoint | Description |
|----------|-------------|
| `/varz` | General server info |
| `/connz` | Connection details |
| `/subsz` | Subscription info |
| `/jsz` | JetStream stats |
| `/routez` | Route info |
| `/gatewayz` | Gateway info |
| `/leafz` | Leaf node info |
| `/accountz` | Account details |

**Example**: `http://localhost:8222/varz`

---

## Environment Variables Pattern

```csharp
var config = new BrokerConfiguration
{
    Host = Environment.GetEnvironmentVariable("NATS_HOST") ?? "0.0.0.0",
    Port = int.Parse(Environment.GetEnvironmentVariable("NATS_PORT") ?? "4222"),

    Auth = new AuthConfig
    {
        Username = Environment.GetEnvironmentVariable("NATS_USER"),
        Password = Environment.GetEnvironmentVariable("NATS_PASS")
    },

    Jetstream = bool.Parse(
        Environment.GetEnvironmentVariable("NATS_JETSTREAM") ?? "false"
    ),
    JetstreamStoreDir = Environment.GetEnvironmentVariable("NATS_STORE_DIR")
        ?? "./jetstream"
};
```

---

## Error Handling

```csharp
try
{
    string result = controller.Start(config);

    if (result.Contains("failed") || result.Contains("error"))
    {
        Console.WriteLine($"Startup failed: {result}");
        return;
    }

    Console.WriteLine($"Started: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}
```

---

## Testing Pattern

```csharp
public class MyTests
{
    private NatsController _controller;

    [SetUp]
    public void Setup()
    {
        _server = new NatsController();
        _controller.Start();
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public void TestMessaging()
    {
        var conn = new ConnectionFactory()
            .CreateConnection(_controller.GetUrl());

        // Test logic
        conn.Publish("test", Encoding.UTF8.GetBytes("hello"));

        conn.Close();
    }
}
```

---

## Production Configuration Template

```csharp
var config = new BrokerConfiguration
{
    // Network
    Host = "0.0.0.0",
    Port = 4222,

    // Performance
    MaxPayload = 8 * 1024 * 1024,      // 8MB
    PingInterval = 120,
    MaxPingsOut = 2,
    WriteDeadline = 10,

    // JetStream
    Jetstream = true,
    JetstreamStoreDir = "/var/lib/nats/jetstream",
    JetstreamMaxMemory = 4L * 1024 * 1024 * 1024,    // 4GB
    JetstreamMaxStore = 100L * 1024 * 1024 * 1024,   // 100GB

    // Monitoring
    HTTPPort = 8222,
    HTTPHost = "127.0.0.1",  // Secure: localhost only

    // Security
    Auth = new AuthConfig
    {
        Username = Environment.GetEnvironmentVariable("NATS_USER"),
        Password = Environment.GetEnvironmentVariable("NATS_PASS")
    },

    // Logging
    Debug = false,
    Trace = false
};

controller.Start(config);
```

---

## Common Issues

| Problem | Solution |
|---------|----------|
| Port in use | Change `Port` to different value |
| DLL not found | Build Go bindings or check output dir |
| Startup timeout | Check logs, verify permissions |
| High memory | Reduce `JetstreamMaxMemory` |
| Update rejected | Check return string for error |
| Can't connect | Use `Host = "0.0.0.0"` not `localhost` |

---

## Performance Tips

1. **Disable debug logging in production**
   ```csharp
   config.Debug = false;
   config.Trace = false;
   ```

2. **Limit JetStream memory**
   ```csharp
   config.JetstreamMaxMemory = 2L * 1024 * 1024 * 1024;  // 2GB
   ```

3. **Tune message size limits**
   ```csharp
   config.MaxPayload = 4 * 1024 * 1024;  // 4MB if large messages needed
   ```

4. **Use local connections when possible**
   ```csharp
   config.Host = "127.0.0.1";  // Localhost only
   ```

---

## Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Environment variables
ENV NATS_HOST=0.0.0.0
ENV NATS_PORT=4222
ENV NATS_JETSTREAM=true
ENV NATS_STORE_DIR=/data/jetstream

# Expose ports
EXPOSE 4222 8222

# Volume for JetStream data
VOLUME ["/data/jetstream"]

ENTRYPOINT ["dotnet", "MyApp.dll"]
```

---

## Kubernetes ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: nats-config
data:
  NATS_HOST: "0.0.0.0"
  NATS_PORT: "4222"
  NATS_JETSTREAM: "true"
  NATS_STORE_DIR: "/data/jetstream"
  NATS_HTTP_PORT: "8222"
---
apiVersion: v1
kind: Secret
metadata:
  name: nats-credentials
type: Opaque
stringData:
  NATS_USER: admin
  NATS_PASS: SecurePassword123!
```

---

## Health Check Implementation

```csharp
public class HealthCheck : IHealthCheck
{
    private readonly NatsController _controller;

    public HealthCheck(NatsController controller)
    {
        _server = server;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var info = _controller.GetInfo();

            if (string.IsNullOrEmpty(info.ClientUrl))
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy("NATS server not responding")
                );
            }

            return Task.FromResult(
                HealthCheckResult.Healthy($"NATS OK - {info.Connections} connections")
            );
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy($"NATS error: {ex.Message}")
            );
        }
    }
}
```

---

## Graceful Shutdown Pattern

```csharp
class Program
{
    private static NatsController _controller;
    private static readonly ManualResetEvent _shutdownEvent = new(false);

    static void Main()
    {
        _server = new NatsController();
        _controller.Start();

        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        Console.WriteLine("Server running. Press Ctrl+C to stop.");
        _shutdownEvent.WaitOne();
    }

    static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        Shutdown();
    }

    static void OnProcessExit(object sender, EventArgs e)
    {
        Shutdown();
    }

    static void Shutdown()
    {
        Console.WriteLine("Shutting down gracefully...");
        _controller?.Shutdown();
        _shutdownEvent.Set();
    }
}
```

---

## Metrics Collection Example

```csharp
public class NatsMetrics
{
    private readonly NatsController _controller;
    private readonly IMetricsCollector _metrics;
    private Timer _timer;

    public NatsMetrics(NatsController controller, IMetricsCollector metrics)
    {
        _server = server;
        _metrics = metrics;
    }

    public void Start()
    {
        _timer = new Timer(_ =>
        {
            var info = _controller.GetInfo();

            _metrics.Gauge("nats.connections", info.Connections);
            _metrics.Gauge("nats.jetstream_enabled", info.JetstreamEnabled ? 1 : 0);

        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
    }

    public void Stop()
    {
        _timer?.Dispose();
    }
}
```

---

## Further Reading

- **Full Documentation**: See [README.md](./README.md)
- **Getting Started**: See [GETTING_STARTED.md](./GETTING_STARTED.md)
- **API Reference**: See [API_DESIGN.md](./API_DESIGN.md)
- **Architecture**: See [ARCHITECTURE.md](./ARCHITECTURE.md)
- **NATS Documentation**: [docs.nats.io](https://docs.nats.io)
