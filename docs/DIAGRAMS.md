# MessageBroker.NET Architecture Diagrams

This document provides visual diagrams to help understand the MessageBroker.NET architecture, data flows, and deployment patterns.

## Table of Contents

- [System Architecture](#system-architecture)
- [Component Interaction](#component-interaction)
- [Configuration Flow](#configuration-flow)
- [Hot Reload Process](#hot-reload-process)
- [Deployment Patterns](#deployment-patterns)
- [Comparison Diagrams](#comparison-diagrams)

---

## System Architecture

### Full Stack Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         .NET Application                         │
│                                                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                   Your Application Code                    │  │
│  │                                                            │  │
│  │  using var server = new NatsServer();                     │  │
│  │  server.Start(config);                                    │  │
│  │  server.UpdateConfig(newConfig);  // Hot reload           │  │
│  └───────────────────────┬───────────────────────────────────┘  │
│                          │                                       │
│                          │ Type-safe API calls                   │
│                          ▼                                       │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              NatsServer C# Wrapper Class                   │  │
│  │                                                            │  │
│  │  • Configuration serialization (JSON)                     │  │
│  │  • Platform detection (Windows/Linux)                     │  │
│  │  • Memory management (Marshal/Free)                       │  │
│  │  • Error handling and validation                          │  │
│  └───────────────────────┬───────────────────────────────────┘  │
│                          │                                       │
│                          │ P/Invoke (DllImport)                  │
│                          ▼                                       │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │         Platform-Specific Bindings (INatsBindings)        │  │
│  │                                                            │  │
│  │  ┌──────────────────────┐  ┌──────────────────────────┐  │  │
│  │  │ WindowsNatsBindings  │  │  LinuxNatsBindings       │  │  │
│  │  │                      │  │                          │  │  │
│  │  │ nats-bindings.dll    │  │  nats-bindings.so        │  │  │
│  │  └──────────────────────┘  └──────────────────────────┘  │  │
│  └───────────────────────┬───────────────────────────────────┘  │
└──────────────────────────┼────────────────────────────────────────┘
                           │
                           │ CGO Bridge (C calling convention)
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Go Bindings Layer (CGO)                       │
│                                                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    nats-bindings.go                        │  │
│  │                                                            │  │
│  │  //export StartServer                                     │  │
│  │  //export UpdateAndReloadConfig                           │  │
│  │  //export GetServerInfo                                   │  │
│  │  //export CreateAccount                                   │  │
│  │  //export ShutdownServer                                  │  │
│  │                                                            │  │
│  │  • JSON parsing                                           │  │
│  │  • ServerConfig → server.Options conversion               │  │
│  │  • C string memory management                             │  │
│  └───────────────────────┬───────────────────────────────────┘  │
│                          │                                       │
│                          │ Direct Go API calls                   │
│                          ▼                                       │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              NATS Server Instance Management               │  │
│  │                                                            │  │
│  │  var natsServer *server.Server                            │  │
│  │                                                            │  │
│  │  natsServer, err = server.NewServer(opts)                 │  │
│  │  go natsServer.Start()                                    │  │
│  │  natsServer.Reload()                                      │  │
│  └───────────────────────┬───────────────────────────────────┘  │
└──────────────────────────┼────────────────────────────────────────┘
                           │
                           │ Go API
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│           NATS Server (github.com/nats-io/nats-server)          │
│                                                                   │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌──────────┐  │
│  │  Core      │  │ JetStream  │  │  Accounts  │  │ Leaf Node│  │
│  │  Pub/Sub   │  │ Streaming  │  │  Multi-    │  │ Clustering│ │
│  │  Engine    │  │ Persistence│  │  tenancy   │  │ Gateway  │  │
│  └────────────┘  └────────────┘  └────────────┘  └──────────┘  │
│                                                                   │
└───────────────────────────────────┬───────────────────────────────┘
                                    │
                                    │ NATS Protocol (TCP)
                                    ▼
                         ┌──────────────────────┐
                         │   NATS Clients       │
                         │                      │
                         │ • nats-csharp        │
                         │ • nats.js            │
                         │ • nats.go            │
                         │ • nats.py            │
                         └──────────────────────┘
```

---

## Component Interaction

### Method Call Flow: Start Server

```
C# Application
     │
     │ server.Start(config)
     ▼
NatsServer.cs
     │
     │ 1. Serialize config to JSON
     │    JsonSerializer.Serialize(config)
     │
     │ 2. Call platform binding
     │    NatsBindings.StartServer(configJson)
     ▼
WindowsNatsBindings.cs / LinuxNatsBindings.cs
     │
     │ [DllImport("nats-bindings.dll/so")]
     │ P/Invoke
     ▼
─────────────────────────────────────────────
CGO Boundary
─────────────────────────────────────────────
     ▼
nats-bindings.go
     │
     │ //export StartServer
     │ func StartServer(configJSON *C.char)
     │
     │ 3. Convert C string to Go string
     │    config := C.GoString(configJSON)
     │
     │ 4. Parse JSON
     │    json.Unmarshal([]byte(config), &serverConfig)
     │
     │ 5. Create server.Options
     │    opts := &server.Options{...}
     │
     │ 6. Create NATS server
     │    natsServer, err = server.NewServer(opts)
     │
     │ 7. Start server
     │    go natsServer.Start()
     │
     │ 8. Wait for ready
     │    natsServer.ReadyForConnections(timeout)
     │
     │ 9. Return client URL
     │    return C.CString(natsServer.ClientURL())
     ▼
─────────────────────────────────────────────
CGO Boundary
─────────────────────────────────────────────
     ▼
WindowsNatsBindings.cs / LinuxNatsBindings.cs
     │
     │ return IntPtr (C string pointer)
     ▼
NatsServer.cs
     │
     │ 10. Marshal pointer to .NET string
     │     Marshal.PtrToStringAnsi(resultPtr)
     │
     │ 11. Free C memory
     │     NatsBindings.FreeString(resultPtr)
     │
     │ 12. Return to application
     ▼
C# Application
     │
     │ string url = "nats://localhost:4222"
     ▼
```

---

## Configuration Flow

### Configuration Update Flow

```
┌──────────────────────────────────────────────────────────────┐
│                    Application Layer                          │
└──────────────────────────┬───────────────────────────────────┘
                           │
                           │ var newConfig = new ServerConfig
                           │ {
                           │     Debug = false,
                           │     HTTPPort = 8223
                           │ };
                           │
                           ▼
┌──────────────────────────────────────────────────────────────┐
│                  Configuration Object                         │
│                                                                │
│  ServerConfig {                                               │
│      Debug: false,                                            │
│      HTTPPort: 8223                                           │
│  }                                                             │
└──────────────────────────┬───────────────────────────────────┘
                           │
                           │ JsonSerializer.Serialize()
                           ▼
┌──────────────────────────────────────────────────────────────┐
│                      JSON String                              │
│                                                                │
│  {                                                            │
│    "debug": false,                                            │
│    "http_port": 8223                                          │
│  }                                                             │
└──────────────────────────┬───────────────────────────────────┘
                           │
                           │ P/Invoke → CGO
                           ▼
┌──────────────────────────────────────────────────────────────┐
│                   Go Bindings Layer                           │
│                                                                │
│  1. Parse JSON                                                │
│  2. Validate values                                           │
│  3. Apply defaults                                            │
│  4. Create server.Options                                     │
└──────────────────────────┬───────────────────────────────────┘
                           │
                           │ server.Options{
                           │     Debug: false,
                           │     HTTPPort: 8223
                           │ }
                           ▼
┌──────────────────────────────────────────────────────────────┐
│                    NATS Server                                │
│                                                                │
│  natsServer.ReloadOptions(opts)                              │
│                                                                │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  1. Validate new options                               │  │
│  │  2. Apply atomically                                   │  │
│  │  3. Update subsystems                                  │  │
│  │  4. Clients remain connected                           │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────┬───────────────────────────────────┘
                           │
                           │ Success/Error message
                           ▼
┌──────────────────────────────────────────────────────────────┐
│                    Application Layer                          │
│                                                                │
│  string result = "Configuration updated successfully"         │
└──────────────────────────────────────────────────────────────┘
```

---

## Hot Reload Process

### Zero-Downtime Configuration Update

```
Time: T0 - Running with Debug = true
┌────────────────────────────────────────────────────┐
│            NATS Server (Running)                    │
│                                                     │
│  Configuration:                                     │
│    Debug: true                                      │
│    HTTPPort: 8222                                   │
│                                                     │
│  Connected Clients: 10                              │
│  Active Subscriptions: 50                           │
└────────────────────────────────────────────────────┘
                      │
                      │ Application continues running
                      ▼
Time: T1 - UpdateConfig() called
┌────────────────────────────────────────────────────┐
│         Configuration Update Request                │
│                                                     │
│  server.UpdateConfig(new ServerConfig              │
│  {                                                  │
│      Debug = false,                                 │
│      HTTPPort = 8223                                │
│  });                                                │
└────────────────────────────────────────────────────┘
                      │
                      │ ~5-20ms processing time
                      ▼
Time: T2 - Configuration Applied (No Restart!)
┌────────────────────────────────────────────────────┐
│            NATS Server (Still Running)              │
│                                                     │
│  Configuration:                                     │
│    Debug: false      ← UPDATED                      │
│    HTTPPort: 8223    ← UPDATED                      │
│                                                     │
│  Connected Clients: 10  ← UNCHANGED                 │
│  Active Subscriptions: 50  ← UNCHANGED              │
│                                                     │
│  ✓ Zero downtime                                    │
│  ✓ No client disconnections                         │
│  ✓ No message loss                                  │
└────────────────────────────────────────────────────┘
                      │
                      │ Application continues running
                      ▼
Time: T3 onwards - New configuration active
```

---

## Deployment Patterns

### Pattern 1: Embedded Deployment

```
┌─────────────────────────────────────────────────────┐
│              Single Application Process              │
│                                                      │
│  ┌────────────────────────────────────────────────┐ │
│  │         ASP.NET Core Web API                   │ │
│  │                                                 │ │
│  │  Startup.cs:                                   │ │
│  │    services.AddSingleton<NatsServer>();        │ │
│  │                                                 │ │
│  │  Controllers use NATS for:                     │ │
│  │  • Internal messaging                          │ │
│  │  • Event publishing                            │ │
│  │  • Request/Reply patterns                      │ │
│  └────────┬───────────────────────────────────────┘ │
│           │                                          │
│           │ In-process communication                 │
│           ▼                                          │
│  ┌────────────────────────────────────────────────┐ │
│  │      Embedded NATS Server Instance             │ │
│  │                                                 │ │
│  │  • Runs in same process                        │ │
│  │  • No network overhead for internal calls      │ │
│  │  • Lifecycle tied to application               │ │
│  │  • Configuration managed by app                │ │
│  └────────────────────────────────────────────────┘ │
│                                                      │
│  Benefits:                                           │
│  ✓ Single deployment unit                           │
│  ✓ No external dependencies                         │
│  ✓ Simplified operations                            │
│  ✓ Perfect for microservices                        │
└─────────────────────────────────────────────────────┘
```

### Pattern 2: Sidecar Deployment

```
┌─────────────────────────────────────────────────┐
│              Kubernetes Pod                      │
│                                                  │
│  ┌────────────────────┐  ┌───────────────────┐ │
│  │  Main Container    │  │  Sidecar Container│ │
│  │                    │  │                   │ │
│  │  Python/Node.js    │  │  .NET + NATS      │ │
│  │  Application       │  │  MessageBroker    │ │
│  │                    │  │                   │ │
│  │  Connects to:      │  │  Manages:         │ │
│  │  localhost:4222 ───┼──┼─▶ NATS Server    │ │
│  │                    │  │                   │ │
│  └────────────────────┘  └───────────────────┘ │
│                                                  │
│  Benefits:                                       │
│  ✓ Language agnostic                             │
│  ✓ Shared NATS instance per pod                  │
│  ✓ Independent scaling                           │
└─────────────────────────────────────────────────┘
```

### Pattern 3: Hub-and-Spoke (Leaf Nodes)

```
                    ┌──────────────────────┐
                    │    Central Hub       │
                    │   NATS Cluster       │
                    │                      │
                    │  Production-grade    │
                    │  High availability   │
                    │  Multi-region        │
                    └──────────┬───────────┘
                               │
                 ┌─────────────┼─────────────┐
                 │             │             │
                 ▼             ▼             ▼
      ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
      │  Edge Node 1 │ │  Edge Node 2 │ │  Edge Node 3 │
      │              │ │              │ │              │
      │ MessageBroker│ │ MessageBroker│ │ MessageBroker│
      │ .NET Server  │ │ .NET Server  │ │ .NET Server  │
      │              │ │              │ │              │
      │ Leaf Node    │ │ Leaf Node    │ │ Leaf Node    │
      │ Config:      │ │ Config:      │ │ Config:      │
      │ RemoteURLs:  │ │ RemoteURLs:  │ │ RemoteURLs:  │
      │ [hub:7422]   │ │ [hub:7422]   │ │ [hub:7422]   │
      └──────────────┘ └──────────────┘ └──────────────┘
           │                 │                 │
           │                 │                 │
      ┌────┴────┐       ┌────┴────┐       ┌────┴────┐
      │ Local   │       │ Local   │       │ Local   │
      │ Clients │       │ Clients │       │ Clients │
      └─────────┘       └─────────┘       └─────────┘

Benefits:
✓ Local processing with cloud backup
✓ Reduced latency for edge clients
✓ Automatic failover to hub
✓ Centralized management
```

---

## Comparison Diagrams

### nats-csharp (Client Only) vs MessageBroker.NET (Server Control)

#### nats-csharp Architecture

```
┌──────────────────────────────────────────┐
│         C# Application                    │
│                                           │
│  using NATS.Client;                      │
│                                           │
│  var conn = new ConnectionFactory()      │
│      .CreateConnection(                  │
│          "nats://external-server:4222"); │
│                                           │
│  // Can only:                             │
│  // ✓ Connect to server                  │
│  // ✓ Publish/Subscribe                  │
│  // ✗ Control server                     │
│  // ✗ Configure server                   │
│  // ✗ Hot reload                          │
└────────────────┬─────────────────────────┘
                 │
                 │ TCP Connection
                 │ NATS Protocol
                 │
                 ▼
┌─────────────────────────────────────────┐
│     External NATS Server                 │
│     (Must be pre-installed)              │
│                                          │
│  • Managed separately (docker/systemd)  │
│  • Configuration via .conf files         │
│  • Requires restart for changes          │
│  • Ops team responsibility               │
└─────────────────────────────────────────┘

Deployment Steps:
1. Install NATS server separately
2. Configure via config file
3. Start NATS server
4. Start C# application
5. Application connects to server

Configuration Changes:
1. Stop NATS server
2. Edit config file
3. Restart NATS server
4. Clients reconnect
   ❌ DOWNTIME REQUIRED
```

#### MessageBroker.NET Architecture

```
┌─────────────────────────────────────────────────────┐
│            C# Application Process                    │
│                                                      │
│  using NatsSharp;                                   │
│                                                      │
│  var server = new NatsServer();                     │
│  server.Start(new ServerConfig { ... });            │
│                                                      │
│  // Can do everything:                               │
│  // ✓ Start/Stop server                             │
│  // ✓ Configure server                              │
│  // ✓ Hot reload (zero downtime)                    │
│  // ✓ Create accounts                               │
│  // ✓ Monitor server                                │
│  ├─────────────────────────────────────────────────┤
│  │      Embedded NATS Server                        │
│  │      (No external dependency)                    │
│  │                                                   │
│  │  • Managed programmatically                      │
│  │  • Configuration via objects                     │
│  │  • Hot reload support                            │
│  │  • Application responsibility                    │
│  └─────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────┘

Deployment Steps:
1. Deploy C# application (includes NATS)
   ✅ SINGLE STEP

Configuration Changes:
1. server.UpdateConfig(newConfig);
   ✅ ZERO DOWNTIME
   ✅ NO CLIENT DISCONNECTIONS
```

### Side-by-Side Feature Comparison

```
┌─────────────────────────┬────────────────┬──────────────────┐
│ Capability              │ nats-csharp    │ MessageBroker.NET│
├─────────────────────────┼────────────────┼──────────────────┤
│ Start Server            │       ✗        │        ✓         │
├─────────────────────────┼────────────────┼──────────────────┤
│ Stop Server             │       ✗        │        ✓         │
├─────────────────────────┼────────────────┼──────────────────┤
│ Configure Server        │       ✗        │        ✓         │
├─────────────────────────┼────────────────┼──────────────────┤
│ Hot Reload Config       │       ✗        │      ✓✓✓        │
├─────────────────────────┼────────────────┼──────────────────┤
│ Create Accounts         │       ✗        │        ✓         │
├─────────────────────────┼────────────────┼──────────────────┤
│ Monitor Server          │       ✗        │        ✓         │
├─────────────────────────┼────────────────┼──────────────────┤
│ Pub/Sub Messages        │       ✓        │   ✓ (via client) │
├─────────────────────────┼────────────────┼──────────────────┤
│ JetStream Client        │       ✓        │   ✓ (via client) │
├─────────────────────────┼────────────────┼──────────────────┤
│ External Dependency     │    Required    │       None       │
├─────────────────────────┼────────────────┼──────────────────┤
│ Deployment Complexity   │      High      │       Low        │
├─────────────────────────┼────────────────┼──────────────────┤
│ Configuration Changes   │   Requires     │   Zero Downtime  │
│                         │   Restart      │   Hot Reload     │
└─────────────────────────┴────────────────┴──────────────────┘

Legend:
✗ = Not supported
✓ = Supported
✓✓✓ = Key differentiator / Special capability
```

---

## Data Flow Diagrams

### Publish/Subscribe Flow

```
Application Code
      │
      │ 1. Start server
      │    server.Start()
      ▼
MessageBroker.NET
      │
      │ 2. Server running
      ▼
NATS Server (embedded)
      │
      │ 3. Client connects
      ▼
┌─────────────────────┐
│  NATS Client        │
│  (nats-csharp)      │
│                     │
│  conn.Subscribe(    │
│    "orders.*",      │
│    handler          │
│  )                  │
└──────┬──────────────┘
       │
       │ 4. Subscribe
       ▼
NATS Server (embedded)
       │
       │ 5. Store subscription
       │
       │ 6. Another client publishes
       ▼
┌─────────────────────┐
│  Publisher Client   │
│                     │
│  conn.Publish(      │
│    "orders.new",    │
│    data             │
│  )                  │
└──────┬──────────────┘
       │
       │ 7. Publish message
       ▼
NATS Server (embedded)
       │
       │ 8. Route to subscribers
       ▼
┌─────────────────────┐
│  NATS Client        │
│                     │
│  handler called     │
│  with message       │
└─────────────────────┘
```

---

## Summary

These diagrams illustrate:

1. **Layered Architecture**: Clear separation between C#, P/Invoke, CGO, and NATS server
2. **Hot Reload Flow**: Zero-downtime configuration updates
3. **Deployment Flexibility**: Embedded, sidecar, and hub-and-spoke patterns
4. **Key Differentiator**: Server control vs client-only (nats-csharp)

For more details, see:
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Complete architecture documentation
- [API_DESIGN.md](./API_DESIGN.md) - API reference with code examples
- [GETTING_STARTED.md](./GETTING_STARTED.md) - Quick start guide
