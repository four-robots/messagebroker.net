# New Features: Logging Control and JetStream Clustering

This document describes the new logging control and JetStream clustering features added to DotGnatly.

## Overview

The following features have been implemented:

### Logging Control
1. **Log File Configuration** - Configure log file path, UTC time, and size-based rotation
2. **ReOpenLogFile()** - Reopen log file for external log rotation support
3. **GetOpts()** - Retrieve current server options/configuration

### Advanced Configuration
4. **JetStream Clustering** - Configure JetStream domain and unique tags for clustering scenarios

## Feature Details

### 1. Log File Configuration

Configure logging settings via `BrokerConfiguration`:

```csharp
using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;

var config = new BrokerConfiguration
{
    Port = 4222,
    Debug = true,
    Trace = false,

    // Logging configuration
    LogFile = "/var/log/nats/server.log",
    LogTimeUtc = true,
    LogFileSize = 10485760 // 10 MB - automatic rotation after this size
};

using var controller = new NatsController();
var result = await controller.ConfigureAsync(config);

if (result.Success)
{
    Console.WriteLine("Server started with logging configured");
}
```

**Properties:**

- **LogFile** (`string?`): Path to the log file. If null, logs go to stdout.
- **LogTimeUtc** (`bool`): If true, log timestamps are in UTC; otherwise, local time. Default: `true`
- **LogFileSize** (`long`): Maximum log file size in bytes before automatic rotation. `0` means no size-based rotation. Default: `0`

### 2. ReOpenLogFile() - Log Rotation Support

Manually trigger log file reopening for external log rotation (e.g., logrotate):

```csharp
using DotGnatly.Nats.Implementation;

using var controller = new NatsController();
await controller.ConfigureAsync(new BrokerConfiguration
{
    Port = 4222,
    LogFile = "/var/log/nats/server.log"
});

// External process renames the log file (e.g., server.log -> server.log.1)
// Then signal DotGnatly to reopen the log file
await controller.ReOpenLogFileAsync();

// Server now writes to a new /var/log/nats/server.log
```

**Use Case: Integration with logrotate**

```bash
# /etc/logrotate.d/nats-server
/var/log/nats/server.log {
    daily
    rotate 7
    compress
    delaycompress
    postrotate
        # Signal your application to reopen the log file
        systemctl reload your-nats-app
    endscript
}
```

In your application:

```csharp
// In your SIGHUP handler or reload endpoint
await natsController.ReOpenLogFileAsync();
```

### 3. GetOpts() - Retrieve Server Options

Get the current running server configuration as JSON:

```csharp
using System.Text.Json;
using DotGnatly.Nats.Implementation;

using var controller = new NatsController();
await controller.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

// Get current server options
string optsJson = await controller.GetOptsAsync();

// Parse and inspect
var opts = JsonSerializer.Deserialize<Dictionary<string, object>>(optsJson);
Console.WriteLine($"Server Port: {opts["port"]}");
Console.WriteLine($"Max Payload: {opts["max_payload"]}");
Console.WriteLine($"JetStream Enabled: {opts["jetstream"]}");
Console.WriteLine($"Debug Mode: {opts["debug"]}");
```

**Returned JSON Structure:**

```json
{
  "host": "localhost",
  "port": 4222,
  "max_payload": 1048576,
  "max_control_line": 4096,
  "max_pings_out": 2,
  "debug": false,
  "trace": false,
  "logtime": true,
  "log_file": "/var/log/nats/server.log",
  "log_size_limit": 10485760,
  "jetstream": true,
  "jetstream_max_memory": -1,
  "jetstream_max_store": -1,
  "jetstream_domain": "production",
  "jetstream_unique_tag": "server-01",
  "store_dir": "./jetstream",
  "http_host": "0.0.0.0",
  "http_port": 8222,
  "https_port": 0,
  "cluster_name": "my-cluster",
  "cluster_port": 6222,
  "leaf_node_port": 7422
}
```

**Use Cases:**
- Debugging configuration issues
- Verifying runtime configuration
- Monitoring and observability
- Configuration auditing

### 4. JetStream Clustering Configuration

Configure JetStream for high-availability clustering scenarios:

```csharp
using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;

// Server 1 configuration
var config1 = new BrokerConfiguration
{
    Port = 4222,
    Jetstream = true,
    JetstreamStoreDir = "./jetstream-node1",

    // JetStream clustering
    JetstreamDomain = "production",
    JetstreamUniqueTag = "nats-server-01",

    // Cluster configuration for server communication
    Cluster = new ClusterConfiguration
    {
        Name = "production-cluster",
        Host = "0.0.0.0",
        Port = 6222,
        Routes = new List<string>
        {
            "nats-route://server2:6222",
            "nats-route://server3:6222"
        }
    }
};

using var controller1 = new NatsController();
await controller1.ConfigureAsync(config1);

// Server 2 configuration (similar but with different unique tag and routes)
var config2 = new BrokerConfiguration
{
    Port = 4222,
    Jetstream = true,
    JetstreamStoreDir = "./jetstream-node2",

    JetstreamDomain = "production",
    JetstreamUniqueTag = "nats-server-02",  // Unique for this server

    Cluster = new ClusterConfiguration
    {
        Name = "production-cluster",
        Host = "0.0.0.0",
        Port = 6222,
        Routes = new List<string>
        {
            "nats-route://server1:6222",
            "nats-route://server3:6222"
        }
    }
};

using var controller2 = new NatsController();
await controller2.ConfigureAsync(config2);
```

**Properties:**

- **JetstreamDomain** (`string?`): JetStream domain name for creating isolated JetStream clusters within a larger NATS cluster. Allows multiple JetStream "domains" to coexist.
- **JetstreamUniqueTag** (`string?`): Unique identifier for this JetStream server in a cluster. If not set, the server generates one automatically.

**Use Cases:**
- **Multi-tenant JetStream**: Create isolated JetStream domains for different tenants
- **High Availability**: Run multiple JetStream servers in a cluster for fault tolerance
- **Disaster Recovery**: Configure JetStream clustering across multiple data centers
- **Load Distribution**: Distribute JetStream workload across multiple servers

**Domain Isolation Example:**

```csharp
// Production domain cluster
var prodConfig = new BrokerConfiguration
{
    Port = 4222,
    Jetstream = true,
    JetstreamDomain = "production",
    JetstreamUniqueTag = "prod-server-01"
};

// Staging domain cluster (isolated from production)
var stagingConfig = new BrokerConfiguration
{
    Port = 4223,  // Different port
    Jetstream = true,
    JetstreamDomain = "staging",  // Different domain
    JetstreamUniqueTag = "staging-server-01"
};
```

## Complete Example: Production Setup with Logging and Clustering

```csharp
using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using System.Text.Json;

public class ProductionNatsServer
{
    public static async Task Main()
    {
        var config = new BrokerConfiguration
        {
            // Basic server settings
            Host = "0.0.0.0",
            Port = 4222,
            Debug = false,
            Trace = false,

            // Logging configuration
            LogFile = "/var/log/nats/production-server.log",
            LogTimeUtc = true,
            LogFileSize = 104857600, // 100 MB

            // JetStream with clustering
            Jetstream = true,
            JetstreamStoreDir = "/data/jetstream",
            JetstreamMaxMemory = 10737418240,  // 10 GB
            JetstreamMaxStore = 53687091200,   // 50 GB
            JetstreamDomain = "production",
            JetstreamUniqueTag = "prod-nats-01",

            // Cluster configuration
            Cluster = new ClusterConfiguration
            {
                Name = "production-cluster",
                Host = "0.0.0.0",
                Port = 6222,
                Routes = new List<string>
                {
                    "nats-route://prod-nats-02:6222",
                    "nats-route://prod-nats-03:6222"
                }
            },

            // HTTP monitoring
            HttpPort = 8222,
            HttpHost = "0.0.0.0"
        };

        using var controller = new NatsController();

        // Start the server
        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"Failed to start server: {result.Message}");
            return;
        }

        Console.WriteLine("NATS server started successfully");

        // Get and log current configuration
        var opts = await controller.GetOptsAsync();
        var optsObj = JsonSerializer.Deserialize<Dictionary<string, object>>(opts);
        Console.WriteLine($"Server running on port {optsObj["port"]}");
        Console.WriteLine($"JetStream Domain: {optsObj["jetstream_domain"]}");
        Console.WriteLine($"Unique Tag: {optsObj["jetstream_unique_tag"]}");

        // Setup log rotation handler (e.g., on SIGHUP)
        Console.CancelKeyPress += async (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("Reopening log file...");
            await controller.ReOpenLogFileAsync();
            Console.WriteLine("Log file reopened");
        };

        Console.WriteLine("Press Ctrl+C to trigger log rotation, Ctrl+Break to exit");

        // Keep running
        await Task.Delay(Timeout.Infinite);
    }
}
```

## Migration Guide

### Updating Existing Code

If you have existing DotGnatly code, the new features are opt-in and fully backward compatible:

```csharp
// Old code - still works exactly as before
var config = new BrokerConfiguration
{
    Port = 4222,
    Jetstream = true
};

// New code - add logging and clustering
var configNew = new BrokerConfiguration
{
    Port = 4222,
    Jetstream = true,

    // New properties (optional)
    LogFile = "/var/log/nats/server.log",
    JetstreamDomain = "production",
    JetstreamUniqueTag = "server-01"
};
```

### Gradual Adoption

You can adopt these features incrementally:

1. **Start with logging**: Add `LogFile` to your configuration
2. **Add log rotation**: Implement `ReOpenLogFileAsync()` for production
3. **Enable clustering**: Configure `JetstreamDomain` and cluster settings
4. **Add monitoring**: Use `GetOptsAsync()` for runtime inspection

## Testing

The new features can be tested without a full cluster setup:

```csharp
[Fact]
public async Task TestLoggingConfiguration()
{
    var config = new BrokerConfiguration
    {
        Port = 4222,
        LogFile = Path.GetTempFileName()
    };

    using var controller = new NatsController();
    var result = await controller.ConfigureAsync(config);

    Assert.True(result.Success);

    // Verify log file exists
    Assert.True(File.Exists(config.LogFile));

    // Test log rotation
    await controller.ReOpenLogFileAsync();

    // File should still exist and be writable
    Assert.True(File.Exists(config.LogFile));
}

[Fact]
public async Task TestGetOpts()
{
    var config = new BrokerConfiguration
    {
        Port = 4222,
        JetstreamDomain = "test-domain"
    };

    using var controller = new NatsController();
    await controller.ConfigureAsync(config);

    var optsJson = await controller.GetOptsAsync();
    var opts = JsonSerializer.Deserialize<Dictionary<string, object>>(optsJson);

    Assert.Equal(4222, opts["port"]);
    Assert.Equal("test-domain", opts["jetstream_domain"]);
}
```

## Notes and Limitations

1. **Log Rotation**: `ReOpenLogFileAsync()` should be called AFTER the log file is moved/renamed by the external rotation tool
2. **JetStream Domain**: All servers in a JetStream cluster should use the same `JetstreamDomain` value
3. **Unique Tags**: Each server in a cluster must have a unique `JetstreamUniqueTag` value
4. **GetOpts()**: Returns a simplified view of server options; some internal/unexported fields are not included

## References

- [NATS Server Documentation](https://docs.nats.io/running-a-nats-service/configuration)
- [JetStream Clustering](https://docs.nats.io/running-a-nats-service/configuration/clustering/jetstream_clustering)
- [Log Rotation Best Practices](https://docs.nats.io/running-a-nats-service/configuration#logging)

## Summary

These new features provide:
- **Production-ready logging** with rotation support
- **Configuration introspection** for debugging and monitoring
- **JetStream clustering** for high availability and scalability

All features are backward compatible and optional, allowing gradual adoption in existing applications.
