# NATS Configuration File Parser

DotGnatly includes a parser that can convert NATS server configuration files (`.conf`) into `BrokerConfiguration` objects. This allows you to work with existing NATS configuration files or migrate from standalone NATS servers to DotGnatly.

## Features

- **Full NATS Config Format Support**: Parses the native NATS configuration format including nested blocks, arrays, and comments
- **Unit Parsing**: Automatically converts size units (MB, GB, KB) and time units (s, m, h) to appropriate values
- **Type Safety**: Converts dynamic configuration text into strongly-typed `BrokerConfiguration` objects
- **Validation Ready**: Parsed configurations can be validated using DotGnatly's built-in validation system

## Supported Configuration Properties

### Basic Server Settings
- `listen` - Server address and port (e.g., `127.0.0.1:4222`)
- `server_name` - Server identifier
- `monitor_port` - HTTP monitoring port
- `debug` - Enable debug logging
- `trace` - Enable trace logging (more verbose than debug)
- `log_file` - Path to log file
- `logfile_size_limit` - Maximum log file size before rotation (supports units: MB, GB, KB)
- `logfile_max_num` - Maximum number of log files to retain
- `max_payload` - Maximum message payload size (supports units)
- `write_deadline` - Write timeout (supports time units: s, m, h)
- `disable_sublist_cache` - Disable the subject list cache
- `system_account` - System account name for server events

### JetStream Settings
- `jetstream` - Enable/disable JetStream (`enabled`, `disabled`, `true`, `false`)
- JetStream block properties:
  - `store_dir` - Directory for JetStream storage
  - `domain` - JetStream domain name
  - `max_memory` - Maximum memory for JetStream
  - `max_file` / `max_file_store` - Maximum disk storage
  - `unique_tag` - Unique identifier for this JetStream server in a cluster

### Leaf Node Settings
- Leaf nodes block properties:
  - `port` - Leaf node listening port
  - `host` - Leaf node binding address
  - `advertise` - Advertise address for leaf nodes
  - `isolate_leafnode_interest` - Isolate leaf node interest propagation
  - `reconnect_delay` - Delay between reconnection attempts
  - `tls` - TLS configuration block (see TLS Configuration below)
  - `authorization` - Authorization configuration block
  - `remotes` - Array of remote leaf node connections

### TLS Configuration
- TLS block properties (for leaf nodes and remotes):
  - `cert_file` - Path to TLS certificate file
  - `key_file` - Path to TLS key file
  - `ca_cert_file` - Path to CA certificate file
  - `verify` - Verify client certificates
  - `timeout` - TLS handshake timeout
  - `handshake_first` - Perform TLS handshake before INFO protocol
  - `insecure` - Skip certificate verification (not recommended for production)
  - `cert_store` - Windows certificate store (e.g., "WindowsLocalMachine", "WindowsCurrentUser")
  - `cert_match_by` - Certificate matching method (e.g., "Subject")
  - `cert_match` - Certificate match pattern
  - `pinned_certs` - Array of pinned certificate fingerprints

### Authorization Configuration
- Authorization block properties:
  - `user` - Username for authentication
  - `password` - Password for authentication
  - `token` - Authentication token
  - `account` - Account assignment
  - `timeout` - Authorization timeout
  - `users` - Array of authorized users

### Accounts Configuration
- Accounts block with named account sections:
  - `jetstream` - Enable/disable JetStream for this account
  - `users` - Array of users (username/password pairs)
  - `imports` - Array of stream/service imports from other accounts
  - `exports` - Array of stream/service exports to other accounts
  - `mappings` - Subject transformation mappings

### Import/Export Configuration
- Import/export object properties:
  - `stream` or `service` - Type of import/export
  - `subject` - Subject pattern
  - `account` - Source/destination account (for imports)
  - `to` - Subject transformation (mapping)
  - `response_type` - Response type for services (single/stream)
  - `response_threshold` - Response timeout threshold

## Usage

### Basic Parsing

```csharp
using DotGnatly.Core.Parsers;
using DotGnatly.Core.Configuration;

// Parse from file
var config = NatsConfigParser.ParseFile("/path/to/nats-server.conf");

// Or parse from string content
var configContent = File.ReadAllText("/path/to/nats-server.conf");
var config = NatsConfigParser.Parse(configContent);

// Access parsed properties
Console.WriteLine($"Server: {config.ServerName}");
Console.WriteLine($"Port: {config.Port}");
Console.WriteLine($"JetStream: {config.Jetstream}");
Console.WriteLine($"Domain: {config.JetstreamDomain}");
```

### Using with NatsController

```csharp
using DotGnatly.Nats.Implementation;

// Parse configuration file
var config = NatsConfigParser.ParseFile("/etc/nats/nats-server.conf");

// Apply to controller
using var controller = new NatsController();
var result = await controller.ConfigureAsync(config);

if (result.Success)
{
    Console.WriteLine("Server configured successfully!");
}
```

### Example with Validation

```csharp
using DotGnatly.Core.Validation;

// Parse configuration
var config = NatsConfigParser.ParseFile("/etc/nats/nats-server.conf");

// Validate before applying
var validator = new ConfigurationValidator();
var validationResult = validator.Validate(config);

if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Validation Error: {error.Message}");
    }
    return;
}

// Apply validated configuration
using var controller = new NatsController();
await controller.ConfigureAsync(config);
```

## Example Configuration Files

### Basic Configuration

```conf
listen: 127.0.0.1:4222
server_name: "nats-server-01"
monitor_port: 8222
jetstream: enabled
jetstream {
    store_dir="/var/lib/nats/data-store"
    domain=basic
}
log_file: "/var/log/nats/nats-server-01.log"
max_payload: 8MB
accounts {
    SYS: {
        users: [ {user: admin, password: $2a$11$hash...} ]
    },
    APP: {
        jetstream: enabled
        users: [ {user: app-user, password: $2a$11$hash...} ]
    },
}
system_account: SYS
```

### Leaf Node Configuration

```conf
listen: 127.0.0.1:4223
server_name: "nats-leaf-01"
monitor_port: 8223
jetstream: enabled
jetstream {
    store_dir = "/var/lib/nats/leaf-data"
    domain = "leaf-01"
}
debug:   false
trace:   false
log_file: "/var/log/nats/nats-leaf-01.log"
logfile_size_limit: 100Mb
logfile_max_num: 10
max_payload: 8MB
write_deadline: 10s
leafnodes {
    port: 7422
    isolate_leafnode_interest: true
    reconnect_delay: 2s
    write_deadline: 30s
}
system_account: SYS
```

### Hub Configuration

```conf
listen: 127.0.0.1:4222
server_name: "nats-hub"
monitor_port: 8222
jetstream: enabled
jetstream {
    store_dir="/var/lib/nats/hub-data"
    domain=hub
}
debug:   false
trace:   false
log_file: "/var/log/nats/nats-hub.log"
logfile_size_limit: 100Mb
logfile_max_num: 10
disable_sublist_cache: false
max_payload: 8MB
write_deadline: 10s
leafnodes {
    port: 7422
    isolate_leafnode_interest: true
    write_deadline: 30s
    advertise: "10.0.0.21:7422"
}
system_account: SYS
```

## Size and Time Unit Parsing

The parser automatically converts size and time units:

### Size Units
- `B` - Bytes
- `KB` or `K` - Kilobytes (1024 bytes)
- `MB` or `M` - Megabytes (1024 KB)
- `GB` or `G` - Gigabytes (1024 MB)
- `TB` or `T` - Terabytes (1024 GB)

Example:
```conf
max_payload: 8MB        # Converts to 8,388,608 bytes
logfile_size_limit: 100Mb  # Converts to 104,857,600 bytes
```

### Time Units
- `ns` - Nanoseconds
- `us` - Microseconds
- `ms` - Milliseconds
- `s` - Seconds
- `m` - Minutes
- `h` - Hours

Example:
```conf
write_deadline: 10s     # Converts to 10 seconds
reconnect_delay: 2m     # Converts to 120 seconds
timeout: 1h            # Converts to 3600 seconds
```

## Utility Methods

The `NatsConfigParser` class provides public utility methods for parsing units:

```csharp
// Parse size units
long bytes = NatsConfigParser.ParseSize("8MB");        // Returns 8,388,608
long bytes2 = NatsConfigParser.ParseSize("100Mb");     // Returns 104,857,600

// Parse time units
int seconds = NatsConfigParser.ParseTimeSeconds("10s"); // Returns 10
int seconds2 = NatsConfigParser.ParseTimeSeconds("2m"); // Returns 120
```

## Advanced Features

The parser now includes full support for advanced NATS configuration features:

- **Accounts**: Complete account definitions with users, imports, exports, and mappings
- **TLS Configuration**: Full TLS support including cert files, Windows cert store, pinned certs, and timeouts
- **Authorization**: Complete authorization blocks with users, tokens, and account assignments
- **Leaf Node Remotes**: Full support for leaf node remote connections with TLS and credentials
- **Import/Export Mappings**: Subject mappings and transformations for account isolation
- **Response Types**: Service response types (single/stream) and response thresholds

### Accounts Configuration

Accounts are fully parsed with support for:
- Multiple users per account
- Stream and service imports/exports
- Subject mappings with "to" transformations
- JetStream enablement per account
- Response types and thresholds on exports

### TLS Configuration

Full TLS support includes:
- Certificate files (cert_file, key_file, ca_cert_file)
- Windows Certificate Store (cert_store, cert_match_by, cert_match)
- Certificate pinning (pinned_certs array)
- TLS options (verify, timeout, handshake_first, insecure)

### Leaf Node Configuration

Complete leaf node support:
- Advertise addresses
- Isolation settings (isolate_leafnode_interest)
- Reconnect delays
- TLS configuration
- Authorization blocks
- Multiple remotes with individual TLS settings

### Cluster Configuration

Basic cluster configuration is recognized. Detailed cluster settings are planned for future versions.

## Testing

The parser includes comprehensive test coverage with 111+ tests across multiple test files:

### Unit Tests
- **NatsConfigParserTests.cs** (27 tests) - Basic parsing, JetStream, leaf nodes, size/time units
- **NatsConfigParserErrorTests.cs** (28 tests) - Error handling and edge cases
- **NatsConfigParserAdvancedTests.cs** (36 tests) - Accounts, TLS, authorization, remotes
- **ActualConfigFilesTests.cs** (10 tests) - Real config file parsing

### Integration Tests
- **ConfigParserIntegrationTests.cs** (10 tests) - End-to-end parsing with NatsController

### Test Coverage
- Size unit parsing (B, KB, MB, GB, TB)
- Time unit parsing (ns, us, ms, s, m, h)
- Basic server configuration
- JetStream configuration
- Leaf node configuration with TLS and authorization
- Accounts with imports/exports
- Subject mappings and transformations
- Windows certificate store configuration
- Certificate pinning
- Response types and thresholds
- Error handling and malformed input
- Unicode and special character handling

To run the tests:

```bash
# Run all parser tests
dotnet test src/DotGnatly.Core.Tests/ --filter "NatsConfigParser"

# Run integration tests
dotnet test src/DotGnatly.IntegrationTests/ --filter "ConfigParser"

# Run all tests
dotnet test
```

## Interactive Example

Run the interactive config parser example:

```bash
cd src/DotGnatly.Examples
dotnet run
# Select option C: Config File Parser
```

This will parse all configuration files in `test-configs/` and display the parsed values, as well as export them to JSON format for easy inspection.

## See Also

- [GETTING_STARTED.md](GETTING_STARTED.md) - Basic usage of DotGnatly
- [API_DESIGN.md](API_DESIGN.md) - Complete API reference
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture details
- [NATS Configuration Documentation](https://docs.nats.io/running-a-nats-service/configuration) - Official NATS config format
