# MessageBroker.NET Documentation - Table of Contents

Complete documentation for MessageBroker.NET v1.0.0

**Total**: 7 files | 5,312 lines | 192 KB

---

## Documentation Files

1. **[INDEX.md](./INDEX.md)** - Documentation index and navigation guide
2. **[README.md](./README.md)** - Overview and introduction
3. **[GETTING_STARTED.md](./GETTING_STARTED.md)** - Installation and first steps
4. **[API_DESIGN.md](./API_DESIGN.md)** - Complete API reference
5. **[ARCHITECTURE.md](./ARCHITECTURE.md)** - System architecture
6. **[DIAGRAMS.md](./DIAGRAMS.md)** - Visual architecture diagrams
7. **[QUICK_REFERENCE.md](./QUICK_REFERENCE.md)** - Quick reference guide

---

## Detailed Table of Contents

### INDEX.md
- Documentation Statistics
- Quick Navigation
- Documentation Files Overview
- Documentation by Task
- Documentation by Audience
- Key Concepts Cross-Reference
- Code Examples Count
- Documentation Completeness

### README.md
- Documentation Overview
- Quick Links
- Key Features
- Why MessageBroker.NET?
- 30-Second Quick Start
- Common Use Cases
- Platform Support
- Performance Characteristics
- Security Considerations
- Deployment Patterns
- Monitoring and Observability
- Troubleshooting Quick Reference
- Support and Community

### GETTING_STARTED.md
- Prerequisites
- Installation (NuGet and from source)
- Verify Installation
- Quick Start
- Common Scenarios:
  - Scenario 1: Basic Messaging Server
  - Scenario 2: JetStream Event Streaming
  - Scenario 3: Secure Server with Authentication
  - Scenario 4: Server with Monitoring
  - Scenario 5: Hot Configuration Reload
  - Scenario 6: Multi-Tenant Server
- Configuration Guide
- Best Practices
- Troubleshooting (6 common issues)
- Next Steps

### API_DESIGN.md
- Overview
- Core Concepts
- Basic Server Operations
  - Simple Server Startup
  - JetStream-Enabled Server
  - Server with Authentication
  - Server with HTTP Monitoring
- Runtime Configuration Management
  - Hot Configuration Reload
  - Configuration Validation
  - Monitoring Configuration Changes
- Advanced Features
  - Multi-Account Management
  - JWT-Based Account Creation
  - Leaf Node Configuration
  - Complete Production Example
- Comparison with nats-csharp
  - Standard nats-csharp
  - MessageBroker.NET
  - Side-by-Side Feature Comparison
  - Usage Scenario Comparison
- Complete API Reference
  - NatsServer Class
  - Configuration Classes
  - Information Classes
- Best Practices

### ARCHITECTURE.md
- System Architecture
  - High-Level Architecture Diagram
- Component Overview
  - C# Application Layer
  - Platform Bindings Layer
  - Go Bindings Layer (CGO)
  - NATS Server Layer
- Interprocess Communication
  - Data Flow: Configuration Update
  - Memory Management
- Configuration Management
  - Configuration Pipeline
  - Configuration Versioning
  - Change Notification System
- Comparison with nats-csharp
  - Architectural Differences
  - Feature Matrix
- Performance Characteristics
  - Startup Performance
  - Configuration Reload Performance
  - Memory Overhead
  - Message Throughput
- Deployment Architecture
  - Embedded Deployment
  - Distributed Deployment
  - Kubernetes Deployment

### DIAGRAMS.md
- System Architecture
  - Full Stack Architecture
- Component Interaction
  - Method Call Flow: Start Server
- Configuration Flow
  - Configuration Update Flow
- Hot Reload Process
  - Zero-Downtime Configuration Update
- Deployment Patterns
  - Pattern 1: Embedded Deployment
  - Pattern 2: Sidecar Deployment
  - Pattern 3: Hub-and-Spoke (Leaf Nodes)
- Comparison Diagrams
  - nats-csharp vs MessageBroker.NET
  - Side-by-Side Feature Comparison
- Data Flow Diagrams
  - Publish/Subscribe Flow

### QUICK_REFERENCE.md
- Installation
- 30-Second Quick Start
- Common Patterns
- Configuration Reference
- API Methods
- HTTP Monitoring Endpoints
- Environment Variables Pattern
- Error Handling
- Testing Pattern
- Production Configuration Template
- Common Issues
- Performance Tips
- Docker Deployment
- Kubernetes ConfigMap
- Health Check Implementation
- Graceful Shutdown Pattern
- Metrics Collection Example

---

## Reading Paths

### Path 1: New User (1.5 hours)
1. README.md (10 min)
2. GETTING_STARTED.md (30 min)
3. API_DESIGN.md (30 min)
4. Bookmark QUICK_REFERENCE.md

### Path 2: Experienced Developer (35 minutes)
1. API_DESIGN.md (20 min)
2. ARCHITECTURE.md (15 min)
3. Bookmark QUICK_REFERENCE.md

### Path 3: Architect/DevOps (1 hour)
1. ARCHITECTURE.md (30 min)
2. DIAGRAMS.md (15 min)
3. API_DESIGN.md (15 min)
4. README.md (10 min)

---

## Quick Access by Topic

### Installation
- GETTING_STARTED.md - Installation section
- QUICK_REFERENCE.md - Installation one-liner

### API Reference
- API_DESIGN.md - Complete API Reference
- QUICK_REFERENCE.md - API Methods

### Code Examples
- API_DESIGN.md - 50+ examples
- GETTING_STARTED.md - 20+ scenarios
- QUICK_REFERENCE.md - 30+ patterns

### Architecture
- ARCHITECTURE.md - Full architecture
- DIAGRAMS.md - Visual diagrams

### Deployment
- README.md - Deployment Patterns
- ARCHITECTURE.md - Deployment Architecture
- QUICK_REFERENCE.md - Docker/Kubernetes

### Troubleshooting
- GETTING_STARTED.md - Detailed troubleshooting
- QUICK_REFERENCE.md - Quick fixes

### Comparison with nats-csharp
- API_DESIGN.md - Feature comparison
- ARCHITECTURE.md - Architectural differences
- DIAGRAMS.md - Visual comparison

---

## Print-Friendly Documentation

To print or export documentation:

```bash
# Combine all documentation into single file
cat INDEX.md README.md GETTING_STARTED.md API_DESIGN.md \
    ARCHITECTURE.md DIAGRAMS.md QUICK_REFERENCE.md > \
    MessageBroker.NET-Complete-Documentation.md

# Or convert to PDF (requires pandoc)
pandoc INDEX.md README.md GETTING_STARTED.md API_DESIGN.md \
       ARCHITECTURE.md DIAGRAMS.md QUICK_REFERENCE.md \
       -o MessageBroker.NET-Documentation.pdf
```

---

## Documentation Standards

All documentation follows:
- **Markdown format**: Compatible with GitHub, static site generators
- **Code examples**: Complete, runnable, tested
- **Diagrams**: ASCII art for universal compatibility
- **Links**: Relative links between documents
- **Accessibility**: Clear headings, table of contents

---

## Maintenance

Documentation is:
- **Version controlled**: Tracked in Git
- **Reviewed**: All changes reviewed for accuracy
- **Updated**: Kept in sync with code changes
- **Tested**: Code examples verified to work

---

## Contributing

To contribute to documentation:

1. Fork the repository
2. Create a branch for documentation changes
3. Follow existing style and format
4. Include runnable code examples
5. Test all examples
6. Submit pull request

---

## Support

For documentation issues:
- **GitHub Issues**: Report documentation bugs
- **GitHub Discussions**: Ask questions
- **Pull Requests**: Suggest improvements

---

**Documentation Version**: 1.0.0  
**Last Updated**: November 2024  
**License**: Apache 2.0  
