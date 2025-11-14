# MessageBroker.Core

This is the core library for MessageBroker.NET, containing the fundamental interfaces, abstractions, and configuration models that form the foundation of the message broker system.

## Purpose

MessageBroker.Core provides:

- **Interfaces**: Core abstractions for message broker functionality, including publishers, subscribers, and connection management
- **Configuration**: Models for configuring message broker connections, serialization, and behavior
- **Validation**: Input validation and configuration validation logic
- **Events**: Event models and event handling abstractions

## Dependencies

This project has no external dependencies except for .NET 9.0 System libraries. This ensures maximum portability and minimal dependency conflicts when used in other projects.

## Project Structure

```
MessageBroker.Core/
├── Interfaces/       # Core interfaces and abstractions
├── Configuration/    # Configuration models and options
├── Validation/       # Validation logic and attributes
└── Events/          # Event definitions and handlers
```

## Usage

This library is intended to be referenced by:
- Implementation libraries (e.g., MessageBroker.Nats)
- Consumer applications that need to work with multiple message broker implementations

Do not instantiate classes directly from this library in production code. Instead, use the implementation-specific libraries that provide concrete implementations of these interfaces.
