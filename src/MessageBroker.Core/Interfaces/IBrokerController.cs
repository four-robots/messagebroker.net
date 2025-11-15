using MessageBroker.Core.Configuration;
using MessageBroker.Core.Events;

namespace MessageBroker.Core.Interfaces;

/// <summary>
/// Defines the contract for controlling and managing a message broker instance.
/// Provides methods for configuration management, monitoring, and lifecycle control.
/// </summary>
public interface IBrokerController
{
    /// <summary>
    /// Gets the current active configuration of the broker.
    /// </summary>
    BrokerConfiguration CurrentConfiguration { get; }

    /// <summary>
    /// Applies a new configuration to the broker.
    /// </summary>
    /// <param name="config">The new configuration to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure, including any error details and the new configuration version.</returns>
    Task<ConfigurationResult> ConfigureAsync(BrokerConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies incremental changes to the current configuration using a configuration action.
    /// </summary>
    /// <param name="configure">An action that modifies the current configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure, including any error details and the new configuration version.</returns>
    Task<ConfigurationResult> ApplyChangesAsync(Action<BrokerConfiguration> configure, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the configuration to a previous version.
    /// </summary>
    /// <param name="toVersion">The version number to roll back to. If null, rolls back to the previous version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure, including any error details and the restored configuration version.</returns>
    Task<ConfigurationResult> RollbackAsync(int? toVersion = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves detailed information about the current state of the broker.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the broker including connection details, statistics, and current configuration.</returns>
    Task<BrokerInfo> GetInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enters lame duck mode, which stops accepting new connections while allowing existing connections to drain gracefully.
    /// This is useful for performing zero-downtime deployments and graceful shutdowns.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when lame duck mode has been entered.</returns>
    Task EnterLameDuckModeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully shuts down the broker instance.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous shutdown operation.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when the broker configuration has been successfully changed.
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Occurs before a configuration change is applied, allowing subscribers to cancel the change.
    /// </summary>
    event EventHandler<ConfigurationChangingEventArgs>? ConfigurationChanging;
}
