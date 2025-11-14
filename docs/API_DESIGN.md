# MessageBroker.NET API Design

## Overview

MessageBroker.NET provides a fluent, type-safe C# API for controlling NATS servers via Go bindings. The key enhancement over standard nats-csharp client libraries is **runtime reconfiguration** with validation, versioning, and change notifications while maintaining full control over the embedded NATS server.

## Table of Contents

- [Core Concepts](#core-concepts)
- [Basic Server Operations](#basic-server-operations)
- [Runtime Configuration Management](#runtime-configuration-management)
- [Advanced Features](#advanced-features)
- [Comparison with nats-csharp](#comparison-with-nats-csharp)
- [Complete API Reference](#complete-api-reference)

---

## Core Concepts

### Server Lifecycle Management

MessageBroker.NET manages the complete lifecycle of a NATS server instance:

1. **Initialization** - Configure and start the server
2. **Runtime Configuration** - Hot-reload settings without restart
3. **Monitoring** - Real-time server metrics and status
4. **Shutdown** - Graceful server termination

### Key Differentiators

- **Embedded Server Control**: Direct control over NATS server via Go bindings
- **Hot Configuration Reload**: Change settings at runtime without downtime
- **Type-Safe Configuration**: Strongly-typed configuration objects with validation
- **JetStream Integration**: First-class JetStream persistence support
- **Multi-tenancy**: Built-in account and user management

---

## Basic Server Operations

### Simple Server Startup

**Minimal Example:**

```csharp
using NatsSharp;

// Create and start server with defaults (localhost:4222)
using var server = new NatsServer();
string url = server.Start();

Console.WriteLine($"Server running at: {url}");
// Output: Server running at: nats://localhost:4222
```

**Custom Configuration:**

```csharp
using var server = new NatsServer();

var config = new ServerConfig
{
    Host = "0.0.0.0",
    Port = 4222,
    Debug = false,
    Trace = false,
    MaxPayload = 1048576,  // 1MB
    PingInterval = 120,     // seconds
    MaxPingsOut = 2
};

string url = server.Start(config);
Console.WriteLine($"Server started: {url}");
```

### JetStream-Enabled Server

```csharp
using var server = new NatsServer();

var config = new ServerConfig
{
    Host = "localhost",
    Port = 4222,
    Jetstream = true,
    JetstreamStoreDir = "./data/jetstream",
    JetstreamMaxMemory = 1024 * 1024 * 1024,  // 1GB
    JetstreamMaxStore = 10 * 1024 * 1024 * 1024  // 10GB
};

string url = server.Start(config);
var info = server.GetInfo();

Console.WriteLine($"JetStream Enabled: {info.JetstreamEnabled}");
// Output: JetStream Enabled: True
```

### Server with Authentication

```csharp
using var server = new NatsServer();

var config = new ServerConfig
{
    Host = "localhost",
    Port = 4222,
    Auth = new AuthConfig
    {
        Username = "admin",
        Password = "SecurePassword123!"
    }
};

string url = server.Start(config);
Console.WriteLine($"Secure server started: {url}");
```

### Server with HTTP Monitoring

```csharp
using var server = new NatsServer();

var config = new ServerConfig
{
    Host = "localhost",
    Port = 4222,
    HTTPPort = 8222,
    HTTPHost = "0.0.0.0"
};

string url = server.Start(config);
Console.WriteLine($"Server: {url}");
Console.WriteLine($"Monitoring: http://localhost:8222");
// Access monitoring at http://localhost:8222/varz
```

---

## Runtime Configuration Management

### Hot Configuration Reload

**The Key Enhancement**: Change server configuration at runtime without restart or downtime.

#### Basic Reload

```csharp
using var server = new NatsServer();

// Start with initial configuration
var initialConfig = new ServerConfig
{
    Port = 4222,
    Debug = true,
    HTTPPort = 8222
};

server.Start(initialConfig);

// Application runs...

// Later: Update configuration at runtime
var updatedConfig = new ServerConfig
{
    Debug = false,      // Disable debug logging
    HTTPPort = 8223     // Change monitoring port
};

string result = server.UpdateConfig(updatedConfig);
Console.WriteLine($"Update result: {result}");
// Output: Configuration updated and reloaded successfully
```

#### Reload from Configuration File

```csharp
using var server = new NatsServer();

// Start from initial config file
server.StartFromConfigFile("./config/nats-initial.conf");

// Application runs...

// Later: Hot reload from updated config file
string result = server.ReloadFromFile("./config/nats-updated.conf");
Console.WriteLine($"Reload result: {result}");
```

### Configuration Validation Example

```csharp
using var server = new NatsServer();

try
{
    var config = new ServerConfig
    {
        Port = 4222,
        MaxPayload = 10 * 1024 * 1024,  // 10MB
        PingInterval = 60,
        MaxPingsOut = 3
    };

    server.Start(config);

    // Validate before updating
    var newConfig = new ServerConfig
    {
        MaxPayload = 5 * 1024 * 1024  // Reduce to 5MB
    };

    // Update will fail if invalid
    string result = server.UpdateConfig(newConfig);

    if (result.Contains("success"))
    {
        Console.WriteLine("Configuration updated successfully");
    }
    else
    {
        Console.WriteLine($"Configuration update failed: {result}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}
```

### Monitoring Configuration Changes

```csharp
using var server = new NatsServer();

// Initial configuration
var config = new ServerConfig
{
    Port = 4222,
    Debug = true,
    HTTPPort = 8222
};

server.Start(config);

// Get initial state
var info1 = server.GetInfo();
Console.WriteLine($"Initial - Debug mode, Port: {info1.Port}");

// Update configuration
var updatedConfig = new ServerConfig
{
    Debug = false
};

server.UpdateConfig(updatedConfig);

// Verify changes
var info2 = server.GetInfo();
Console.WriteLine($"Updated - Production mode, Port: {info2.Port}");
```

---

## Advanced Features

### Multi-Account Management

```csharp
using var server = new NatsServer();

var config = new ServerConfig
{
    Port = 4222,
    HTTPPort = 8222
};

server.Start(config);

// Create enterprise account
var enterpriseAccount = new AccountConfig
{
    Name = "enterprise",
    Description = "Enterprise tier account",
    MaxConnections = 1000,
    MaxSubscriptions = 10000
};

var accountInfo = server.CreateAccount(enterpriseAccount);
Console.WriteLine($"Created account: {accountInfo.Name} (ID: {accountInfo.Id})");

// Create development account
var devAccount = new AccountConfig
{
    Name = "development",
    Description = "Development environment",
    MaxConnections = 50,
    MaxSubscriptions = 500
};

var devInfo = server.CreateAccount(devAccount);
Console.WriteLine($"Created account: {devInfo.Name} (ID: {devInfo.Id})");
```

### JWT-Based Account Creation

```csharp
using var server = new NatsServer();

server.Start();

// Operator seed (in production, store securely)
string operatorSeed = "SOALU7LPGJK2BDF7IHD7UZT6ZM23UMKYLGJLNN35QJSUI5BNR4DJRFH4R4";

var accountConfig = new AccountConfig
{
    Name = "secure-account",
    Description = "JWT-secured account",
    MaxConnections = 100
};

var jwtResult = server.CreateAccountWithJWT(operatorSeed, accountConfig);

Console.WriteLine($"Account Public Key: {jwtResult.PublicKey}");
Console.WriteLine($"Account JWT: {jwtResult.JWT.Substring(0, 50)}...");
Console.WriteLine($"Account Seed: {jwtResult.Seed}");

// Store JWT and seed securely for client connections
```

### Leaf Node Configuration

```csharp
using var server = new NatsServer();

var config = new ServerConfig
{
    Port = 4222,
    LeafNode = new LeafNodeConfig
    {
        Host = "0.0.0.0",
        Port = 7422,
        RemoteURLs = new List<string>
        {
            "nats://hub.example.com:7422"
        },
        AuthUsername = "leaf-user",
        AuthPassword = "leaf-password"
    }
};

server.Start(config);
Console.WriteLine("Leaf node connected to hub server");
```

### Complete Production Example

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using NatsSharp;

class ProductionServer
{
    static async Task Main(string[] args)
    {
        using var server = new NatsServer();

        try
        {
            // Production configuration
            var config = new ServerConfig
            {
                Host = "0.0.0.0",
                Port = 4222,

                // Performance tuning
                MaxPayload = 8 * 1024 * 1024,  // 8MB
                MaxControlLine = 4096,
                PingInterval = 120,
                MaxPingsOut = 2,
                WriteDeadline = 10,

                // JetStream persistence
                Jetstream = true,
                JetstreamStoreDir = "/var/lib/nats/jetstream",
                JetstreamMaxMemory = 4L * 1024 * 1024 * 1024,   // 4GB
                JetstreamMaxStore = 100L * 1024 * 1024 * 1024,  // 100GB

                // Monitoring
                HTTPPort = 8222,
                HTTPHost = "0.0.0.0",

                // Security
                Auth = new AuthConfig
                {
                    Username = Environment.GetEnvironmentVariable("NATS_USER"),
                    Password = Environment.GetEnvironmentVariable("NATS_PASS")
                },

                // Logging
                Debug = false,
                Trace = false
            };

            // Start server
            string url = server.Start(config);
            Console.WriteLine($"Production server started: {url}");

            // Display server info
            var info = server.GetInfo();
            Console.WriteLine($"Server ID: {info.Id}");
            Console.WriteLine($"Version: {info.Version}");
            Console.WriteLine($"JetStream: {info.JetstreamEnabled}");
            Console.WriteLine($"Monitoring: http://localhost:8222/varz");

            // Set up configuration hot-reload watcher
            var cts = new CancellationTokenSource();
            var reloadTask = Task.Run(async () =>
            {
                var watcher = new FileSystemWatcher("/etc/nats");
                watcher.Filter = "server.conf";
                watcher.Changed += (s, e) =>
                {
                    Console.WriteLine("Configuration file changed, reloading...");
                    try
                    {
                        string result = server.ReloadFromFile(e.FullPath);
                        Console.WriteLine($"Reload result: {result}");

                        // Log configuration change
                        var newInfo = server.GetInfo();
                        Console.WriteLine($"Updated server info - Connections: {newInfo.Connections}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Reload failed: {ex.Message}");
                    }
                };
                watcher.EnableRaisingEvents = true;

                await Task.Delay(Timeout.Infinite, cts.Token);
            }, cts.Token);

            // Periodic health check
            var healthTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), cts.Token);

                    var currentInfo = server.GetInfo();
                    Console.WriteLine($"Health Check - Connections: {currentInfo.Connections}, " +
                                    $"JetStream: {currentInfo.JetstreamEnabled}");
                }
            }, cts.Token);

            // Wait for shutdown signal
            Console.WriteLine("\nServer running. Press Ctrl+C to shutdown...");
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            await Task.WhenAny(reloadTask, healthTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        // Graceful shutdown
        Console.WriteLine("Shutting down server...");
        server.Shutdown();
    }
}
```

---

## Comparison with nats-csharp

### Standard nats-csharp (Client Only)

The standard nats-csharp library provides **client** functionality only:

```csharp
// nats-csharp - CLIENT LIBRARY
// Requires external NATS server to be running

using NATS.Client;

var opts = ConnectionFactory.GetDefaultOptions();
opts.Url = "nats://localhost:4222";  // Must connect to existing server

using var conn = new ConnectionFactory().CreateConnection(opts);

// Can only:
// - Connect to server
// - Publish/Subscribe to messages
// - Use JetStream (if server has it enabled)

// CANNOT:
// - Start/stop NATS server
// - Configure server settings
// - Hot-reload configuration
// - Manage accounts
// - Control JetStream on server
```

**Limitations:**
- No server control
- Requires separate NATS server installation
- Cannot reconfigure server at runtime
- No embedded server support
- Manual server lifecycle management

### MessageBroker.NET (Server Control)

MessageBroker.NET provides **full server control** via Go bindings:

```csharp
// MessageBroker.NET - SERVER CONTROL
// Embedded NATS server with full lifecycle management

using NatsSharp;

using var server = new NatsServer();

// Start server with configuration
var config = new ServerConfig
{
    Port = 4222,
    Jetstream = true,
    Debug = true
};

server.Start(config);

// Hot-reload configuration (NO RESTART NEEDED)
server.UpdateConfig(new ServerConfig
{
    Debug = false,
    HTTPPort = 8222
});

// Create accounts
server.CreateAccount(new AccountConfig
{
    Name = "enterprise",
    MaxConnections = 1000
});

// Get server metrics
var info = server.GetInfo();
Console.WriteLine($"Connections: {info.Connections}");

// Full control over server lifecycle
server.Shutdown();
```

**Capabilities:**
- Full server lifecycle control
- Runtime configuration hot-reload
- Account and user management
- JetStream configuration
- Server monitoring and metrics
- No external dependencies

### Side-by-Side Feature Comparison

| Feature | nats-csharp | MessageBroker.NET |
|---------|-------------|-------------------|
| **Client Operations** | Yes | Via nats-csharp client |
| **Server Control** | No | Yes |
| **Start/Stop Server** | No | Yes |
| **Runtime Reconfiguration** | No | **Yes** |
| **Hot Configuration Reload** | No | **Yes** |
| **Account Management** | No | Yes |
| **JetStream Control** | Client only | Server & Client |
| **Server Monitoring** | No | Yes |
| **Embedded Server** | No | Yes |
| **Configuration Validation** | No | Yes |
| **Leaf Node Setup** | No | Yes |
| **Type-Safe Config** | N/A | Yes |

### Usage Scenario Comparison

#### Scenario 1: Simple Pub/Sub Application

**With nats-csharp:**
```csharp
// Step 1: Install and start NATS server separately
// $ docker run -p 4222:4222 nats:latest

// Step 2: Connect from C# application
using NATS.Client;

var conn = new ConnectionFactory().CreateConnection("nats://localhost:4222");
conn.SubscribeAsync("orders.*", (sender, args) =>
{
    Console.WriteLine($"Received: {Encoding.UTF8.GetString(args.Message.Data)}");
});
```

**With MessageBroker.NET:**
```csharp
// Step 1: Start embedded server
using NatsSharp;

var server = new NatsServer();
server.Start();

// Step 2: Connect using nats-csharp client
using NATS.Client;

var conn = new ConnectionFactory().CreateConnection(server.GetUrl());
conn.SubscribeAsync("orders.*", (sender, args) =>
{
    Console.WriteLine($"Received: {Encoding.UTF8.GetString(args.Message.Data)}");
});

// Benefits: No external dependencies, full control
```

#### Scenario 2: Configuration Changes

**With nats-csharp:**
```csharp
// Step 1: Stop NATS server
// Step 2: Edit nats-server.conf file
// Step 3: Restart NATS server
// Step 4: Reconnect all clients

// DOWNTIME REQUIRED
```

**With MessageBroker.NET:**
```csharp
using var server = new NatsServer();
server.Start();

// No downtime - hot reload
server.UpdateConfig(new ServerConfig
{
    Debug = false,
    MaxPayload = 10 * 1024 * 1024
});

// Server continues running, clients stay connected
// ZERO DOWNTIME
```

#### Scenario 3: Multi-Tenant Application

**With nats-csharp:**
```csharp
// Manual account setup via nsc tool or config files
// Requires server restart to apply changes
// Complex JWT management
```

**With MessageBroker.NET:**
```csharp
using var server = new NatsServer();
server.Start();

// Programmatic account creation
var tenants = new[] { "acme-corp", "globex", "initech" };

foreach (var tenant in tenants)
{
    var account = server.CreateAccount(new AccountConfig
    {
        Name = tenant,
        MaxConnections = 100
    });

    Console.WriteLine($"Created tenant: {account.Name}");
}

// Accounts active immediately, no restart needed
```

---

## Complete API Reference

### NatsServer Class

#### Constructors

```csharp
public NatsServer()
```
Creates a new NatsServer instance. Automatically detects OS and loads appropriate bindings.

#### Server Control Methods

```csharp
public string Start(ServerConfig config)
```
Starts the NATS server with the specified configuration.

**Parameters:**
- `config`: ServerConfig object with server settings

**Returns:** Client URL string (e.g., "nats://localhost:4222") or error message

**Example:**
```csharp
var config = new ServerConfig { Port = 4222 };
string url = server.Start(config);
```

---

```csharp
public string Start(string host = "localhost", int port = 4222)
```
Convenience method to start server with minimal configuration.

**Parameters:**
- `host`: Server host address (default: "localhost")
- `port`: Server port (default: 4222)

**Returns:** Client URL string or error message

**Example:**
```csharp
string url = server.Start("0.0.0.0", 4222);
```

---

```csharp
public string StartWithJetStream(string host = "localhost", int port = 4222,
                                 string storeDir = "./jetstream_data")
```
Starts server with JetStream enabled.

**Parameters:**
- `host`: Server host address
- `port`: Server port
- `storeDir`: JetStream storage directory

**Returns:** Client URL string or error message

---

```csharp
public string StartFromConfigFile(string configFilePath)
```
Starts server from a NATS configuration file.

**Parameters:**
- `configFilePath`: Path to .conf file

**Returns:** Client URL string or error message

---

```csharp
public void Shutdown()
```
Gracefully shuts down the NATS server. Called automatically by Dispose().

#### Configuration Methods

```csharp
public string Reload()
```
Reloads the current server configuration.

**Returns:** Success or error message

---

```csharp
public string ReloadFromFile(string configFilePath)
```
Reloads server configuration from a file.

**Parameters:**
- `configFilePath`: Path to updated .conf file

**Returns:** Success or error message

---

```csharp
public string UpdateConfig(ServerConfig config)
```
Updates and reloads server configuration without restart.

**Parameters:**
- `config`: New ServerConfig object (only specify changed fields)

**Returns:** Success or error message

**Example:**
```csharp
var updated = new ServerConfig { Debug = false };
string result = server.UpdateConfig(updated);
```

#### Monitoring Methods

```csharp
public string GetUrl()
```
Gets the server's client connection URL.

**Returns:** Client URL (e.g., "nats://localhost:4222")

---

```csharp
public ServerInfo GetInfo()
```
Gets detailed server information and metrics.

**Returns:** ServerInfo object with current server state

**Example:**
```csharp
var info = server.GetInfo();
Console.WriteLine($"Connections: {info.Connections}");
Console.WriteLine($"Version: {info.Version}");
```

#### Account Management Methods

```csharp
public AccountInfo CreateAccount(AccountConfig config)
```
Creates a new account on the server.

**Parameters:**
- `config`: Account configuration

**Returns:** AccountInfo with account details

---

```csharp
public JWTAccountResult CreateAccountWithJWT(string operatorSeed, AccountConfig config)
```
Creates a JWT-secured account.

**Parameters:**
- `operatorSeed`: Operator seed for signing JWTs
- `config`: Account configuration

**Returns:** JWTAccountResult with JWT, seed, and public key

### Configuration Classes

#### ServerConfig

```csharp
public class ServerConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 4222;
    public int MaxPayload { get; set; } = 1048576;  // 1MB
    public int MaxControlLine { get; set; } = 4096;
    public int PingInterval { get; set; } = 120;  // seconds
    public int MaxPingsOut { get; set; } = 2;
    public int WriteDeadline { get; set; } = 2;  // seconds
    public bool Debug { get; set; } = false;
    public bool Trace { get; set; } = false;

    // JetStream
    public bool Jetstream { get; set; } = false;
    public string JetstreamStoreDir { get; set; } = "./jetstream";
    public long JetstreamMaxMemory { get; set; } = -1;  // unlimited
    public long JetstreamMaxStore { get; set; } = -1;   // unlimited

    // Monitoring
    public int HTTPPort { get; set; } = 0;  // 0 = disabled
    public string HTTPHost { get; set; } = "0.0.0.0";
    public int HTTPSPort { get; set; } = 0;

    // Security
    public AuthConfig Auth { get; set; } = new AuthConfig();

    // Clustering
    public LeafNodeConfig LeafNode { get; set; } = new LeafNodeConfig();
}
```

#### AuthConfig

```csharp
public class AuthConfig
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Token { get; set; }
    public List<string> AllowedUsers { get; set; } = new List<string>();
}
```

#### AccountConfig

```csharp
public class AccountConfig
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int MaxConnections { get; set; } = -1;  // unlimited
    public int MaxSubscriptions { get; set; } = -1;
    public long MaxData { get; set; } = -1;
    public long MaxPayload { get; set; } = -1;
}
```

#### LeafNodeConfig

```csharp
public class LeafNodeConfig
{
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; }
    public List<string> RemoteURLs { get; set; } = new List<string>();
    public string AuthUsername { get; set; }
    public string AuthPassword { get; set; }
    public string TLSCert { get; set; }
    public string TLSKey { get; set; }
    public string TLSCACert { get; set; }
}
```

### Information Classes

#### ServerInfo

```csharp
public class ServerInfo
{
    public string Id { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public string ClientUrl { get; set; }
    public bool JetstreamEnabled { get; set; }
    public int Connections { get; set; }
    public string Version { get; set; }
}
```

#### AccountInfo

```csharp
public class AccountInfo
{
    public string Name { get; set; }
    public string Id { get; set; }
}
```

#### JWTAccountResult

```csharp
public class JWTAccountResult
{
    public string JWT { get; set; }
    public string Seed { get; set; }
    public string PublicKey { get; set; }
}
```

---

## Best Practices

### 1. Resource Management

Always use `using` statement for proper disposal:

```csharp
using var server = new NatsServer();
server.Start();
// Server automatically shut down when disposed
```

### 2. Configuration Validation

Validate configuration before applying:

```csharp
try
{
    var config = new ServerConfig { Port = 4222 };
    string result = server.UpdateConfig(config);

    if (!result.Contains("success"))
    {
        Console.WriteLine($"Configuration rejected: {result}");
        // Handle error or rollback
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}
```

### 3. Monitoring

Implement health checks:

```csharp
var healthTimer = new Timer(_ =>
{
    var info = server.GetInfo();
    if (info.Connections < 0)
    {
        Console.WriteLine("Server health check failed");
        // Alert or restart
    }
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
```

### 4. Graceful Shutdown

Handle shutdown signals properly:

```csharp
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    Console.WriteLine("Shutting down gracefully...");
    server.Shutdown();
};
```

### 5. Error Handling

Check return values for errors:

```csharp
string result = server.Start(config);
if (result.Contains("failed") || result.Contains("error"))
{
    Console.WriteLine($"Startup failed: {result}");
    return;
}
```

---

## Summary

MessageBroker.NET provides:

1. **Full Server Control**: Start, stop, and configure NATS servers programmatically
2. **Hot Configuration**: Update settings without restart or downtime
3. **Type Safety**: Strongly-typed configuration with compile-time checking
4. **Zero Dependencies**: Embedded server, no external installations needed
5. **Production Ready**: Monitoring, health checks, and graceful shutdown
6. **Multi-tenancy**: Built-in account and user management
7. **JetStream**: First-class persistence support

The key differentiator is **runtime reconfiguration** - something not possible with standard nats-csharp client libraries.
