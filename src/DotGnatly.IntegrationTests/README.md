# DotGnatly Integration Tests

Comprehensive integration test suite for DotGnatly to ensure all functionality works correctly, including multi-server support and hot configuration reloads.

## Test Suites

### 1. MultiServerTests
Tests running multiple NATS servers in a single process.

**Tests:**
- Multiple servers on different ports can start simultaneously
- Multiple servers maintain independent configurations
- Concurrent hot reloads on multiple servers
- Sequential server lifecycle
- Multiple servers with independent JetStream configurations
- Stress test: 10 concurrent servers

**Purpose:** Ensures proper isolation and no port conflicts when running multiple servers in one process.

### 2. LeafNodeConfigurationTests
Tests leaf node configuration including hot reload of import/export subjects.

**Tests:**
- Configure leaf node with import and export subjects
- Hot reload: Add import subjects to leaf node
- Hot reload: Remove import subjects from leaf node
- Hot reload: Add export subjects to leaf node
- Hot reload: Replace all import subjects
- Hot reload: Replace all export subjects
- Multiple sequential hot reloads of leaf node subjects
- Leaf node with wildcard patterns (* and >)

**Purpose:** Validates the new leaf node import/export subject configuration and hot reload capabilities.

### 3. ValidationTests
Tests configuration validation and error handling.

**Tests:**
- Validation rejects invalid port numbers
- Validation rejects invalid subject patterns
- Validation rejects empty subjects
- Validation accepts valid subject patterns
- Validation rejects port conflicts
- Validation enforces JetStream requirements
- Validation rejects mismatched auth credentials
- Validation rejects mismatched leaf node auth
- Hot reload validation prevents invalid changes
- Hot reload validation accepts valid changes
- Validation reports multiple errors
- Validation rejects invalid wildcard placement

**Purpose:** Ensures validation system catches configuration errors before they're applied.

### 4. EventSystemTests
Tests the event system for configuration changes.

**Tests:**
- ConfigurationChanging event fires before changes
- ConfigurationChanged event fires after changes
- ConfigurationChanging event can cancel changes
- ConfigurationChanged event provides correct diff
- Multiple event handlers all fire
- Event provides access to old and new configurations
- Events fire for leaf node subject changes
- Cancelled configuration remains unchanged
- Events fire in correct order (Changing then Changed)
- Events fire independently for multiple servers

**Purpose:** Validates the event-driven architecture for configuration changes.

### 5. ConcurrentOperationTests
Tests concurrent operations on brokers.

**Tests:**
- Concurrent configuration changes are handled safely
- Concurrent reads during configuration changes
- Concurrent leaf node subject modifications
- Concurrent operations on multiple independent servers
- Rapid sequential configuration changes
- Concurrent rollback attempts are handled safely
- Stress test: 100 rapid operations across multiple servers

**Purpose:** Ensures thread safety and proper locking mechanisms.

### 6. ConfigurationReloadTests
Tests hot reload functionality for various configuration scenarios.

**Tests:**
- Basic hot reload changes configuration
- Hot reload multiple properties simultaneously
- Hot reload increments version number
- Rollback restores previous configuration
- Multiple sequential hot reloads work correctly
- Hot reload with validation failure preserves original config
- JetStream can be toggled via hot reload
- Version history tracks all changes
- Hot reload of nested configuration (LeafNode)
- Hot reload preserves unmodified properties
- Fluent API extensions work for hot reload
- Authentication can be changed via hot reload

**Purpose:** Validates zero-downtime configuration updates.

## Running the Tests

### Build the Project
```bash
dotnet build src/MessageBroker.IntegrationTests/MessageBroker.IntegrationTests.csproj
```

### Run All Tests

**Standard mode** (shows summary and failed tests only):
```bash
dotnet run --project src/MessageBroker.IntegrationTests/MessageBroker.IntegrationTests.csproj
```

**Verbose mode** (shows all test output):
```bash
dotnet run --project src/MessageBroker.IntegrationTests/MessageBroker.IntegrationTests.csproj -- --verbose
# or
dotnet run --project src/MessageBroker.IntegrationTests/MessageBroker.IntegrationTests.csproj -- -v
```

### Output Modes

**Standard mode:**
- Shows test summary
- Shows failed tests with error messages
- Suppresses individual test output for cleaner results
- Ideal for CI/CD pipelines

**Verbose mode:**
- Shows all test suite progress
- Shows individual test start/completion
- Shows detailed test output from test methods
- Shows full stack traces for exceptions
- **Shows NATS server logs in real-time** (Linux/macOS only)
- Ideal for debugging test failures

### Expected Output (Standard Mode)
```
========================================
DotGnatly Integration Tests
========================================

========================================
Test Results Summary
========================================
Total Tests: 62
Passed: 62
Failed: 0
Success Rate: 100.0%

✓ All integration tests passed!
```

### Expected Output (Verbose Mode)
```
========================================
DotGnatly Integration Tests
========================================

Running in VERBOSE mode - showing all test output

Running 12 test suites...

[1/12] Running MultiServerTests...
------------------------------------------------------------
  → Starting: Multiple servers on different ports can start simultaneously
  ✓ Multiple servers on different ports can start simultaneously
  → Starting: Multiple servers maintain independent configurations
  ✓ Multiple servers maintain independent configurations
  ...
✓ MultiServerTests completed

[2/12] Running LeafNodeConfigurationTests...
------------------------------------------------------------
  → Starting: Configure leaf node with import and export subjects
  ✓ Configure leaf node with import and export subjects
  ...
✓ LeafNodeConfigurationTests completed

...

========================================
Test Results Summary
========================================
Total Tests: 62
Passed: 62
Failed: 0
Success Rate: 100.0%

✓ All integration tests passed!
```

## Test Framework

The integration tests use a simple custom test framework that:
- Groups tests into logical suites
- Provides assertion helpers
- Tracks and reports results
- Supports async test methods
- Handles exceptions gracefully

## Key Features Tested

### Multi-Server Support
- ✅ Multiple servers can run simultaneously on different ports
- ✅ Servers maintain independent configurations
- ✅ No port conflicts or resource sharing issues
- ✅ Concurrent operations on different servers work correctly

### Leaf Node Configuration
- ✅ Import/export subjects can be configured
- ✅ Subjects can be added via hot reload
- ✅ Subjects can be removed via hot reload
- ✅ Subjects can be completely replaced
- ✅ Wildcard patterns (* and >) are supported
- ✅ Subject validation prevents invalid patterns

### Hot Reload
- ✅ Zero-downtime configuration updates
- ✅ Multiple properties can be changed simultaneously
- ✅ Version history is maintained
- ✅ Rollback to previous versions works
- ✅ Invalid changes are rejected by validation
- ✅ Events fire correctly during reload

### Thread Safety
- ✅ Concurrent configuration changes are safe
- ✅ Concurrent reads during writes work correctly
- ✅ No race conditions in multi-threaded scenarios
- ✅ Stress tests with 100+ concurrent operations pass

## Exit Codes
- `0` - All tests passed
- `1` - One or more tests failed

## CI/CD Integration

These tests can be integrated into CI/CD pipelines:

```yaml
# GitHub Actions example
- name: Run Integration Tests
  run: dotnet run --project src/MessageBroker.IntegrationTests/MessageBroker.IntegrationTests.csproj
```

## Contributing

When adding new features to DotGnatly:
1. Add corresponding integration tests
2. Ensure all existing tests still pass
3. Document the new tests in this README

## Notes

- Tests are designed to be fast and not require external dependencies
- Each test cleans up its resources (servers are properly shut down)
- Tests use different ports to avoid conflicts
- The test framework provides detailed error messages for failures

## NATS Server Log Streaming

In verbose mode on Linux/macOS, the integration tests capture and display NATS server logs in real-time using lock-free Unix domain sockets. This provides:

- **Real-time debugging**: See exactly what the NATS server is doing during tests
- **Zero performance impact**: OS-buffered streaming with no application-level locks
- **Automatic cleanup**: Logs are captured per-test-run and cleaned up automatically

The log streaming uses a Unix domain socket connection from the native Go bindings to the .NET test framework, providing lock-free, high-performance log capture without any mutex contention.

**Platform Support:**
- ✅ Linux: Full support via Unix domain sockets
- ✅ macOS: Full support via Unix domain sockets
- ❌ Windows: Not currently supported (could be added via Named Pipes)

**Example verbose output with NATS logs:**
```
[1/12] Running MultiServerTests...
------------------------------------------------------------
  → Starting: Multiple servers on different ports can start simultaneously
[NATS] [INF] Starting nats-server
[NATS] [INF]   Version:  2.11.0
[NATS] [INF]   Git:      [not set]
[NATS] [INF]   Listening on 127.0.0.1:14222
[NATS] [INF]   Server is ready
  ✓ Multiple servers on different ports can start simultaneously
```
