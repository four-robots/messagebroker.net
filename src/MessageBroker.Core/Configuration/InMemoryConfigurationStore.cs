using System.Collections.Concurrent;
using MessageBroker.Core.Interfaces;

namespace MessageBroker.Core.Configuration;

/// <summary>
/// In-memory implementation of configuration version storage.
/// Stores configuration versions in memory with thread-safe operations.
/// Suitable for testing and single-instance scenarios where persistence is not required.
/// </summary>
public class InMemoryConfigurationStore : IConfigurationStore
{
    private readonly ConcurrentDictionary<int, ConfigurationVersion> _versions = new();
    private int _nextVersionNumber = 1;
    private readonly object _versionLock = new();

    /// <summary>
    /// Saves a configuration version to the in-memory store.
    /// If the version number is 0 or not set, a new version number will be assigned.
    /// </summary>
    /// <param name="version">The configuration version to save.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public Task SaveAsync(ConfigurationVersion version, CancellationToken cancellationToken = default)
    {
        if (version == null)
        {
            throw new ArgumentNullException(nameof(version));
        }

        lock (_versionLock)
        {
            // Assign version number if not set
            if (version.Version <= 0)
            {
                version.Version = _nextVersionNumber++;
            }
            else
            {
                // Ensure next version number is higher than any manually set version
                if (version.Version >= _nextVersionNumber)
                {
                    _nextVersionNumber = version.Version + 1;
                }
            }

            _versions[version.Version] = version;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves a specific configuration version by its version number.
    /// </summary>
    /// <param name="versionNumber">The version number to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The configuration version if found; otherwise, null.</returns>
    public Task<ConfigurationVersion?> GetVersionAsync(int versionNumber, CancellationToken cancellationToken = default)
    {
        _versions.TryGetValue(versionNumber, out var version);
        return Task.FromResult(version);
    }

    /// <summary>
    /// Retrieves the most recent configuration version from the store.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The latest configuration version if any exist; otherwise, null.</returns>
    public Task<ConfigurationVersion?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        if (_versions.IsEmpty)
        {
            return Task.FromResult<ConfigurationVersion?>(null);
        }

        var latestVersion = _versions.Keys.Max();
        _versions.TryGetValue(latestVersion, out var version);
        return Task.FromResult(version);
    }

    /// <summary>
    /// Retrieves a list of recent configuration versions in descending order (newest first).
    /// </summary>
    /// <param name="count">The maximum number of versions to retrieve. Default is 10.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of configuration versions.</returns>
    public Task<IReadOnlyList<ConfigurationVersion>> GetHistoryAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var history = _versions.Values
            .OrderByDescending(v => v.Version)
            .Take(count)
            .ToList();

        return Task.FromResult<IReadOnlyList<ConfigurationVersion>>(history);
    }

    /// <summary>
    /// Gets the total number of versions stored.
    /// </summary>
    public int Count => _versions.Count;

    /// <summary>
    /// Clears all stored versions. Useful for testing.
    /// </summary>
    public void Clear()
    {
        lock (_versionLock)
        {
            _versions.Clear();
            _nextVersionNumber = 1;
        }
    }

    /// <summary>
    /// Gets all stored versions as a read-only list.
    /// </summary>
    /// <returns>A list of all configuration versions ordered by version number.</returns>
    public IReadOnlyList<ConfigurationVersion> GetAll()
    {
        return _versions.Values
            .OrderBy(v => v.Version)
            .ToList();
    }
}
