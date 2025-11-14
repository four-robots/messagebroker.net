namespace MessageBroker.Core.Configuration;

/// <summary>
/// Represents the result of a configuration operation.
/// Contains information about success, errors, and the resulting configuration state.
/// </summary>
public class ConfigurationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// Null if the operation was successful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the configuration version that resulted from the operation.
    /// Null if the operation failed or did not produce a new version.
    /// </summary>
    public ConfigurationVersion? Version { get; set; }

    /// <summary>
    /// Gets or sets the differences between the old and new configuration.
    /// Null if the operation failed or there were no changes.
    /// </summary>
    public ConfigurationDiff? Diff { get; set; }

    /// <summary>
    /// Creates a successful result with the specified version and diff.
    /// </summary>
    /// <param name="version">The configuration version that was applied.</param>
    /// <param name="diff">The differences between configurations.</param>
    /// <returns>A successful ConfigurationResult.</returns>
    public static ConfigurationResult Succeeded(ConfigurationVersion version, ConfigurationDiff? diff = null)
    {
        return new ConfigurationResult
        {
            Success = true,
            Version = version,
            Diff = diff
        };
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the operation failed.</param>
    /// <returns>A failed ConfigurationResult.</returns>
    public static ConfigurationResult Failed(string errorMessage)
    {
        return new ConfigurationResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
