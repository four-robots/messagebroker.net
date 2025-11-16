# Getting Started with DotGnatly

## Overview

DotGnatly is a .NET library that provides full control over NATS server instances with runtime reconfiguration capabilities. This guide will help you get started quickly.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Common Scenarios](#common-scenarios)
- [Configuration Guide](#configuration-guide)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)
- [Next Steps](#next-steps)

---

## Prerequisites

### System Requirements

- **Operating System**: Windows 10+ or Linux (Ubuntu 20.04+, RHEL 8+, etc.)
- **.NET Runtime**: .NET 9.0 or later
- **Memory**: Minimum 512 MB RAM (1 GB+ recommended for JetStream)
- **Disk Space**: 100 MB for binaries, additional space for JetStream persistence

### Development Environment

- **IDE**: Visual Studio 2022, Rider 2024.1+, or VS Code with C# extension
- **.NET SDK**: .NET 9.0 SDK or later
- **Git**: For cloning the repository (optional)

### Platform-Specific Requirements

#### Windows
- Windows 10 version 1909 or later
- Visual C++ Redistributable (usually already installed)

#### Linux
- glibc 2.31 or later (Ubuntu 20.04+)
- For building from source: Go 1.21+ and GCC

---

## Installation

### Option 1: NuGet Package (Recommended)

```bash
# .NET CLI
dotnet add package DotGnatly

# Package Manager Console (Visual Studio)
Install-Package DotGnatly
```

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/dotgnatly.git
cd dotgnatly

# Build Go bindings
cd nats-csharp/NatsBindings
go build -buildmode=c-shared -o ../NatsSharp/nats-bindings.dll nats-bindings.go  # Windows
go build -buildmode=c-shared -o ../NatsSharp/nats-bindings.so nats-bindings.go   # Linux

# Build C# library
cd ../NatsSharp
dotnet build

# Run example
cd ../
dotnet run
```

### Verify Installation

Create a test file `VerifyInstall.cs`:

```csharp
using System;
using DotGnatly.Nats;

class Program
{
    static void Main()
    {
        using var controller = new NatsController();
        string url = controller.Start();

        if (url.Contains("nats://"))
        {
            Console.WriteLine($"Success! Server running at: {url}");
            var info = controller.GetInfo();
            Console.WriteLine($"Version: {info.Version}");
        }
        else
        {
            Console.WriteLine($"Installation issue: {url}");
        }
    }
}
```

Run it:
```bash
dotnet run
```

Expected output:
```
Success! Server running at: nats://localhost:4222
Version: 2.10.x
```

---

## Quick Start

### Your First NATS Server (30 seconds)

Create a new console application:

```bash
mkdir MyNatsApp
cd MyNatsApp
dotnet new console
dotnet add package DotGnatly
```

Replace `Program.cs` with:

```csharp
using System;
using DotGnatly.Nats;

// Create and start NATS server
using var controller = new NatsController();
string url = controller.Start();

Console.WriteLine($"NATS Server: {url}");
Console.WriteLine("Press any key to stop...");
Console.ReadKey();

// Server automatically stops when disposed
```

Run it:
```bash
dotnet run
```

That's it! You now have a running NATS controller.

---

## Common Scenarios

### Scenario 1: Basic Messaging Server

**Goal**: Start a NATS server for simple pub/sub messaging

```csharp
using System;
using DotGnatly.Nats;

class BasicMessagingServer
{
    static void Main()
    {
        using var controller = new NatsController();

        // Start with default settings
        var config = new BrokerConfiguration
        {
            Host = "localhost",
            Port = 4222,
            MaxPayload = 1048576  // 1MB max message size
        };

        string url = controller.Start(config);
        Console.WriteLine($"Messaging server running at: {url}");

        // Server info
        var info = controller.GetInfo();
        Console.WriteLine($"Server ID: {info.Id}");
        Console.WriteLine($"Connections: {info.Connections}");

        Console.WriteLine("\nServer running. Press Ctrl+C to stop.");
        Console.ReadKey();
    }
}
```

**Use Case**: Development, testing, simple message passing

---

### Scenario 2: JetStream Event Streaming

**Goal**: Enable persistent messaging with JetStream

```csharp
using System;
using System.IO;
using DotGnatly.Nats;

class EventStreamingServer
{
    static void Main()
    {
        using var controller = new NatsController();

        // Configure JetStream for persistence
        var config = new BrokerConfiguration
        {
            Host = "0.0.0.0",
            Port = 4222,

            // Enable JetStream
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "nats", "jetstream"
            ),
            JetstreamMaxMemory = 1024L * 1024 * 1024,    // 1 GB
            JetstreamMaxStore = 10L * 1024 * 1024 * 1024  // 10 GB
        };

        string url = controller.Start(config);
        Console.WriteLine($"JetStream server running at: {url}");

        var info = controller.GetInfo();
        Console.WriteLine($"JetStream Enabled: {info.JetstreamEnabled}");
        Console.WriteLine($"Storage: {config.JetstreamStoreDir}");

        Console.WriteLine("\nJetStream server running. Press Ctrl+C to stop.");
        Console.ReadKey();
    }
}
```

**Use Case**: Event sourcing, audit logs, reliable message delivery

---

### Scenario 3: Secure Server with Authentication

**Goal**: Run a NATS server with user authentication

```csharp
using System;
using DotGnatly.Nats;

class SecureServer
{
    static void Main()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "0.0.0.0",
            Port = 4222,

            // Configure authentication
            Auth = new AuthConfig
            {
                Username = "admin",
                Password = ReadPassword("Enter admin password: ")
            }
        };

        string url = controller.Start(config);
        Console.WriteLine($"Secure server running at: {url}");
        Console.WriteLine("Clients must authenticate with username/password");

        Console.WriteLine("\nServer running. Press Ctrl+C to stop.");
        Console.ReadKey();
    }

    static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        string password = "";
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(true);
            if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
            {
                password += key.KeyChar;
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, password.Length - 1);
                Console.Write("\b \b");
            }
        } while (key.Key != ConsoleKey.Enter);
        Console.WriteLine();
        return password;
    }
}
```

**Use Case**: Production environments, multi-tenant applications

---

### Scenario 4: Server with Monitoring

**Goal**: Enable HTTP monitoring endpoints

```csharp
using System;
using DotGnatly.Nats;

class MonitoredServer
{
    static void Main()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "localhost",
            Port = 4222,

            // Enable HTTP monitoring
            HTTPPort = 8222,
            HTTPHost = "0.0.0.0"
        };

        string url = controller.Start(config);
        Console.WriteLine($"NATS Server: {url}");
        Console.WriteLine($"Monitoring:  http://localhost:8222");
        Console.WriteLine("\nMonitoring Endpoints:");
        Console.WriteLine("  /varz     - Server info");
        Console.WriteLine("  /connz    - Connection info");
        Console.WriteLine("  /subsz    - Subscription info");
        Console.WriteLine("  /jsz      - JetStream info");

        Console.WriteLine("\nOpen http://localhost:8222/varz in your browser");
        Console.WriteLine("Press Ctrl+C to stop.");
        Console.ReadKey();
    }
}
```

**Use Case**: Production monitoring, observability, debugging

---

### Scenario 5: Hot Configuration Reload

**Goal**: Update server configuration without restart

```csharp
using System;
using System.Threading;
using DotGnatly.Nats;

class HotReloadDemo
{
    static void Main()
    {
        using var controller = new NatsController();

        // Initial configuration with debug enabled
        var initialConfig = new BrokerConfiguration
        {
            Port = 4222,
            Debug = true,
            Trace = true,
            HTTPPort = 8222
        };

        controller.Start(initialConfig);
        Console.WriteLine("Server started with DEBUG mode ON");

        // Simulate running for a while
        Console.WriteLine("\nRunning in debug mode for 5 seconds...");
        Thread.Sleep(5000);

        // Hot reload to production configuration
        Console.WriteLine("\nSwitching to PRODUCTION mode (hot reload)...");
        var productionConfig = new BrokerConfiguration
        {
            Debug = false,
            Trace = false
        };

        string result = controller.UpdateConfig(productionConfig);
        Console.WriteLine($"Reload result: {result}");
        Console.WriteLine("Server now running in production mode");
        Console.WriteLine("NO RESTART REQUIRED - Zero downtime!");

        // Verify the change
        var info = controller.GetInfo();
        Console.WriteLine($"\nServer still running: {info.ClientUrl}");
        Console.WriteLine($"Active connections: {info.Connections}");

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
```

**Use Case**: Production systems requiring zero downtime, dynamic configuration

---

### Scenario 6: Multi-Tenant Server

**Goal**: Create isolated accounts for different tenants

```csharp
using System;
using System.Collections.Generic;
using DotGnatly.Nats;

class MultiTenantServer
{
    static void Main()
    {
        using var controller = new NatsController();

        // Start server
        controller.Start();
        Console.WriteLine("Multi-tenant server started\n");

        // Create tenant accounts
        var tenants = new[]
        {
            new { Name = "acme-corp", MaxConn = 100, MaxSubs = 1000 },
            new { Name = "globex-inc", MaxConn = 50, MaxSubs = 500 },
            new { Name = "initech-llc", MaxConn = 25, MaxSubs = 250 }
        };

        Console.WriteLine("Creating tenant accounts:");
        foreach (var tenant in tenants)
        {
            var accountConfig = new AccountConfig
            {
                Name = tenant.Name,
                Description = $"Tenant account for {tenant.Name}",
                MaxConnections = tenant.MaxConn,
                MaxSubscriptions = tenant.MaxSubs
            };

            var accountInfo = controller.CreateAccount(accountConfig);
            Console.WriteLine($"  ✓ {accountInfo.Name}");
            Console.WriteLine($"    Max Connections: {tenant.MaxConn}");
            Console.WriteLine($"    Max Subscriptions: {tenant.MaxSubs}");
            Console.WriteLine();
        }

        Console.WriteLine("Multi-tenant server ready");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
```

**Use Case**: SaaS platforms, multi-customer deployments

---

## Configuration Guide

### Basic Configuration Options

```csharp
var config = new BrokerConfiguration
{
    // Network
    Host = "0.0.0.0",           // Bind address
    Port = 4222,                 // Client port

    // Performance
    MaxPayload = 1048576,        // 1MB max message size
    MaxControlLine = 4096,       // Max protocol line size
    PingInterval = 120,          // Seconds between pings
    MaxPingsOut = 2,             // Max unanswered pings
    WriteDeadline = 2,           // Write timeout in seconds

    // Logging
    Debug = false,               // Debug logging
    Trace = false                // Trace logging
};
```

### JetStream Configuration

```csharp
var config = new BrokerConfiguration
{
    // Enable JetStream
    Jetstream = true,

    // Storage configuration
    JetstreamStoreDir = "./data/jetstream",

    // Resource limits
    JetstreamMaxMemory = 1024 * 1024 * 1024,      // 1 GB memory
    JetstreamMaxStore = 10L * 1024 * 1024 * 1024   // 10 GB disk
};
```

### Security Configuration

```csharp
var config = new BrokerConfiguration
{
    // Username/Password authentication
    Auth = new AuthConfig
    {
        Username = "admin",
        Password = "SecurePassword123!"
    }

    // OR Token-based authentication
    // Auth = new AuthConfig
    // {
    //     Token = "MySecretToken123"
    // }
};
```

### Monitoring Configuration

```csharp
var config = new BrokerConfiguration
{
    // HTTP monitoring endpoint
    HTTPPort = 8222,
    HTTPHost = "0.0.0.0"  // Listen on all interfaces

    // HTTPS monitoring (requires TLS certs)
    // HTTPSPort = 8223
};
```

### Leaf Node Configuration

```csharp
var config = new BrokerConfiguration
{
    LeafNode = new LeafNodeConfig
    {
        Host = "0.0.0.0",
        Port = 7422,

        // Connect to hub server
        RemoteURLs = new List<string>
        {
            "nats://hub.example.com:7422"
        },

        // Authentication for hub connection
        AuthUsername = "leaf-node-1",
        AuthPassword = "SecureLeafPassword"
    }
};
```

---

## Best Practices

### 1. Always Use `using` Statement

```csharp
// Good - Ensures proper cleanup
using var controller = new NatsController();
controller.Start();

// Bad - May not cleanup properly
var controller = new NatsController();
controller.Start();
// Forget to call controller.Dispose()
```

### 2. Check Start Results

```csharp
string result = controller.Start(config);

if (result.Contains("failed") || result.Contains("error"))
{
    Console.WriteLine($"Failed to start: {result}");
    return;
}

Console.WriteLine($"Started successfully: {result}");
```

### 3. Store JetStream Data in Persistent Location

```csharp
// Good - Uses persistent application data folder
var dataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "MyApp", "jetstream"
);

var config = new BrokerConfiguration
{
    Jetstream = true,
    JetstreamStoreDir = dataPath
};

// Bad - Uses temporary directory
// JetstreamStoreDir = "./jetstream"  // May be cleared on restart
```

### 4. Use Environment Variables for Sensitive Data

```csharp
var config = new BrokerConfiguration
{
    Auth = new AuthConfig
    {
        Username = Environment.GetEnvironmentVariable("NATS_USER") ?? "admin",
        Password = Environment.GetEnvironmentVariable("NATS_PASS") ?? "password"
    }
};
```

### 5. Implement Health Checks

```csharp
using var controller = new NatsController();
controller.Start();

var healthTimer = new Timer(_ =>
{
    try
    {
        var info = controller.GetInfo();
        Console.WriteLine($"Health: {info.Connections} connections");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Health check failed: {ex.Message}");
    }
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

Console.ReadKey();
healthTimer.Dispose();
```

### 6. Graceful Shutdown

```csharp
using var controller = new NatsController();
controller.Start();

// Handle Ctrl+C gracefully
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down gracefully...");
    controller.Shutdown();
    Environment.Exit(0);
};

Console.WriteLine("Press Ctrl+C to stop");
Thread.Sleep(Timeout.Infinite);
```

---

## Troubleshooting

### Issue: "Server failed to start within timeout"

**Symptoms**: Server startup returns error after ~10 seconds

**Causes**:
- Port already in use
- Insufficient permissions
- JetStream storage directory not writable

**Solutions**:

```csharp
// Check if port is available
var config = new BrokerConfiguration { Port = 4222 };
string result = controller.Start(config);

if (result.Contains("failed"))
{
    Console.WriteLine("Port 4222 might be in use, trying 4223...");
    config.Port = 4223;
    result = controller.Start(config);
}
```

```csharp
// Ensure JetStream directory is writable
var jsDir = "./jetstream";
try
{
    Directory.CreateDirectory(jsDir);
    File.WriteAllText(Path.Combine(jsDir, "test.txt"), "test");
    File.Delete(Path.Combine(jsDir, "test.txt"));
}
catch (Exception ex)
{
    Console.WriteLine($"JetStream directory not writable: {ex.Message}");
}
```

---

### Issue: "Configuration update rejected"

**Symptoms**: `UpdateConfig()` returns error message

**Causes**:
- Invalid configuration values
- Incompatible setting changes
- Server not initialized

**Solutions**:

```csharp
// Validate configuration before updating
var newConfig = new BrokerConfiguration
{
    MaxPayload = 10 * 1024 * 1024  // 10MB
};

try
{
    string result = controller.UpdateConfig(newConfig);

    if (!result.Contains("success"))
    {
        Console.WriteLine($"Update rejected: {result}");
        // Keep current configuration
    }
    else
    {
        Console.WriteLine("Configuration updated successfully");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Update error: {ex.Message}");
}
```

---

### Issue: "DllNotFoundException" or "nats-bindings.dll not found"

**Symptoms**: Exception when creating `NatsController`

**Causes**:
- Missing platform bindings (DLL/SO file)
- Incorrect build configuration
- Wrong platform target

**Solutions**:

**Windows:**
```bash
# Ensure nats-bindings.dll is in output directory
cd NatsBindings
go build -buildmode=c-shared -o ../NatsSharp/nats-bindings.dll nats-bindings.go
```

**Linux:**
```bash
# Ensure nats-bindings.so is in output directory
cd NatsBindings
go build -buildmode=c-shared -o ../NatsSharp/nats-bindings.so nats-bindings.go
```

Verify the file is present:
```csharp
var bindingFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? "nats-bindings.dll"
    : "nats-bindings.so";

var bindingPath = Path.Combine(AppContext.BaseDirectory, bindingFile);
if (!File.Exists(bindingPath))
{
    Console.WriteLine($"ERROR: {bindingFile} not found at {bindingPath}");
}
else
{
    Console.WriteLine($"Found bindings: {bindingPath}");
}
```

---

### Issue: High Memory Usage

**Symptoms**: Process using more RAM than expected

**Causes**:
- JetStream caching too much data in memory
- Large number of active subscriptions
- Message backlog

**Solutions**:

```csharp
// Limit JetStream memory usage
var config = new BrokerConfiguration
{
    Jetstream = true,
    JetstreamMaxMemory = 512 * 1024 * 1024  // Limit to 512 MB
};

// Monitor memory usage
var info = controller.GetInfo();
Console.WriteLine($"Active connections: {info.Connections}");

// Reduce connection limits if needed
controller.UpdateConfig(new BrokerConfiguration
{
    MaxPayload = 1 * 1024 * 1024  // Reduce max message size
});
```

---

### Issue: Can't Connect from Client

**Symptoms**: Client connection refused or timeout

**Causes**:
- Firewall blocking port
- Server bound to localhost only
- Authentication required but not provided

**Solutions**:

```csharp
// Ensure server binds to all interfaces
var config = new BrokerConfiguration
{
    Host = "0.0.0.0",  // Not "localhost" or "127.0.0.1"
    Port = 4222
};

controller.Start(config);
var info = controller.GetInfo();
Console.WriteLine($"Server URL: {info.ClientUrl}");
Console.WriteLine("Ensure firewall allows port 4222");
```

For authentication issues:
```csharp
// Client must authenticate
using NATS.Client;

var opts = ConnectionFactory.GetDefaultOptions();
opts.Url = "nats://localhost:4222";
opts.User = "admin";
opts.Password = "password";

var conn = new ConnectionFactory().CreateConnection(opts);
```

---

## Next Steps

### Learn More

1. **API Design Documentation**: See [API_DESIGN.md](./API_DESIGN.md) for complete API reference
2. **Architecture Guide**: See [ARCHITECTURE.md](./ARCHITECTURE.md) for system architecture
3. **NATS Documentation**: Visit [docs.nats.io](https://docs.nats.io) for NATS concepts

### Sample Projects

Explore example implementations:

```bash
# Clone examples repository
git clone https://github.com/yourusername/dotgnatly-examples.git
cd dotgnatly-examples

# Run examples
dotnet run --project BasicMessaging
dotnet run --project JetStreamDemo
dotnet run --project MultiTenantApp
```

### Integration with NATS Clients

Use DotGnatly server with standard NATS clients:

```csharp
// Start embedded server
using var controller = new NatsController();
string url = controller.Start();

// Connect with nats-csharp client
using NATS.Client;

var conn = new ConnectionFactory().CreateConnection(url);

// Publish messages
conn.Publish("greet.joe", Encoding.UTF8.GetBytes("Hello Joe!"));

// Subscribe
conn.SubscribeAsync("greet.*", (sender, args) =>
{
    Console.WriteLine($"Received: {Encoding.UTF8.GetString(args.Message.Data)}");
});
```

### Production Deployment

For production deployments, consider:

1. **Persistent Storage**: Configure JetStream with durable storage
2. **Monitoring**: Enable HTTP monitoring and integrate with metrics systems
3. **Security**: Implement authentication and consider JWT-based auth
4. **High Availability**: Use leaf nodes or clustering (external NATS cluster)
5. **Resource Limits**: Set appropriate memory and connection limits
6. **Logging**: Configure appropriate log levels for production

Example production configuration:

```csharp
var config = new BrokerConfiguration
{
    Host = "0.0.0.0",
    Port = 4222,

    // Performance
    MaxPayload = 8 * 1024 * 1024,
    PingInterval = 120,
    MaxPingsOut = 2,

    // JetStream persistence
    Jetstream = true,
    JetstreamStoreDir = "/var/lib/nats/jetstream",
    JetstreamMaxMemory = 4L * 1024 * 1024 * 1024,
    JetstreamMaxStore = 100L * 1024 * 1024 * 1024,

    // Monitoring
    HTTPPort = 8222,
    HTTPHost = "0.0.0.0",

    // Security
    Auth = new AuthConfig
    {
        Username = Environment.GetEnvironmentVariable("NATS_USER"),
        Password = Environment.GetEnvironmentVariable("NATS_PASS")
    },

    // Production logging
    Debug = false,
    Trace = false
};

controller.Start(config);
```

---

## Community and Support

- **GitHub Issues**: Report bugs and request features
- **Discussions**: Ask questions and share ideas
- **NATS Slack**: Join the NATS community at [slack.nats.io](https://slack.nats.io)
- **Documentation**: Full docs at [docs.nats.io](https://docs.nats.io)

---

## Summary

You've learned how to:

1. Install and verify DotGnatly
2. Start a basic NATS server
3. Configure JetStream, authentication, and monitoring
4. Implement hot configuration reload
5. Create multi-tenant accounts
6. Troubleshoot common issues
7. Follow best practices for production

Start building with DotGnatly today!
