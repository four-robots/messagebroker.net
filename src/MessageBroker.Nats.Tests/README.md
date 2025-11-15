# MessageBroker.Nats.Tests

Unit tests for the MessageBroker.Nats library.

## Overview

This project contains unit tests for the MessageBroker.Nats library using xUnit.

## Test Coverage

### Implementation Tests
- **ConfigurationMapperTests**: Tests for bidirectional mapping between Core and Bindings configuration models
- **NatsControllerMonitoringTests**: Comprehensive tests for all monitoring endpoints (40+ tests)
  - GetConnzAsync() - Connection monitoring
  - GetSubszAsync() - Subscription monitoring
  - GetJszAsync() - JetStream statistics
  - GetRoutezAsync() - Cluster routing
  - GetLeafzAsync() - Leaf node monitoring
  - GetAccountzAsync() - Account monitoring
  - GetVarzAsync() - Server variables
  - GetGatewayzAsync() - Gateway monitoring
  - DisconnectClientAsync() - Client disconnection
  - GetClientInfoAsync() - Client information
  - Memory management (IntPtr disposal)
  - Thread safety (concurrent access)
  - Error handling (server not running, client not found)

## Running Tests

### Using dotnet CLI

```bash
# Run all tests in this project
dotnet test src/MessageBroker.Nats.Tests/MessageBroker.Nats.Tests.csproj

# Run with verbose output
dotnet test src/MessageBroker.Nats.Tests/MessageBroker.Nats.Tests.csproj --verbosity detailed

# Run with code coverage
dotnet test src/MessageBroker.Nats.Tests/MessageBroker.Nats.Tests.csproj --collect:"XPlat Code Coverage"
```

### Using Visual Studio

Open the Test Explorer and run all tests or individual test classes.

## Test Structure

```
MessageBroker.Nats.Tests/
└── Implementation/
    └── ConfigurationMapperTests.cs
```

## Dependencies

- xUnit 2.9.0
- Microsoft.NET.Test.Sdk 17.11.0
- Moq 4.20.72 (for mocking)
- MessageBroker.Core (project reference)
- MessageBroker.Nats (project reference)

## Notes

- All tests use xUnit's fact and theory attributes
- Tests follow the Arrange-Act-Assert pattern
- Configuration mapper tests verify round-trip conversions
- Tests do not require NATS server bindings
