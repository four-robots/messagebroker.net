using MessageBroker.Core.Configuration;

namespace MessageBroker.Core.Interfaces;

/// <summary>
/// Defines the contract for persisting and retrieving configuration versions.
/// Implementations can store configurations in memory, databases, or file systems.
/// </summary>
public interface IConfigurationStore
{
    /// <summary>
    /// Saves a configuration version to the store.
    /// </summary>
    /// <param name="version">The configuration version to save.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveAsync(ConfigurationVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific configuration version by its version number.
    /// </summary>
    /// <param name="versionNumber">The version number to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The configuration version if found; otherwise, null.</returns>
    Task<ConfigurationVersion?> GetVersionAsync(int versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent configuration version from the store.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The latest configuration version if any exist; otherwise, null.</returns>
    Task<ConfigurationVersion?> GetLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of recent configuration versions in descending order (newest first).
    /// </summary>
    /// <param name="count">The maximum number of versions to retrieve. Default is 10.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of configuration versions.</returns>
    Task<IReadOnlyList<ConfigurationVersion>> GetHistoryAsync(int count = 10, CancellationToken cancellationToken = default);
}
