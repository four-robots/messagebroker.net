namespace MessageBroker.Core.Events;

/// <summary>
/// Base class for broker-related event arguments.
/// Provides common properties for all broker events.
/// </summary>
public class BrokerEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets an optional message providing additional context about the event.
    /// </summary>
    public string? Message { get; set; }
}
