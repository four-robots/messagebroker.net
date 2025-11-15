# NATS Server Features - Implementation TODO

This document tracks the implementation status of NATS server features that are available in the Go `server.Server` object but not yet exposed through our MessageBroker.NET library.

## Implementation Status Legend
- ✅ Implemented
- 🚧 In Progress
- ⏳ Planned
- 🔴 Not Started

## High Priority Features

### 1. Monitoring & Statistics Endpoints

#### ✅ Varz() - Full Server Variables
- **Status**: **Fully Implemented** in `GetVarzAsync()`
- **Location**: `native/nats-bindings.go:843`, `NatsController.cs:830`
- **Purpose**: Complete server variables and statistics
- **Returns**:
  - Server ID, version, and Go version
  - CPU cores and memory usage
  - Connection counts and statistics
  - JetStream configuration (if enabled)
  - Uptime and start time
- **Use Case**: Comprehensive server monitoring
- **Priority**: HIGH
- **Completed**: 2025-11-15

#### ✅ Connz() - Connection Monitoring
- **Status**: **Implemented** in `GetConnzAsync()`
- **Location**: `native/nats-bindings.go:587`, `NatsController.cs:506`
- **Purpose**: List active connections with client details
- **Returns**:
  - Number of connections
  - List of clients (ID, IP, port, subscriptions count)
  - Connection uptime
  - Bytes in/out per connection
- **Use Case**: Monitor client connections, debug connection issues
- **Priority**: HIGH
- **Completed**: 2025-11-15

#### ✅ Subsz() - Subscription Information
- **Status**: **Implemented** in `GetSubszAsync()`
- **Location**: `native/nats-bindings.go:624`, `NatsController.cs:547`
- **Purpose**: Detailed view of all subscriptions
- **Returns**:
  - Total subscription count
  - Subscriptions by subject
  - Queue groups
- **Use Case**: Debug subscription issues, monitor subject usage
- **Priority**: HIGH
- **Completed**: 2025-11-15

#### ✅ Jsz() - JetStream Statistics
- **Status**: **Implemented** in `GetJszAsync()`
- **Location**: `native/nats-bindings.go:658`, `NatsController.cs:588`
- **Purpose**: JetStream monitoring and statistics
- **Returns**:
  - Stream count and details
  - Consumer count
  - Storage usage (memory/file)
  - Account-level JetStream stats
- **Use Case**: Essential for JetStream operations monitoring
- **Priority**: HIGH
- **Completed**: 2025-11-15

#### ✅ Routez() - Cluster Routing
- **Status**: **Implemented** in `GetRoutezAsync()`
- **Location**: `native/nats-bindings.go:695`, `NatsController.cs:628`
- **Purpose**: Cluster routing information
- **Returns**:
  - Routes between servers
  - Route connection states
  - Number of subscriptions per route
- **Use Case**: Debug cluster connectivity
- **Priority**: MEDIUM
- **Completed**: 2025-11-15

#### ✅ Leafz() - Leaf Node Information
- **Status**: **Implemented** in `GetLeafzAsync()`
- **Location**: `native/nats-bindings.go:722`, `NatsController.cs:668`
- **Purpose**: Leaf node connection details
- **Returns**:
  - Connected leaf nodes
  - Remote leaf connections
  - Account information
- **Use Case**: Monitor leaf node topology
- **Priority**: MEDIUM
- **Completed**: 2025-11-15

#### ✅ Accountz() - Account Monitoring
- **Status**: **Implemented** in `GetAccountzAsync()`
- **Location**: `native/nats-bindings.go:810`, `NatsController.cs:789`
- **Purpose**: Account-level monitoring
- **Returns**:
  - List of accounts
  - Connection counts per account
  - Subscription counts per account
  - System account information
- **Use Case**: Multi-tenant monitoring
- **Priority**: MEDIUM
- **Completed**: 2025-11-15

#### ✅ AccountStatz() - Account Statistics
- **Status**: **Implemented** in `GetAccountStatzAsync()`
- **Location**: `native/nats-bindings.go:985`, `NatsController.cs:1017`
- **Purpose**: Per-account usage statistics
- **Returns**:
  - server_id, timestamp, accounts array
  - Per-account: connections, leafnodes, total_conns, num_subscriptions
  - Sent/received bytes and message counts
  - Slow consumers count
  - Inbound/outbound message and byte statistics
- **Use Case**: Account billing, quota enforcement, performance monitoring
- **Priority**: MEDIUM
- **Completed**: 2025-11-15

#### ⏳ JszAccount() - Account JetStream Info
- **Purpose**: Account-specific JetStream statistics
- **Returns**:
  - Streams owned by account
  - Storage usage by account
- **Use Case**: JetStream multi-tenancy
- **Priority**: MEDIUM

#### ✅ Gatewayz() - Gateway Statistics
- **Status**: **Implemented** in `GetGatewayzAsync()`
- **Location**: `native/nats-bindings.go:867`, `NatsController.cs:871`
- **Purpose**: Cross-cluster gateway monitoring
- **Returns**:
  - Gateway connections
  - Inbound/outbound traffic
  - Server ID and gateway name
  - Connection details
- **Use Case**: Super-cluster monitoring
- **Priority**: MEDIUM
- **Completed**: 2025-11-15

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

#### ✅ DisconnectClientByID() - Force Disconnect
- **Status**: **Implemented** in `DisconnectClientAsync()`
- **Location**: `native/nats-bindings.go:749`, `NatsController.cs:709`
- **Purpose**: Forcefully disconnect a specific client
- **Parameters**: Client ID (from Connz)
- **Use Case**: Remove misbehaving clients, enforce policies
- **Priority**: HIGH
- **Completed**: 2025-11-15

#### ✅ GetClientInfo() - Get Client Info
- **Status**: **Implemented** in `GetClientInfoAsync()`
- **Location**: `native/nats-bindings.go:769`, `NatsController.cs:748`
- **Purpose**: Retrieve detailed information about a specific client
- **Parameters**: Client ID
- **Returns**: Full client connection details
- **Use Case**: Client debugging
- **Priority**: MEDIUM
- **Completed**: 2025-11-15

### 3. Account Management (Runtime)

#### ✅ RegisterAccount() - Create Account
- **Status**: **Implemented** in `RegisterAccountAsync()`
- **Location**: `native/nats-bindings.go:900`, `NatsController.cs:923`
- **Purpose**: Programmatically register accounts at runtime
- **Parameters**: Account name
- **Returns**: JSON with account info (name, connections, subscriptions, jetstream, system_account)
- **Use Case**: Dynamic multi-tenancy
- **Priority**: HIGH
- **Completed**: 2025-11-15

#### ✅ LookupAccount() - Query Account
- **Status**: **Implemented** in `LookupAccountAsync()`
- **Location**: `native/nats-bindings.go:942`, `NatsController.cs:970`
- **Purpose**: Retrieve account details by name
- **Parameters**: Account name
- **Returns**: JSON with account object (limits, stats, total_subs)
- **Use Case**: Account management UI
- **Priority**: HIGH
- **Completed**: 2025-11-15

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

### Phase 1: Essential Monitoring (Sprint 1) ✅ **COMPLETED**
1. ✅ Enhance Varz() to return full data structure
2. ✅ Implement Connz() - connection monitoring
3. ✅ Implement Subsz() - subscription monitoring
4. ✅ Implement Jsz() - JetStream stats
5. ✅ Add comprehensive tests for all monitoring endpoints
**Completed**: 2025-11-15

### Phase 2: Connection & Account Management (Sprint 2) ✅ **COMPLETED**
1. ✅ Implement DisconnectClientByID()
2. ✅ Implement RegisterAccount()
3. ✅ Implement LookupAccount()
4. ✅ Implement GetClientInfo() (was GetClient)
5. ✅ Add tests for connection and account management (7/7 completed)
**Status**: All connection and account management features complete
**Completed**: 2025-11-15

### Phase 3: Advanced Monitoring (Sprint 3) ✅ **COMPLETED**
1. ✅ Implement Routez() - cluster routes
2. ✅ Implement Leafz() - leaf nodes
3. ✅ Implement Accountz() - account monitoring
4. ✅ Implement Varz() - full server variables
5. ✅ Implement Gatewayz() - gateway monitoring
6. ✅ Implement AccountStatz() - account statistics
7. ✅ Add tests for all monitoring endpoints (11/11 completed)
**Status**: All monitoring endpoints complete
**Completed**: 2025-11-15

### Phase 4: Runtime Control (Sprint 4)
1. ⏳ Implement EnableJetStream() / DisableJetStream()
2. ⏳ Implement JetStreamEnabled()
3. ⏳ Implement ReadyForConnections() exposure
4. ⏳ Implement Running() status check
5. ⏳ Add tests for runtime control

### Phase 5: Advanced Features (Sprint 5)
1. ⏳ Implement Raftz() - Raft state
2. ⏳ Implement SetSystemAccount()
3. ⏳ Implement UpdateAccountClaims()
4. ⏳ Add tests for advanced features

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
- Go bindings tests: `native/nats-bindings_test.go` ✅ **Created** (30+ tests)
- C# binding tests: `src/MessageBroker.Nats.Tests/Implementation/NatsControllerMonitoringTests.cs` ✅ **Created** (40+ tests)
- Integration tests: `src/MessageBroker.IntegrationTests/MonitoringTests.cs` ✅ **Created** (11 tests)

## Documentation Updates

✅ **Completed**:
1. ✅ Created `docs/MONITORING.md` - Comprehensive 500+ line guide
2. ✅ Added monitoring examples to `src/MessageBroker.Examples/Monitoring/`
3. ✅ Created `IMPLEMENTATION_SUMMARY.md` - Technical documentation
4. ✅ Created 11 integration tests in `MonitoringTests.cs`
5. ✅ Created 30+ Go unit tests in `native/nats-bindings_test.go`
6. ✅ Created 40+ C# unit tests in `NatsControllerMonitoringTests.cs`
7. ✅ Created test documentation in `native/README_TESTS.md`

**Remaining**:
1. ⏳ Update `CLAUDE.md` with new features
2. ⏳ Update `docs/API_DESIGN.md` with new methods
3. ⏳ Update `docs/ARCHITECTURE.md` with monitoring architecture

## Current Implementation Status

### Implemented Features

**Server Lifecycle & Configuration:**
- ✅ Basic server lifecycle (Start, Shutdown, LameDuckMode)
- ✅ Configuration reload (hot reload support)
- ✅ JWT account creation
- ✅ Multi-server support
- ✅ Cluster configuration
- ✅ Leaf node configuration
- ✅ Authentication (username/password, token)

**Monitoring Endpoints (11):**
- ✅ **Varz** - Full server variables
- ✅ **Connz** - Connection monitoring
- ✅ **Subsz** - Subscription monitoring
- ✅ **Jsz** - JetStream statistics
- ✅ **Routez** - Cluster routing monitoring
- ✅ **Leafz** - Leaf node monitoring
- ✅ **Accountz** - Account monitoring
- ✅ **Gatewayz** - Gateway monitoring
- ✅ **AccountStatz** - Account statistics

**Connection Management (2):**
- ✅ **DisconnectClientByID** - Force disconnect clients
- ✅ **GetClientInfo** - Detailed client information

**Account Management (2):**
- ✅ **RegisterAccount** - Runtime account creation
- ✅ **LookupAccount** - Account queries

### Total Features
- **Implemented**: 21/35 (60%)
- **In Progress**: 0/35 (0%)
- **Planned**: 14/35 (40%)

---

**Last Updated**: 2025-11-15
