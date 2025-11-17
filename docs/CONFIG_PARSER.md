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

### Leaf Node Settings
- Leaf nodes block properties:
  - `port` - Leaf node listening port
  - `host` - Leaf node binding address

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

## Current Limitations

The parser currently supports basic NATS configuration properties. The following advanced features are recognized but not fully parsed:

- **Accounts**: Account definitions are recognized but imports/exports are not yet parsed into structured objects
- **TLS Configuration**: TLS blocks are recognized but detailed TLS settings are not yet extracted
- **Authorization**: Authorization blocks are recognized but not fully parsed
- **Clustering**: Cluster configuration blocks are recognized but detailed settings are not yet extracted

These features will be added in future versions. For now, basic server configuration, JetStream, and leaf node settings are fully supported.

## Testing

The parser includes comprehensive unit tests in `DotGnatly.Core.Tests/Parsers/NatsConfigParserTests.cs`:

- Size unit parsing (MB, GB, KB)
- Time unit parsing (s, m, h)
- Basic configuration parsing
- JetStream configuration parsing
- Leaf node configuration parsing
- Comment handling
- Boolean value parsing

To run the tests:

```bash
dotnet test src/DotGnatly.Core.Tests/
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
