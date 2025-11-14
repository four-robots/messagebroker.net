using MessageBroker.Core.Configuration;

namespace MessageBroker.Core.Events;

/// <summary>
/// Event arguments for when a broker configuration is about to be changed.
/// This event is raised before the change is applied, allowing subscribers to cancel the operation.
/// </summary>
public class ConfigurationChangingEventArgs : BrokerEventArgs
{
    /// <summary>
    /// Gets or sets the current configuration before the change.
    /// </summary>
    public BrokerConfiguration Current { get; set; } = null!;

    /// <summary>
    /// Gets or sets the proposed new configuration.
    /// </summary>
    public BrokerConfiguration Proposed { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the configuration change should be canceled.
    /// Set this to true in an event handler to prevent the configuration from being applied.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Gets or sets the detailed differences between the current and proposed configurations.
    /// </summary>
    public ConfigurationDiff Diff { get; set; } = null!;

    /// <summary>
    /// Gets or sets the reason for cancellation if Cancel is set to true.
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Initializes a new instance of the ConfigurationChangingEventArgs class.
    /// </summary>
    public ConfigurationChangingEventArgs()
    {
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationChangingEventArgs class with the specified configurations and diff.
    /// </summary>
    /// <param name="current">The current configuration.</param>
    /// <param name="proposed">The proposed new configuration.</param>
    /// <param name="diff">The differences between the configurations.</param>
    public ConfigurationChangingEventArgs(
        BrokerConfiguration current,
        BrokerConfiguration proposed,
        ConfigurationDiff diff)
    {
        Current = current;
        Proposed = proposed;
        Diff = diff;
    }

    /// <summary>
    /// Cancels the configuration change with the specified reason.
    /// </summary>
    /// <param name="reason">The reason for canceling the change.</param>
    public void CancelChange(string reason)
    {
        Cancel = true;
        CancellationReason = reason;
        Message = $"Configuration change canceled: {reason}";
    }
}
