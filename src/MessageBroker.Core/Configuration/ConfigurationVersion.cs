namespace MessageBroker.Core.Configuration;

/// <summary>
/// Represents a versioned snapshot of a broker configuration.
/// Each configuration change creates a new version for tracking and rollback purposes.
/// </summary>
public class ConfigurationVersion
{
    /// <summary>
    /// Gets or sets the version number. Version numbers are sequential starting from 1.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the configuration snapshot for this version.
    /// </summary>
    public BrokerConfiguration Configuration { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp when this version was applied to the broker.
    /// </summary>
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user or system that applied this version.
    /// Can be null if the source is unknown or not tracked.
    /// </summary>
    public string? AppliedBy { get; set; }

    /// <summary>
    /// Gets or sets the type of change that created this version.
    /// </summary>
    public ConfigurationChangeType ChangeType { get; set; } = ConfigurationChangeType.Update;
}

/// <summary>
/// Specifies the type of configuration change that occurred.
/// </summary>
public enum ConfigurationChangeType
{
    /// <summary>
    /// The initial configuration when the broker was first started.
    /// </summary>
    Initial,

    /// <summary>
    /// A normal configuration update.
    /// </summary>
    Update,

    /// <summary>
    /// A rollback to a previous configuration version.
    /// </summary>
    Rollback
}
