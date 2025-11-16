# NATS Server Monitoring Guide

⚠️ **Developer Reference**: This is a technical reference document for platform engineers and DevOps teams.

This guide covers the comprehensive monitoring capabilities available in DotGnatly for observing and managing NATS server instances.

## Table of Contents

- [Overview](#overview)
- [Monitoring Endpoints](#monitoring-endpoints)
- [Connection Management](#connection-management)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)
- [Performance Considerations](#performance-considerations)

## Overview

DotGnatly provides direct access to NATS server monitoring endpoints, enabling:

- **Real-time Observability**: Monitor connections, subscriptions, and JetStream statistics
- **Cluster Health**: Track cluster routes and leaf node connections
- **Client Management**: Inspect and control individual client connections
- **Operational Insights**: Collect metrics for alerting, capacity planning, and troubleshooting

All monitoring methods are async, thread-safe, and return JSON data for flexible parsing and integration with monitoring systems.

## Monitoring Endpoints

### 1. Connz - Connection Monitoring

**Purpose**: Monitor all client connections to the server.

**Method**: `GetConnzAsync(string? subscriptionsFilter = null)`

**Returns**: JSON containing:
- Total and active connection counts
- Connection details (CID, IP, port)
- Bytes/messages in/out per connection
- Subscription counts
- Client library information
- Connection uptime and idle time

**Example**:
```csharp
using var controller = new NatsController();
await controller.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

// Get all connections
string connz = await controller.GetConnzAsync();

// Get connections with subscription details
string connzWithSubs = await controller.GetConnzAsync("include-subscriptions");

// Parse the JSON
using var doc = JsonDocument.Parse(connz);
var numConnections = doc.RootElement.GetProperty("num_connections").GetInt32();
Console.WriteLine($"Active connections: {numConnections}");
```

**Use Cases**:
- Monitor connection count trends
- Identify high-traffic clients
- Track connection lifecycle
- Alert on connection spikes/drops

---

### 2. Subsz - Subscription Monitoring

**Purpose**: Monitor all subscriptions across the server.

**Method**: `GetSubszAsync(string? subscriptionsFilter = null)`

**Returns**: JSON containing:
- Total subscription count
- Subscription details by subject
- Queue groups
- Insert/remove statistics
- Match counts

**Example**:
```csharp
// Get all subscriptions
string subsz = await controller.GetSubszAsync();

// Filter by subject pattern
string filteredSubs = await controller.GetSubszAsync("orders.*");

using var doc = JsonDocument.Parse(subsz);
var totalSubs = doc.RootElement.GetProperty("total").GetInt32();
Console.WriteLine($"Total subscriptions: {totalSubs}");
```

**Use Cases**:
- Debug subscription issues
- Monitor subject usage patterns
- Identify subscription leaks
- Validate queue group setup

---

### 3. Jsz - JetStream Monitoring

**Purpose**: Monitor JetStream configuration and statistics.

**Method**: `GetJszAsync(string? accountName = null)`

**Returns**: JSON containing:
- JetStream configuration (max memory/storage)
- Current memory and storage usage
- Stream and consumer counts
- Message and byte counts
- Account-specific JetStream data

**Example**:
```csharp
var config = new BrokerConfiguration
{
    Port = 4222,
    Jetstream = true,
    JetstreamStoreDir = "/tmp/jetstream"
};

await controller.ConfigureAsync(config);

// Get JetStream stats for all accounts
string jsz = await controller.GetJszAsync();

// Get stats for specific account
string accountJsz = await controller.GetJszAsync("$G");

using var doc = JsonDocument.Parse(jsz);
var streams = doc.RootElement.GetProperty("streams").GetInt32();
var memory = doc.RootElement.GetProperty("memory").GetInt64();
Console.WriteLine($"Streams: {streams}, Memory: {memory:N0} bytes");
```

**Use Cases**:
- Monitor JetStream resource usage
- Track stream/consumer growth
- Capacity planning for JetStream
- Alert on storage thresholds

---

### 4. Routez - Cluster Route Monitoring

**Purpose**: Monitor cluster routing connections between servers.

**Method**: `GetRoutezAsync()`

**Returns**: JSON containing:
- Number of cluster routes
- Route details (remote server ID, IP, port)
- Bytes/messages in/out per route
- Subscription counts per route
- Pending bytes

**Example**:
```csharp
var config = new BrokerConfiguration
{
    Port = 4222,
    Cluster = new ClusterConfiguration
    {
        Name = "my-cluster",
        Host = "127.0.0.1",
        Port = 6222,
        Routes = new[] { "nats://node2:6222", "nats://node3:6222" }
    }
};

await controller.ConfigureAsync(config);

string routez = await controller.GetRoutezAsync();

using var doc = JsonDocument.Parse(routez);
var numRoutes = doc.RootElement.GetProperty("num_routes").GetInt32();
Console.WriteLine($"Active routes: {numRoutes}");
```

**Use Cases**:
- Monitor cluster connectivity
- Debug cluster formation issues
- Track inter-server communication
- Identify route failures

---

### 5. Leafz - Leaf Node Monitoring

**Purpose**: Monitor leaf node connections (edge servers to hub).

**Method**: `GetLeafzAsync()`

**Returns**: JSON containing:
- Number of leaf node connections
- Leaf node details (account, IP, port)
- Bytes/messages in/out
- Subscription counts

**Example**:
```csharp
var config = new BrokerConfiguration
{
    Port = 4222,
    LeafNode = new LeafNodeConfiguration
    {
        Host = "127.0.0.1",
        Port = 7422,
        RemoteURLs = new[] { "nats://hub.example.com:7422" }
    }
};

await controller.ConfigureAsync(config);

string leafz = await controller.GetLeafzAsync();

using var doc = JsonDocument.Parse(leafz);
var numLeafs = doc.RootElement.GetProperty("num_leafs").GetInt32();
Console.WriteLine($"Leaf nodes: {numLeafs}");
```

**Use Cases**:
- Monitor edge server connectivity
- Track leaf node health
- Debug super-cluster issues
- Monitor geographic distribution

---

## Connection Management

### DisconnectClientAsync

**Purpose**: Force disconnect a specific client by connection ID.

**Method**: `DisconnectClientAsync(ulong clientId)`

**Example**:
```csharp
// First, get the connection ID from Connz
string connz = await controller.GetConnzAsync();
using var doc = JsonDocument.Parse(connz);
var connections = doc.RootElement.GetProperty("connections");

// Find a problematic client (e.g., high pending bytes)
foreach (var conn in connections.EnumerateArray())
{
    var cid = (ulong)conn.GetProperty("cid").GetInt64();
    var pending = conn.GetProperty("pending_bytes").GetInt64();

    if (pending > 10_000_000) // More than 10MB pending
    {
        Console.WriteLine($"Disconnecting slow consumer {cid}");
        await controller.DisconnectClientAsync(cid);
    }
}
```

**Use Cases**:
- Remove misbehaving clients
- Enforce security policies
- Free up resources from idle connections
- Handle slow consumers

---

### GetClientInfoAsync

**Purpose**: Get detailed information about a specific client.

**Method**: `GetClientInfoAsync(ulong clientId)`

**Example**:
```csharp
ulong clientId = 42; // From Connz

string clientInfo = await controller.GetClientInfoAsync(clientId);

using var doc = JsonDocument.Parse(clientInfo);
var ip = doc.RootElement.GetProperty("ip").GetString();
var subs = doc.RootElement.GetProperty("subscriptions").GetInt32();
var inMsgs = doc.RootElement.GetProperty("in_msgs").GetInt64();

Console.WriteLine($"Client {clientId}: {ip}, {subs} subs, {inMsgs:N0} msgs in");
```

**Use Cases**:
- Debug specific client issues
- Audit client activity
- Detailed connection analysis
- Security investigations

---

## Usage Examples

### Example 1: Monitor Connection Health

```csharp
public async Task MonitorConnectionHealthAsync(NatsController controller)
{
    while (true)
    {
        var connz = await controller.GetConnzAsync();
        using var doc = JsonDocument.Parse(connz);

        var numConns = doc.RootElement.GetProperty("num_connections").GetInt32();
        var inMsgs = doc.RootElement.GetProperty("in_msgs").GetInt64();
        var outMsgs = doc.RootElement.GetProperty("out_msgs").GetInt64();

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connections: {numConns}, " +
                         $"In: {inMsgs:N0}, Out: {outMsgs:N0}");

        // Alert if no connections
        if (numConns == 0)
        {
            await SendAlertAsync("No active connections!");
        }

        await Task.Delay(TimeSpan.FromSeconds(10));
    }
}
```

### Example 2: JetStream Storage Monitoring

```csharp
public async Task MonitorJetStreamStorageAsync(NatsController controller)
{
    var jsz = await controller.GetJszAsync();
    using var doc = JsonDocument.Parse(jsz);
    var root = doc.RootElement;

    var config = root.GetProperty("config");
    var maxStorage = config.GetProperty("max_storage").GetInt64();
    var currentStorage = root.GetProperty("storage").GetInt64();

    var percentUsed = (currentStorage / (double)maxStorage) * 100;

    Console.WriteLine($"JetStream Storage: {percentUsed:F1}% used");

    if (percentUsed > 80)
    {
        await SendAlertAsync($"JetStream storage at {percentUsed:F1}%!");
    }
}
```

### Example 3: Identify and Disconnect Slow Consumers

```csharp
public async Task DisconnectSlowConsumersAsync(NatsController controller)
{
    const long SLOW_CONSUMER_THRESHOLD = 5_000_000; // 5MB pending

    var connz = await controller.GetConnzAsync("include-subs");
    using var doc = JsonDocument.Parse(connz);
    var connections = doc.RootElement.GetProperty("connections");

    foreach (var conn in connections.EnumerateArray())
    {
        var cid = (ulong)conn.GetProperty("cid").GetInt64();
        var pending = conn.GetProperty("pending_bytes").GetInt64();
        var name = conn.GetProperty("name").GetString() ?? "unknown";

        if (pending > SLOW_CONSUMER_THRESHOLD)
        {
            Console.WriteLine($"Slow consumer detected: {name} (CID {cid}), " +
                            $"Pending: {pending:N0} bytes");

            await controller.DisconnectClientAsync(cid);
            Console.WriteLine($"Disconnected slow consumer {cid}");

            await LogEventAsync($"Disconnected slow consumer {name} ({cid})");
        }
    }
}
```

### Example 4: Cluster Health Dashboard

```csharp
public async Task DisplayClusterHealthAsync(NatsController controller)
{
    Console.Clear();
    Console.WriteLine("=== Cluster Health Dashboard ===\n");

    // Server Info
    var info = await controller.GetInfoAsync();
    Console.WriteLine($"Server: {info.ServerId}");
    Console.WriteLine($"Version: {info.Version}");
    Console.WriteLine();

    // Connections
    var connz = await controller.GetConnzAsync();
    using var connDoc = JsonDocument.Parse(connz);
    var numConns = connDoc.RootElement.GetProperty("num_connections").GetInt32();
    Console.WriteLine($"Connections: {numConns}");

    // Routes
    var routez = await controller.GetRoutezAsync();
    using var routeDoc = JsonDocument.Parse(routez);
    var numRoutes = routeDoc.RootElement.GetProperty("num_routes").GetInt32();
    Console.WriteLine($"Cluster Routes: {numRoutes}");

    // JetStream (if enabled)
    try
    {
        var jsz = await controller.GetJszAsync();
        using var jsDoc = JsonDocument.Parse(jsz);
        var streams = jsDoc.RootElement.GetProperty("streams").GetInt32();
        var consumers = jsDoc.RootElement.GetProperty("consumers").GetInt32();
        Console.WriteLine($"JetStream Streams: {streams}");
        Console.WriteLine($"JetStream Consumers: {consumers}");
    }
    catch
    {
        Console.WriteLine("JetStream: Disabled");
    }
}
```

---

## Best Practices

### 1. Polling Frequency

- **Production Monitoring**: Poll every 10-60 seconds
- **Development/Debug**: Poll as needed (1-5 seconds)
- **Dashboards**: 5-15 second refresh
- **Alerting**: 30-60 seconds to avoid false positives

```csharp
// Good: Reasonable polling interval
await Task.Delay(TimeSpan.FromSeconds(30));

// Bad: Excessive polling
await Task.Delay(TimeSpan.FromMilliseconds(100)); // Too frequent!
```

### 2. Error Handling

Always wrap monitoring calls in try-catch blocks:

```csharp
try
{
    var connz = await controller.GetConnzAsync();
    // Process data
}
catch (InvalidOperationException ex)
{
    // Server not running
    Console.WriteLine($"Server not available: {ex.Message}");
}
catch (Exception ex)
{
    // Other errors
    Console.WriteLine($"Monitoring error: {ex.Message}");
}
```

### 3. Resource Management

Parse JSON efficiently and dispose of JsonDocument:

```csharp
// Good: Using statement ensures disposal
string connz = await controller.GetConnzAsync();
using var doc = JsonDocument.Parse(connz);
var numConns = doc.RootElement.GetProperty("num_connections").GetInt32();

// Bad: Potential memory leak
var doc = JsonDocument.Parse(connz);
var numConns = doc.RootElement.GetProperty("num_connections").GetInt32();
// Missing disposal!
```

### 4. Caching

Cache monitoring data when appropriate:

```csharp
private DateTime _lastFetch;
private string? _cachedConnz;
private const int CACHE_SECONDS = 5;

public async Task<string> GetCachedConnzAsync()
{
    if (_cachedConnz == null ||
        (DateTime.Now - _lastFetch).TotalSeconds > CACHE_SECONDS)
    {
        _cachedConnz = await controller.GetConnzAsync();
        _lastFetch = DateTime.Now;
    }

    return _cachedConnz;
}
```

### 5. Metrics Export

Export metrics to monitoring systems:

```csharp
public async Task ExportMetricsAsync(NatsController controller)
{
    var connz = await controller.GetConnzAsync();
    using var doc = JsonDocument.Parse(connz);
    var root = doc.RootElement;

    // Export to Prometheus, StatsD, CloudWatch, etc.
    metricsClient.Gauge("nats.connections",
        root.GetProperty("num_connections").GetInt32());
    metricsClient.Counter("nats.messages.in",
        root.GetProperty("in_msgs").GetInt64());
    metricsClient.Counter("nats.messages.out",
        root.GetProperty("out_msgs").GetInt64());
}
```

---

## Performance Considerations

### 1. Thread Safety

All monitoring methods use semaphore locking and are thread-safe. However, excessive concurrent calls can cause queueing:

```csharp
// Good: Sequential monitoring
await controller.GetConnzAsync();
await controller.GetSubszAsync();
await controller.GetJszAsync();

// Acceptable: Parallel if needed (but adds no benefit)
var tasks = new[]
{
    controller.GetConnzAsync(),
    controller.GetSubszAsync(),
    controller.GetJszAsync()
};
await Task.WhenAll(tasks);
```

### 2. JSON Parsing Overhead

JSON serialization/deserialization has overhead. Minimize calls:

```csharp
// Good: One call, extract multiple values
var connz = await controller.GetConnzAsync();
using var doc = JsonDocument.Parse(connz);
var numConns = doc.RootElement.GetProperty("num_connections").GetInt32();
var inBytes = doc.RootElement.GetProperty("in_bytes").GetInt64();
var outBytes = doc.RootElement.GetProperty("out_bytes").GetInt64();

// Bad: Multiple calls for same data
var connz1 = await controller.GetConnzAsync();
var numConns = JsonDocument.Parse(connz1).RootElement
    .GetProperty("num_connections").GetInt32();

var connz2 = await controller.GetConnzAsync(); // Wasteful!
var inBytes = JsonDocument.Parse(connz2).RootElement
    .GetProperty("in_bytes").GetInt64();
```

### 3. Filter Usage

Use filters to reduce data transfer:

```csharp
// Good: Filter for specific subscriptions
var subsz = await controller.GetSubszAsync("orders.*");

// Less efficient: Get all subscriptions, filter client-side
var allSubs = await controller.GetSubszAsync();
// ... filter in C# ...
```

### 4. Memory Considerations

Large connection counts can produce large JSON responses. Consider pagination if available or limit polling frequency.

---

## Integration with Monitoring Systems

### Prometheus Example

```csharp
public class NatsExporter
{
    private readonly NatsController _controller;

    [PrometheusMetric]
    public async Task<GaugeMetric[]> GetMetricsAsync()
    {
        var connz = await _controller.GetConnzAsync();
        using var doc = JsonDocument.Parse(connz);
        var root = doc.RootElement;

        return new[]
        {
            new GaugeMetric("nats_connections_total",
                root.GetProperty("num_connections").GetInt32()),
            new GaugeMetric("nats_bytes_in_total",
                root.GetProperty("in_bytes").GetInt64()),
            new GaugeMetric("nats_bytes_out_total",
                root.GetProperty("out_bytes").GetInt64())
        };
    }
}
```

### Application Insights Example

```csharp
public async Task TrackMetricsAsync(NatsController controller,
    TelemetryClient telemetry)
{
    var connz = await controller.GetConnzAsync();
    using var doc = JsonDocument.Parse(connz);
    var root = doc.RootElement;

    telemetry.TrackMetric("NATS.Connections",
        root.GetProperty("num_connections").GetInt32());
    telemetry.TrackMetric("NATS.MessagesIn",
        root.GetProperty("in_msgs").GetInt64());
    telemetry.TrackMetric("NATS.MessagesOut",
        root.GetProperty("out_msgs").GetInt64());
}
```

---

## See Also

- [API Design Documentation](API_DESIGN.md)
- [Architecture Overview](ARCHITECTURE.md)
- [Monitoring Examples](../src/DotGnatly.Examples/Monitoring/)
- [NATS Monitoring Documentation](https://docs.nats.io/running-a-nats-service/nats_admin/monitoring)

---

**Last Updated**: 2025-11-15
**Version**: 1.0.0
