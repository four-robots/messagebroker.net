# NATS Server Features - Implementation TODO

This document tracks the implementation status of NATS server features that are available in the Go `server.Server` object but not yet exposed through our MessageBroker.NET library.

## Implementation Status Legend
- ✅ Implemented
- 🚧 In Progress
- ⏳ Planned
- 🔴 Not Started

## High Priority Features

### 1. Monitoring & Statistics Endpoints

#### ✅ Varz() - Basic Server Info
- Status: **Partially Implemented** in `GetServerInfo()`
- Location: `native/nats-bindings.go:374`
- Returns basic server variables and statistics
- **Enhancement Needed**: Return full Varz data structure

#### ⏳ Connz() - Connection Monitoring
- **Purpose**: List active connections with client details
- **Returns**:
  - Number of connections
  - List of clients (ID, IP, port, subscriptions count)
  - Connection uptime
  - Bytes in/out per connection
- **Use Case**: Monitor client connections, debug connection issues
- **Priority**: HIGH

#### ⏳ Subsz() - Subscription Information
- **Purpose**: Detailed view of all subscriptions
- **Returns**:
  - Total subscription count
  - Subscriptions by subject
  - Queue groups
- **Use Case**: Debug subscription issues, monitor subject usage
- **Priority**: HIGH

#### ⏳ Jsz() - JetStream Statistics
- **Purpose**: JetStream monitoring and statistics
- **Returns**:
  - Stream count and details
  - Consumer count
  - Storage usage (memory/file)
  - Account-level JetStream stats
- **Use Case**: Essential for JetStream operations monitoring
- **Priority**: HIGH

#### ⏳ Routez() - Cluster Routing
- **Purpose**: Cluster routing information
- **Returns**:
  - Routes between servers
  - Route connection states
  - Number of subscriptions per route
- **Use Case**: Debug cluster connectivity
- **Priority**: MEDIUM

#### ⏳ Leafz() - Leaf Node Information
- **Purpose**: Leaf node connection details
- **Returns**:
  - Connected leaf nodes
  - Remote leaf connections
  - Account information
- **Use Case**: Monitor leaf node topology
- **Priority**: MEDIUM

#### ⏳ Accountz() - Account Monitoring
- **Purpose**: Account-level monitoring
- **Returns**:
  - List of accounts
  - Connection counts per account
  - Subscription counts per account
- **Use Case**: Multi-tenant monitoring
- **Priority**: MEDIUM

#### ⏳ AccountStatz() - Account Statistics
- **Purpose**: Per-account usage statistics
- **Returns**:
  - Sent/received bytes per account
  - Message counts
  - Slow consumers
- **Use Case**: Account billing, quota enforcement
- **Priority**: MEDIUM

#### ⏳ JszAccount() - Account JetStream Info
- **Purpose**: Account-specific JetStream statistics
- **Returns**:
  - Streams owned by account
  - Storage usage by account
- **Use Case**: JetStream multi-tenancy
- **Priority**: MEDIUM

#### ⏳ Gatewayz() - Gateway Statistics
- **Purpose**: Cross-cluster gateway monitoring
- **Returns**:
  - Gateway connections
  - Inbound/outbound traffic
- **Use Case**: Super-cluster monitoring
- **Priority**: LOW (only if using gateways)

#### ⏳ Raftz() - Raft Consensus State
- **Purpose**: JetStream Raft clustering state
- **Returns**:
  - Leader/follower status
  - Raft log state
  - Cluster health
- **Use Case**: JetStream clustering debugging
- **Priority**: LOW (advanced use case)

#### ⏳ Ipqueuesz() - IP Queue Status
- **Purpose**: IP-based queue monitoring
- **Returns**: IP queue statistics
- **Use Case**: Advanced routing scenarios
- **Priority**: LOW

### 2. Connection Management

#### ⏳ DisconnectClientByID() - Force Disconnect
- **Purpose**: Forcefully disconnect a specific client
- **Parameters**: Client ID (from Connz)
- **Use Case**: Remove misbehaving clients, enforce policies
- **Priority**: HIGH

#### ⏳ GetClient() - Get Client Info
- **Purpose**: Retrieve detailed information about a specific client
- **Parameters**: Client ID
- **Returns**: Full client connection details
- **Use Case**: Client debugging
- **Priority**: MEDIUM

### 3. Account Management (Runtime)

#### 🔴 RegisterAccount() - Create Account
- **Purpose**: Programmatically register accounts at runtime
- **Parameters**: Account name, configuration
- **Use Case**: Dynamic multi-tenancy
- **Priority**: HIGH

#### 🔴 LookupAccount() - Query Account
- **Purpose**: Retrieve account details by name
- **Parameters**: Account name
- **Returns**: Account object with limits and stats
- **Use Case**: Account management UI
- **Priority**: HIGH

#### 🔴 SetSystemAccount() - System Account
- **Purpose**: Designate a special system account for server events
- **Parameters**: Account name
- **Use Case**: NATS system events, monitoring
- **Priority**: MEDIUM

#### 🔴 UpdateAccountClaims() - Update Claims
- **Purpose**: Update account JWT claims without restart
- **Parameters**: Account name, new JWT
- **Use Case**: Dynamic limit changes
- **Priority**: MEDIUM

#### ✅ CreateAccountWithJWT() - JWT Account Creation
- **Status**: Implemented
- **Location**: `native/nats-bindings.go:517`
- Returns JWT and public key

### 4. JetStream Runtime Control

#### ⏳ EnableJetStream() - Enable at Runtime
- **Purpose**: Enable JetStream after server start
- **Parameters**: JetStream configuration
- **Use Case**: Dynamic JetStream activation
- **Priority**: MEDIUM

#### ⏳ DisableJetStream() - Disable at Runtime
- **Purpose**: Disable JetStream without restart
- **Use Case**: Resource management
- **Priority**: MEDIUM

#### ⏳ JetStreamEnabled() - Check Status
- **Purpose**: Query if JetStream is currently enabled
- **Returns**: Boolean
- **Use Case**: Feature detection
- **Priority**: MEDIUM

#### ⏳ JetStreamStepdownStream() - Leader Election
- **Purpose**: Force Raft leader election for a stream
- **Parameters**: Stream name
- **Use Case**: Cluster rebalancing
- **Priority**: LOW

### 5. Server State & Health

#### ⏳ ReadyForConnections() - Health Check
- **Purpose**: Check if server is ready to accept connections
- **Currently**: Used internally, not exposed
- **Returns**: Boolean
- **Use Case**: Load balancer health checks
- **Priority**: MEDIUM

#### ⏳ ID() - Server Unique Identifier
- **Purpose**: Get server unique ID
- **Returns**: Server ID string
- **Use Case**: Cluster identification
- **Priority**: LOW

#### ⏳ Name() - Server Name
- **Purpose**: Get server name from config
- **Returns**: Server name
- **Use Case**: Display/logging
- **Priority**: LOW

#### ⏳ Running() - Server Status
- **Purpose**: Check if server is running
- **Returns**: Boolean
- **Use Case**: Status monitoring
- **Priority**: MEDIUM

### 6. Logging Control

#### ⏳ SetLogger() - Custom Logger
- **Purpose**: Inject custom logger implementation
- **Parameters**: Logger interface
- **Use Case**: Integration with application logging
- **Priority**: LOW

#### ⏳ ReOpenLogFile() - Log Rotation
- **Purpose**: Support log file rotation
- **Use Case**: Log management
- **Priority**: LOW

### 7. Advanced Configuration

#### ⏳ GetOpts() - Retrieve Options
- **Purpose**: Get current server options
- **Returns**: Full Options object
- **Use Case**: Configuration introspection
- **Priority**: LOW

#### ⏳ AccountResolver() - Custom Resolution
- **Purpose**: Set custom account resolution logic
- **Parameters**: Resolver interface
- **Use Case**: External account systems
- **Priority**: LOW

#### ⏳ EnableJetStreamClustering() - JS Clustering
- **Purpose**: Configure JetStream clustering
- **Parameters**: Clustering configuration
- **Use Case**: JetStream HA
- **Priority**: LOW

## Implementation Roadmap

### Phase 1: Essential Monitoring (Sprint 1)
1. ✅ Enhance Varz() to return full data structure
2. ⏳ Implement Connz() - connection monitoring
3. ⏳ Implement Subsz() - subscription monitoring
4. ⏳ Implement Jsz() - JetStream stats
5. ⏳ Add comprehensive tests for all monitoring endpoints

### Phase 2: Connection & Account Management (Sprint 2)
1. ⏳ Implement DisconnectClientByID()
2. ⏳ Implement RegisterAccount()
3. ⏳ Implement LookupAccount()
4. ⏳ Implement GetClient()
5. ⏳ Add tests for connection and account management

### Phase 3: Advanced Monitoring (Sprint 3)
1. ⏳ Implement Routez() - cluster routes
2. ⏳ Implement Leafz() - leaf nodes
3. ⏳ Implement Accountz() - account monitoring
4. ⏳ Implement AccountStatz() - account stats
5. ⏳ Add tests for advanced monitoring

### Phase 4: Runtime Control (Sprint 4)
1. ⏳ Implement EnableJetStream() / DisableJetStream()
2. ⏳ Implement JetStreamEnabled()
3. ⏳ Implement ReadyForConnections() exposure
4. ⏳ Implement Running() status check
5. ⏳ Add tests for runtime control

### Phase 5: Advanced Features (Sprint 5)
1. ⏳ Implement Gatewayz() - gateway monitoring
2. ⏳ Implement Raftz() - Raft state
3. ⏳ Implement SetSystemAccount()
4. ⏳ Implement UpdateAccountClaims()
5. ⏳ Add tests for advanced features

## Testing Strategy

### Unit Tests
- Each new Go binding function needs a corresponding test
- Test error conditions (server not running, invalid params)
- Test JSON serialization/deserialization

### Integration Tests
- Test monitoring endpoints with real server data
- Test connection management operations
- Test account lifecycle (create, lookup, update, delete)
- Test JetStream runtime control

### Test Locations
- Go bindings tests: `native/nats-bindings_test.go` (to be created)
- C# binding tests: `src/MessageBroker.Nats.Tests/Bindings/`
- Integration tests: `src/MessageBroker.IntegrationTests/MonitoringTests.cs` (to be created)

## Documentation Updates Required

After implementation:
1. Update `CLAUDE.md` with new features
2. Update `docs/API_DESIGN.md` with new methods
3. Update `docs/ARCHITECTURE.md` with monitoring architecture
4. Create new `docs/MONITORING.md` guide
5. Add examples to `src/MessageBroker.Examples/`

## Current Implementation Status

### Implemented Features
- ✅ Basic server lifecycle (Start, Shutdown, LameDuckMode)
- ✅ Configuration reload (hot reload support)
- ✅ Basic server info (Varz - partial)
- ✅ JWT account creation
- ✅ Multi-server support
- ✅ Cluster configuration
- ✅ Leaf node configuration
- ✅ Authentication (username/password, token)

### In Progress
- 🚧 Full monitoring endpoint implementation

### Total Features
- **Implemented**: 8/35 (23%)
- **In Progress**: 1/35 (3%)
- **Planned**: 26/35 (74%)

---

**Last Updated**: 2025-11-15
**Current Sprint**: Phase 1 - Essential Monitoring
**Target Completion**: TBD
