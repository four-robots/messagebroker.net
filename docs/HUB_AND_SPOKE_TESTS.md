# Hub-and-Spoke Network Tests

## Overview

This document describes the comprehensive integration tests for DotGnatly's hub-and-spoke network topology. These tests verify that NATS servers can communicate through a leaf node architecture, where one or more "spoke" servers connect to a central "hub" server.

## Test Suite: HubAndSpokeTests

Location: `/src/DotGnatly.IntegrationTests/HubAndSpokeTests.cs`

### Dependencies

- **NATS.Net v2.5.2**: Official .NET client library for NATS messaging
- **DotGnatly.Core**: Configuration models and abstractions
- **DotGnatly.Nats**: NATS server controller implementation

## Test Coverage

### 1. Hub-to-Leaf Message Flow
**Test Name**: "Hub-to-leaf message flow: Hub publishes, leaf receives"

**Purpose**: Verifies that messages published on the hub server are correctly forwarded to connected leaf nodes based on export/import subject configuration.

**Setup**:
- Hub server on port 4222 with leaf node port 7422
- Hub exports subject `hub.>`
- Leaf server on port 4223 connecting to hub
- Leaf imports subject `hub.>`

**Verification**:
- Messages published to `hub.test` on hub are received by subscribers on leaf
- All messages are delivered in order
- No message loss occurs

### 2. Leaf-to-Hub Message Flow
**Test Name**: "Leaf-to-hub message flow: Leaf publishes, hub receives"

**Purpose**: Verifies that messages published on a leaf node are correctly forwarded to the hub server.

**Setup**:
- Hub imports subject `leaf.>`
- Leaf exports subject `leaf.>`

**Verification**:
- Messages published on leaf are received by hub subscribers
- Message delivery is reliable and ordered

### 3. Bidirectional Message Flow
**Test Name**: "Bidirectional message flow: Hub and leaf can both send and receive"

**Purpose**: Verifies simultaneous two-way communication between hub and leaf nodes.

**Setup**:
- Hub imports `leaf.>` and exports `hub.>`
- Leaf imports `hub.>` and exports `leaf.>`

**Verification**:
- Hub can publish messages that leaf receives
- Leaf can publish messages that hub receives
- Both directions work simultaneously without interference

### 4. Multiple Leaf Nodes
**Test Name**: "Multiple leaf nodes: One hub, two leaves communicating through hub"

**Purpose**: Verifies that multiple leaf nodes can communicate with each other through the hub server.

**Setup**:
- One hub server
- Two leaf nodes (ports 4223 and 4224)
- All use wildcard subject `>` (all messages)

**Verification**:
- Messages published on leaf1 are received by leaf2 (routed through hub)
- Hub acts as message broker between leaf nodes

### 5. Dynamic Subject Addition
**Test Name**: "Dynamic subject changes: Add new export subject on hub, verify leaf receives"

**Purpose**: Verifies that subjects can be added dynamically without server restart.

**Setup**:
- Hub initially exports only `hub.old.>`
- Dynamically add `hub.new.>` export using hot reload

**Verification**:
- Messages on new subject are NOT received before addition
- Messages ARE received after dynamic addition
- No server restart required

**API Used**: `AddLeafNodeExportSubjectsAsync()`

### 6. Dynamic Subject Removal
**Test Name**: "Dynamic subject changes: Remove export subject, verify messages stop flowing"

**Purpose**: Verifies that removing subjects stops message flow without restart.

**Setup**:
- Hub initially exports `hub.test.>`
- Dynamically remove the export subject

**Verification**:
- Messages flow before removal
- Messages do NOT flow after removal
- Configuration change applies immediately

**API Used**: `RemoveLeafNodeExportSubjectsAsync()`

### 7. Dynamic Subject Replacement
**Test Name**: "Dynamic subject changes: Replace import subjects on leaf node"

**Purpose**: Verifies complete replacement of subject filters on leaf nodes.

**Setup**:
- Leaf initially imports `old.>`
- Replace with `new.>` using hot reload

**Verification**:
- Old subject works before replacement
- New subject works after replacement
- Old subject stops working after replacement

**API Used**: `SetLeafNodeImportSubjectsAsync()`

### 8. Wildcard Subjects
**Test Name**: "Wildcard subjects: Single-token (*) and multi-token (>) wildcards"

**Purpose**: Verifies that NATS wildcard patterns work correctly in hub-and-spoke topology.

**Setup**:
- Hub exports `events.*.created` (single-token wildcard)
- Hub exports `data.>` (multi-token wildcard)

**Verification**:
- `events.user.created` matches first pattern
- `events.order.created` matches first pattern
- `data.metrics.cpu` matches second pattern
- `data.metrics.memory.usage` matches second pattern

## Running the Tests

### Prerequisites

1. .NET 9.0 SDK installed
2. Native NATS bindings built (`native/build.sh` or `native/build.ps1`)

### Execution

```bash
# Navigate to integration tests directory
cd src/DotGnatly.IntegrationTests

# Restore packages
dotnet restore

# Build
dotnet build

# Run all integration tests (includes HubAndSpokeTests)
dotnet run
```

### Expected Output

```
========================================
MessageBroker.NET Integration Tests
========================================

[3/11] Running HubAndSpokeTests...
------------------------------------------------------------
  → Starting: Hub-to-leaf message flow: Hub publishes, leaf receives
  ✓ Hub-to-leaf message flow: Hub publishes, leaf receives
  → Starting: Leaf-to-hub message flow: Leaf publishes, hub receives
  ✓ Leaf-to-hub message flow: Leaf publishes, hub receives
  → Starting: Bidirectional message flow: Hub and leaf can both send and receive
  ✓ Bidirectional message flow: Hub and leaf can both send and receive
  → Starting: Multiple leaf nodes: One hub, two leaves communicating through hub
  ✓ Multiple leaf nodes: One hub, two leaves communicating through hub
  → Starting: Dynamic subject changes: Add new export subject on hub, verify leaf receives
  ✓ Dynamic subject changes: Add new export subject on hub, verify leaf receives
  → Starting: Dynamic subject changes: Remove export subject, verify messages stop flowing
  ✓ Dynamic subject changes: Remove export subject, verify messages stop flowing
  → Starting: Dynamic subject changes: Replace import subjects on leaf node
  ✓ Dynamic subject changes: Replace import subjects on leaf node
  → Starting: Wildcard subjects: Single-token (*) and multi-token (>) wildcards
  ✓ Wildcard subjects: Single-token (*) and multi-token (>) wildcards
✓ HubAndSpokeTests completed
```

## Architecture Tested

```
┌─────────────────────────────────────────┐
│              Hub Server                  │
│         (Port 4222 - clients)            │
│       (Port 7422 - leaf nodes)           │
│                                          │
│  Exports: hub.>, data.>                  │
│  Imports: leaf.>                         │
└────────┬─────────────────────┬───────────┘
         │                     │
         │ Leaf Connection     │ Leaf Connection
         │                     │
    ┌────▼─────┐          ┌────▼─────┐
    │  Leaf 1  │          │  Leaf 2  │
    │Port 4223 │          │Port 4224 │
    │          │          │          │
    │Imports:  │          │Imports:  │
    │  hub.>   │          │  hub.>   │
    │Exports:  │          │Exports:  │
    │  leaf.>  │          │  leaf.>  │
    └──────────┘          └──────────┘
```

## Key Concepts Tested

### Import/Export Subjects

- **Export**: Subjects that a server makes available to connected leaf nodes
- **Import**: Subjects that a server receives from remote servers
- Hub exports → Leaf imports: Hub-to-leaf communication
- Leaf exports → Hub imports: Leaf-to-hub communication

### Hot Reload

All tests verify that configuration changes apply without server restart:
- Add subjects: `AddLeafNodeImportSubjectsAsync()`, `AddLeafNodeExportSubjectsAsync()`
- Remove subjects: `RemoveLeafNodeImportSubjectsAsync()`, `RemoveLeafNodeExportSubjectsAsync()`
- Replace subjects: `SetLeafNodeImportSubjectsAsync()`, `SetLeafNodeExportSubjectsAsync()`

### Subject Wildcards

- `*` (single-token): Matches exactly one token (e.g., `events.*.created` matches `events.user.created`)
- `>` (multi-token): Matches one or more tokens (e.g., `data.>` matches `data.cpu` and `data.memory.usage`)

## Timing and Synchronization

The tests use several timing strategies to ensure reliable results:

1. **Server Readiness**: `WaitForReadyAsync(timeoutSeconds: 5)` ensures servers are fully started
2. **Connection Establishment**: `Task.Delay(1000)` after leaf configuration allows connection to hub
3. **Subscription Establishment**: `Task.Delay(500)` after subscribing ensures subscription is active
4. **Message Delivery**: Timeouts (typically 5 seconds) prevent tests from hanging
5. **Config Reload**: `Task.Delay(1000)` after hot reload allows configuration to propagate

## Error Handling

Each test includes timeout protection:
```csharp
var timeoutTask = Task.Delay(5000);
var completedTask = await Task.WhenAny(subscriptionTask, timeoutTask);

if (completedTask == timeoutTask)
{
    throw new Exception("Timeout waiting for messages");
}
```

This ensures tests fail gracefully rather than hanging indefinitely.

## Common Patterns

### Test Structure

```csharp
await results.AssertNoExceptionAsync(
    "Test description",
    async () =>
    {
        // 1. Start servers
        using var hub = new NatsController();
        using var leaf = new NatsController();

        // 2. Configure servers
        await hub.ConfigureAsync(...);
        await leaf.ConfigureAsync(...);

        // 3. Wait for readiness
        await hub.WaitForReadyAsync(timeoutSeconds: 5);
        await leaf.WaitForReadyAsync(timeoutSeconds: 5);

        // 4. Connect NATS clients
        await using var hubClient = new NatsClient("nats://localhost:4222");
        await using var leafClient = new NatsClient("nats://localhost:4223");

        // 5. Set up subscriptions
        var subscription = await leafClient.SubscribeAsync<string>("subject");

        // 6. Publish messages
        await hubClient.PublishAsync("subject", "data");

        // 7. Verify results
        if (receivedMessages.Count != expected)
        {
            throw new Exception("Verification failed");
        }

        // 8. Cleanup
        await hub.ShutdownAsync();
        await leaf.ShutdownAsync();
    });
```

## Port Allocation

To avoid conflicts, tests use these ports:
- Hub client port: 4222
- Hub leaf node port: 7422
- Leaf 1 client port: 4223
- Leaf 2 client port: 4224

Tests run sequentially, so each test properly shuts down servers before the next test starts, preventing port conflicts.

## Troubleshooting

### Test Timeout
**Symptom**: Test fails with "Timeout waiting for messages"

**Possible Causes**:
1. Subject import/export mismatch
2. Leaf node not connecting to hub
3. Network delays

**Solutions**:
- Verify export subjects on hub match import subjects on leaf
- Increase connection delay after leaf configuration
- Check server logs for connection errors

### No Messages Received
**Symptom**: receivedMessages.Count is 0

**Possible Causes**:
1. Subscription not established before publishing
2. Subject wildcards don't match
3. Server not ready

**Solutions**:
- Increase delay after subscription
- Verify subject patterns match
- Use `WaitForReadyAsync()` before operations

### Port Already In Use
**Symptom**: Server fails to start with port conflict

**Possible Causes**:
1. Previous test didn't shut down properly
2. Another application using the port

**Solutions**:
- Ensure all tests call `ShutdownAsync()`
- Use `using` statements for automatic disposal
- Check for hanging processes: `lsof -i :4222`

## Performance Characteristics

Typical test execution times:
- Hub-to-leaf flow: ~2-3 seconds
- Leaf-to-hub flow: ~2-3 seconds
- Bidirectional: ~3-4 seconds
- Multiple leaf nodes: ~3-4 seconds
- Dynamic changes: ~3-5 seconds (includes reload time)

Total suite execution: ~25-30 seconds

## Future Enhancements

Potential additions to the test suite:

1. **Authentication**: Test leaf nodes with username/password
2. **TLS**: Test encrypted leaf node connections
3. **Resilience**: Test automatic reconnection when hub restarts
4. **Performance**: Measure message throughput in hub-and-spoke topology
5. **Large-scale**: Test with 10+ leaf nodes
6. **Subject Conflicts**: Test overlapping import/export patterns
7. **Queue Groups**: Test queue subscribers across hub and leaf
8. **Request-Reply**: Test request-reply patterns across topology

## Related Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) - Overall system architecture
- [API_DESIGN.md](API_DESIGN.md) - Complete API reference including leaf node methods
- [GETTING_STARTED.md](GETTING_STARTED.md) - Initial setup and basic usage
- [LeafNodeConfigurationTests.cs](../src/DotGnatly.IntegrationTests/LeafNodeConfigurationTests.cs) - Configuration-only tests

## References

- [NATS Leaf Nodes Documentation](https://docs.nats.io/running-a-nats-service/configuration/leafnodes)
- [NATS.Net Client Library](https://nats-io.github.io/nats.net/)
- [NATS Subject Wildcards](https://docs.nats.io/nats-concepts/subjects#wildcards)
