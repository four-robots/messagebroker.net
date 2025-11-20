# DotGnatly.Core.Tests

Unit tests for the DotGnatly.Core library.

## Overview

This project contains comprehensive unit tests for the DotGnatly.Core library using xUnit.

## Test Coverage

### Configuration Tests
- **BrokerConfigurationTests**: Tests for configuration models, cloning, and default values
- **ConfigurationDiffEngineTests**: Tests for configuration change detection and diff calculation
- **InMemoryConfigurationStoreTests**: Tests for version storage and retrieval

### Validation Tests
- **ConfigurationValidatorTests**: Comprehensive validation rule tests
- **ValidationResultTests**: Tests for validation result handling

## Running Tests

### Using dotnet CLI

```bash
# Run all tests in this project
dotnet test src/DotGnatly.Core.Tests/DotGnatly.Core.Tests.csproj

# Run with verbose output
dotnet test src/DotGnatly.Core.Tests/DotGnatly.Core.Tests.csproj --verbosity detailed

# Run with code coverage
dotnet test src/DotGnatly.Core.Tests/DotGnatly.Core.Tests.csproj --collect:"XPlat Code Coverage"
```

### Using Visual Studio

Open the Test Explorer and run all tests or individual test classes.

## Test Structure

```
DotGnatly.Core.Tests/
├── Configuration/
│   ├── BrokerConfigurationTests.cs
│   ├── ConfigurationDiffEngineTests.cs
│   └── InMemoryConfigurationStoreTests.cs
└── Validation/
    ├── ConfigurationValidatorTests.cs
    └── ValidationResultTests.cs
```

## Dependencies

- xUnit 2.9.0
- Microsoft.NET.Test.Sdk 17.11.0
- DotGnatly.Core (project reference)

## Notes

- All tests use xUnit's fact and theory attributes
- Tests follow the Arrange-Act-Assert pattern
- Test names clearly describe what is being tested
