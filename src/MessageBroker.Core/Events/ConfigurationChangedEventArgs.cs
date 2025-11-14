using MessageBroker.Core.Configuration;

namespace MessageBroker.Core.Events;

/// <summary>
/// Event arguments for when a broker configuration has been successfully changed.
/// Contains information about the old and new configurations and what changed.
/// </summary>
public class ConfigurationChangedEventArgs : BrokerEventArgs
{
    /// <summary>
    /// Gets or sets the previous configuration version before the change.
    /// </summary>
    public ConfigurationVersion OldVersion { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new configuration version after the change.
    /// </summary>
    public ConfigurationVersion NewVersion { get; set; } = null!;

    /// <summary>
    /// Gets or sets the detailed differences between the old and new configurations.
    /// </summary>
    public ConfigurationDiff Diff { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp when the configuration was changed.
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Initializes a new instance of the ConfigurationChangedEventArgs class.
    /// </summary>
    public ConfigurationChangedEventArgs()
    {
        Timestamp = ChangedAt;
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationChangedEventArgs class with the specified versions and diff.
    /// </summary>
    /// <param name="oldVersion">The previous configuration version.</param>
    /// <param name="newVersion">The new configuration version.</param>
    /// <param name="diff">The differences between the configurations.</param>
    public ConfigurationChangedEventArgs(
        ConfigurationVersion oldVersion,
        ConfigurationVersion newVersion,
        ConfigurationDiff diff)
    {
        OldVersion = oldVersion;
        NewVersion = newVersion;
        Diff = diff;
        ChangedAt = DateTimeOffset.UtcNow;
        Timestamp = ChangedAt;
    }
}
