# MessageBroker.Nats

This is the NATS-specific implementation library for MessageBroker.NET, providing concrete implementations of the MessageBroker.Core interfaces using NATS as the underlying message broker.

## Purpose

MessageBroker.Nats provides:

- **Bindings**: P/Invoke bindings and native interop code for the NATS C library
- **Implementation**: Concrete implementations of MessageBroker.Core interfaces that use NATS for message transport

## Dependencies

- **MessageBroker.Core**: Reference to the core abstractions and interfaces
- **NATS C Library**: Native library bindings for high-performance NATS messaging (P/Invoke based)

## Project Structure

```
MessageBroker.Nats/
├── Bindings/        # P/Invoke declarations and native interop code
└── Implementation/  # Concrete NATS implementations of Core interfaces
```

## Usage

Add a reference to this library in your application to use NATS as your message broker implementation. The library provides dependency injection extensions to register NATS services with your application's service container.

## Performance

This implementation uses P/Invoke bindings to the native NATS C library for maximum performance. The bindings are based on the nats-csharp library and provide zero-copy, low-latency message transport.

## Configuration

Configuration is managed through the MessageBroker.Core configuration models. NATS-specific options include:
- Server URLs
- Connection timeouts
- Reconnection policies
- TLS/authentication settings
