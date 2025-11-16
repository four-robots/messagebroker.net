# DotGnatly Documentation Index

Complete documentation for DotGnatly - a .NET library providing full control over NATS servers with runtime reconfiguration capabilities.

## Documentation Overview

**Total Files**: 6 core documentation files
**Total Coverage**: Installation, API reference, architecture, monitoring, quick reference
**Version**: 1.0.0

---

## Core Documentation Files

### 1. [README.md](./README.md) - Start Here
**Purpose**: Documentation overview and quick introduction

- What is DotGnatly
- Key features and benefits
- 30-second quick start
- Quick links to other docs

**Audience**: Everyone
**Time**: 10 minutes

---

### 2. [GETTING_STARTED.md](./GETTING_STARTED.md) - Installation & First Steps
**Purpose**: Installation guide with hands-on examples

- Prerequisites and installation
- Your first NATS server
- 6 complete scenarios (messaging, JetStream, auth, monitoring, hot reload, multi-tenant)
- Configuration guide
- Best practices
- Troubleshooting

**Audience**: New users
**Time**: 30-45 minutes

---

### 3. [API_DESIGN.md](./API_DESIGN.md) - Complete API Reference
**Purpose**: Full API documentation with examples

- Core concepts
- Basic and advanced server operations
- Runtime configuration management
- Comparison with nats-csharp
- Complete API reference for all classes
- Best practices

**Audience**: Developers implementing features
**Time**: 30-45 minutes
**Examples**: 50+ complete, runnable code examples

---

### 4. [ARCHITECTURE.md](./ARCHITECTURE.md) - System Design (Advanced)
**Purpose**: Deep dive into system architecture and internals

- System architecture with diagrams
- Component overview (4 layers: C#, Bindings, Go, NATS)
- Interprocess communication (P/Invoke, CGO)
- Configuration management and versioning
- Performance characteristics
- Deployment patterns

**Audience**: Architects, senior developers, DevOps
**Time**: 30-60 minutes

---

### 5. [DEV_MONITORING.md](./DEV_MONITORING.md) - Monitoring Guide (Developer Reference)
**Purpose**: Comprehensive monitoring and observability

⚠️ **Developer Reference**: Technical implementation details

- Monitoring endpoints (Connz, Subsz, Jsz, Routez, Leafz)
- Connection management
- Usage examples
- Integration with metrics systems

**Audience**: Platform engineers, DevOps
**Time**: 20-30 minutes

---

### 6. [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - Cheat Sheet
**Purpose**: Quick syntax and pattern reference

- Common code patterns
- Configuration reference
- API methods
- Error handling
- Production templates
- Docker/Kubernetes deployment
- Quick troubleshooting

**Audience**: Everyone (bookmark for quick lookup)
**Time**: Use as needed

---

## Recommended Reading Paths

### Path 1: New User (1.5 hours)
1. **README.md** (10 min) - Understand what DotGnatly is
2. **GETTING_STARTED.md** (30 min) - Install and run your first server
3. **API_DESIGN.md** (30 min) - Learn the complete API
4. **Bookmark QUICK_REFERENCE.md** - For quick lookups

**Result**: Ready to build with DotGnatly

---

### Path 2: Experienced Developer (35 minutes)
1. **API_DESIGN.md** (20 min) - Jump to code examples
2. **ARCHITECTURE.md** (15 min) - Understand internals
3. **Bookmark QUICK_REFERENCE.md** - For syntax lookups

**Result**: Deep understanding and ready to implement

---

### Path 3: Architect/DevOps (1 hour)
1. **ARCHITECTURE.md** (30 min) - System design and deployment
2. **API_DESIGN.md** (15 min) - Capabilities and limitations
3. **DEV_MONITORING.md** (15 min) - Monitoring and observability
4. **README.md** (10 min) - Platform support and security

**Result**: Ready for production deployment planning

---

## Quick Access by Task

### I want to...

#### Install and run my first server
→ [GETTING_STARTED.md](./GETTING_STARTED.md) - Installation section
→ Time: 5 minutes

#### See code examples
→ [API_DESIGN.md](./API_DESIGN.md) - All examples
→ [GETTING_STARTED.md](./GETTING_STARTED.md) - Scenario examples
→ [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - Quick patterns

#### Enable JetStream persistence
→ [GETTING_STARTED.md](./GETTING_STARTED.md) - Scenario 2
→ [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - JetStream pattern

#### Implement hot configuration reload
→ [API_DESIGN.md](./API_DESIGN.md) - Runtime Configuration section
→ [GETTING_STARTED.md](./GETTING_STARTED.md) - Scenario 5

#### Set up authentication
→ [GETTING_STARTED.md](./GETTING_STARTED.md) - Scenario 3
→ [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - Authentication pattern

#### Understand the architecture
→ [ARCHITECTURE.md](./ARCHITECTURE.md) - Complete architecture
→ Time: 30-45 minutes

#### Monitor server health
→ [DEV_MONITORING.md](./DEV_MONITORING.md) - Monitoring guide
→ [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - Health check pattern

#### Deploy to production
→ [ARCHITECTURE.md](./ARCHITECTURE.md) - Deployment patterns
→ [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - Production template

#### Troubleshoot an issue
→ [GETTING_STARTED.md](./GETTING_STARTED.md) - Troubleshooting section
→ [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - Common issues

#### Compare with nats-csharp
→ [API_DESIGN.md](./API_DESIGN.md) - Comparison section
→ [ARCHITECTURE.md](./ARCHITECTURE.md) - Architectural differences

---

## Key Topics Cross-Reference

### Runtime Reconfiguration (Hot Reload)
- [API_DESIGN.md](./API_DESIGN.md) - Runtime Configuration Management
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Configuration Management section
- [GETTING_STARTED.md](./GETTING_STARTED.md) - Scenario 5

### JetStream Persistence
- [GETTING_STARTED.md](./GETTING_STARTED.md) - Scenario 2
- [API_DESIGN.md](./API_DESIGN.md) - JetStream-Enabled Server
- [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - JetStream pattern
- [DEV_MONITORING.md](./DEV_MONITORING.md) - JetStream Monitoring

### Multi-Tenancy & Accounts
- [GETTING_STARTED.md](./GETTING_STARTED.md) - Scenario 6
- [API_DESIGN.md](./API_DESIGN.md) - Multi-Account Management
- [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - Multi-tenant pattern

### Deployment Patterns
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Deployment Architecture section
- [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - Docker/Kubernetes examples

### Monitoring & Observability
- [DEV_MONITORING.md](./DEV_MONITORING.md) - Complete monitoring guide
- [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) - Metrics collection

---

## Documentation Statistics

| Document | Lines | Examples | Type |
|----------|-------|----------|------|
| API_DESIGN.md | ~1,070 | 50+ | API reference with examples |
| GETTING_STARTED.md | ~1,000 | 20+ | Tutorial/scenarios |
| ARCHITECTURE.md | ~1,500 | 15+ | Technical architecture |
| QUICK_REFERENCE.md | ~570 | 30+ | Quick reference patterns |
| DEV_MONITORING.md | ~650 | 10+ | Monitoring reference |
| README.md | ~100 | - | Overview |

**Total**: ~5,000 lines | 125+ code examples

---

## External Resources

### NATS Documentation
- **NATS Docs**: [docs.nats.io](https://docs.nats.io)
- **NATS Server**: [github.com/nats-io/nats-server](https://github.com/nats-io/nats-server)
- **NATS Protocol**: [NATS Protocol Reference](https://docs.nats.io/reference/reference-protocols/nats-protocol)

### Community
- **GitHub Issues**: Report bugs and request features
- **GitHub Discussions**: Ask questions and share ideas
- **NATS Slack**: [slack.nats.io](https://slack.nats.io)

---

## Contributing to Documentation

Documentation contributions welcome! Guidelines:

1. **Clarity**: Write for developers of all levels
2. **Examples**: Include complete, runnable code
3. **Accuracy**: Test all examples
4. **Consistency**: Follow existing style and format

---

## Version Information

**Documentation Version**: 1.0.0
**Last Updated**: November 2024
**Covers**: DotGnatly v1.0.0
**NATS Server Version**: 2.10.x - 2.11.0

---

## Quick Start (30 seconds)

New to DotGnatly? Start here:

```csharp
using DotGnatly.Nats;

// Create and start NATS server
using var controller = new NatsController();
var config = new BrokerConfiguration { Port = 4222 };
await controller.ConfigureAsync(config);

Console.WriteLine("Server running at nats://localhost:4222");
Console.ReadKey();
```

Then read [GETTING_STARTED.md](./GETTING_STARTED.md) for detailed tutorials.

---

Happy coding with DotGnatly! 🚀
