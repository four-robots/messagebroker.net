using System.Runtime.InteropServices;
using System.Text.Json;
using MessageBroker.Core.Configuration;
using MessageBroker.Core.Events;
using MessageBroker.Core.Interfaces;
using MessageBroker.Core.Validation;
using MessageBroker.Nats.Bindings;

namespace MessageBroker.Nats.Implementation;

/// <summary>
/// Main implementation of the broker controller that bridges Core abstractions with NATS native bindings.
/// Provides enhanced runtime reconfiguration with validation, versioning, and change notifications.
/// </summary>
public class NatsController : IBrokerController, IDisposable
{
    private readonly IConfigurationValidator _validator;
    private readonly IConfigurationStore _store;
    private readonly INatsBindings _bindings;
    private readonly object _lock = new();
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);
    private bool _disposed;

    private BrokerConfiguration? _currentConfiguration;
    private int _currentVersionNumber;

    /// <summary>
    /// Initializes a new instance of the NatsController class.
    /// </summary>
    /// <param name="validator">The configuration validator to use. If null, uses ConfigurationValidator from Core.</param>
    /// <param name="store">The configuration store to use. If null, uses InMemoryConfigurationStore from Core.</param>
    public NatsController(
        IConfigurationValidator? validator = null,
        IConfigurationStore? store = null)
    {
        _validator = validator ?? new ConfigurationValidator();
        _store = store ?? new InMemoryConfigurationStore();
        _bindings = NatsBindingsFactory.Create();
    }

    /// <summary>
    /// Gets the current active configuration of the broker.
    /// </summary>
    public BrokerConfiguration CurrentConfiguration
    {
        get
        {
            lock (_lock)
            {
                return _currentConfiguration ?? throw new InvalidOperationException(
                    "Broker has not been configured yet. Call ConfigureAsync first.");
            }
        }
        private set
        {
            lock (_lock)
            {
                _currentConfiguration = value;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the broker is currently running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Occurs when the broker configuration has been successfully changed.
    /// </summary>
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Occurs before a configuration change is applied, allowing subscribers to cancel the change.
    /// </summary>
    public event EventHandler<ConfigurationChangingEventArgs>? ConfigurationChanging;

    /// <summary>
    /// Applies a new configuration to the broker.
    /// First call starts the server; subsequent calls perform hot reload.
    /// </summary>
    /// <param name="config">The new configuration to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure, including any error details and the new configuration version.</returns>
    public async Task<ConfigurationResult> ConfigureAsync(
        BrokerConfiguration config,
        CancellationToken cancellationToken = default)
    {
        if (config == null)
        {
            return ConfigurationResult.Failed("Configuration cannot be null");
        }

        await _operationSemaphore.WaitAsync(cancellationToken);
        try
        {
            return await ConfigureInternalAsync(config, cancellationToken);
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    /// <summary>
    /// Internal configuration method that doesn't acquire the semaphore.
    /// Used by RollbackAsync and other methods that already hold the semaphore.
    /// </summary>
    private async Task<ConfigurationResult> ConfigureInternalAsync(
        BrokerConfiguration config,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate the configuration
        var validationResult = _currentConfiguration == null
            ? _validator.Validate(config)
            : _validator.ValidateChanges(_currentConfiguration, config);

        if (!validationResult.IsValid)
        {
            return ConfigurationResult.Failed(
                $"Configuration validation failed: {validationResult.GetSummary()}");
        }

        // Step 2: Calculate diff and raise ConfigurationChanging event (cancelable)
        var diff = ConfigurationDiffEngine.CalculateDiff(_currentConfiguration, config);
        var changingArgs = new ConfigurationChangingEventArgs(_currentConfiguration ?? config, config, diff);

        ConfigurationChanging?.Invoke(this, changingArgs);

        if (changingArgs.Cancel)
        {
            return ConfigurationResult.Failed(
                changingArgs.CancellationReason ?? "Configuration change was canceled");
        }

        // Step 3: Map to ServerConfig and serialize
        var serverConfig = ConfigurationMapper.MapToServerConfig(config);
        var configJson = SerializeConfig(serverConfig);

        // Step 4: Call appropriate binding method (start or reload)
        IntPtr resultPtr = IntPtr.Zero;
        string response;

        try
        {
            if (!IsRunning)
            {
                // First time - start the server
                resultPtr = _bindings.StartServer(configJson);
                response = MarshalResponseString(resultPtr);

                if (IsErrorResponse(response))
                {
                    return ConfigurationResult.Failed($"Failed to start NATS server: {response}");
                }

                IsRunning = true;
            }
            else
            {
                // Subsequent times - hot reload
                resultPtr = _bindings.UpdateAndReloadConfig(configJson);
                response = MarshalResponseString(resultPtr);

                if (IsErrorResponse(response))
                {
                    return ConfigurationResult.Failed($"Failed to reload NATS configuration: {response}");
                }
            }
        }
        finally
        {
            if (resultPtr != IntPtr.Zero)
            {
                _bindings.FreeString(resultPtr);
            }
        }

        // Step 5: Create and save version
        var oldVersion = _currentVersionNumber > 0
            ? await _store.GetVersionAsync(_currentVersionNumber, cancellationToken)
            : null;

        var newVersion = new ConfigurationVersion
        {
            Configuration = (BrokerConfiguration)config.Clone(),
            AppliedAt = DateTimeOffset.UtcNow,
            ChangeType = IsRunning && _currentVersionNumber > 0
                ? ConfigurationChangeType.Update
                : ConfigurationChangeType.Initial
        };

        await _store.SaveAsync(newVersion, cancellationToken);

        // Update current state
        _currentVersionNumber = newVersion.Version;
        CurrentConfiguration = (BrokerConfiguration)config.Clone();

        // Step 6: Raise ConfigurationChanged event
        if (oldVersion != null)
        {
            var changedArgs = new ConfigurationChangedEventArgs(oldVersion, newVersion, diff);
            ConfigurationChanged?.Invoke(this, changedArgs);
        }

        // Step 7: Return success result
        return ConfigurationResult.Succeeded(newVersion, diff);
    }

    /// <summary>
    /// Applies incremental changes to the current configuration using a configuration action.
    /// </summary>
    /// <param name="configure">An action that modifies the current configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure, including any error details and the new configuration version.</returns>
    public async Task<ConfigurationResult> ApplyChangesAsync(
        Action<BrokerConfiguration> configure,
        CancellationToken cancellationToken = default)
    {
        if (configure == null)
        {
            return ConfigurationResult.Failed("Configure action cannot be null");
        }

        if (_currentConfiguration == null)
        {
            return ConfigurationResult.Failed(
                "Cannot apply changes - broker has not been configured yet. Call ConfigureAsync first.");
        }

        // Clone current configuration
        var modifiedConfig = (BrokerConfiguration)_currentConfiguration.Clone();

        // Apply the configure action
        try
        {
            configure(modifiedConfig);
        }
        catch (Exception ex)
        {
            return ConfigurationResult.Failed($"Error applying configuration changes: {ex.Message}");
        }

        // Apply the modified configuration
        return await ConfigureAsync(modifiedConfig, cancellationToken);
    }

    /// <summary>
    /// Rolls back the configuration to a previous version.
    /// </summary>
    /// <param name="toVersion">The version number to roll back to. If null, rolls back to the previous version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure, including any error details and the restored configuration version.</returns>
    public async Task<ConfigurationResult> RollbackAsync(
        int? toVersion = null,
        CancellationToken cancellationToken = default)
    {
        await _operationSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Determine target version
            ConfigurationVersion? targetVersion;

            if (toVersion.HasValue)
            {
                targetVersion = await _store.GetVersionAsync(toVersion.Value, cancellationToken);
                if (targetVersion == null)
                {
                    return ConfigurationResult.Failed($"Version {toVersion.Value} not found in configuration history");
                }
            }
            else
            {
                // Get previous version (current - 1)
                var targetVersionNumber = _currentVersionNumber - 1;
                if (targetVersionNumber < 1)
                {
                    return ConfigurationResult.Failed("No previous version available to rollback to");
                }

                targetVersion = await _store.GetVersionAsync(targetVersionNumber, cancellationToken);
                if (targetVersion == null)
                {
                    return ConfigurationResult.Failed($"Previous version {targetVersionNumber} not found in configuration history");
                }
            }

            // Clone the configuration and mark as rollback
            var rollbackConfig = (BrokerConfiguration)targetVersion.Configuration.Clone();
            rollbackConfig.Description = $"Rollback to version {targetVersion.Version}";

            // Apply the rollback configuration using internal method (we already hold the semaphore)
            var result = await ConfigureInternalAsync(rollbackConfig, cancellationToken);

            // If successful, update the change type to Rollback
            if (result.Success && result.Version != null)
            {
                result.Version.ChangeType = ConfigurationChangeType.Rollback;
                await _store.SaveAsync(result.Version, cancellationToken);
            }

            return result;
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves detailed information about the current state of the broker.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the broker including connection details, statistics, and current configuration.</returns>
    public async Task<BrokerInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Broker is not running. Call ConfigureAsync first.");
        }

        await _operationSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Set the current port to ensure we query the correct server instance
            // This is critical for multi-server scenarios
            _bindings.SetCurrentPort(_currentConfiguration!.Port);

            // Get client URL
            IntPtr urlPtr = IntPtr.Zero;
            string clientUrl;

            try
            {
                urlPtr = _bindings.GetClientURL();
                clientUrl = MarshalResponseString(urlPtr);

                if (IsErrorResponse(clientUrl))
                {
                    clientUrl = $"nats://{_currentConfiguration?.Host ?? "localhost"}:{_currentConfiguration?.Port ?? 4222}";
                }
            }
            finally
            {
                if (urlPtr != IntPtr.Zero)
                {
                    _bindings.FreeString(urlPtr);
                }
            }

            // Get server info
            IntPtr infoPtr = IntPtr.Zero;
            string serverInfoJson;

            try
            {
                infoPtr = _bindings.GetServerInfo();
                serverInfoJson = MarshalResponseString(infoPtr);
            }
            finally
            {
                if (infoPtr != IntPtr.Zero)
                {
                    _bindings.FreeString(infoPtr);
                }
            }

            // Parse server info JSON
            var brokerInfo = new BrokerInfo
            {
                ClientUrl = clientUrl,
                CurrentConfig = (BrokerConfiguration)_currentConfiguration!.Clone(),
                JetstreamEnabled = _currentConfiguration?.Jetstream ?? false
            };

            // Try to parse server info if not an error
            if (!IsErrorResponse(serverInfoJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(serverInfoJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("server_id", out var serverId))
                    {
                        brokerInfo.ServerId = serverId.GetString() ?? string.Empty;
                    }

                    if (root.TryGetProperty("version", out var version))
                    {
                        brokerInfo.Version = version.GetString();
                    }

                    if (root.TryGetProperty("connections", out var connections))
                    {
                        brokerInfo.Connections = connections.GetInt32();
                    }

                    if (root.TryGetProperty("now", out var now))
                    {
                        var nowString = now.GetString();
                        if (DateTimeOffset.TryParse(nowString, out var timestamp))
                        {
                            brokerInfo.StartedAt = timestamp;
                        }
                    }
                }
                catch (JsonException)
                {
                    // If parsing fails, use defaults
                    brokerInfo.ServerId = "unknown";
                    brokerInfo.StartedAt = DateTimeOffset.UtcNow;
                }
            }
            else
            {
                brokerInfo.ServerId = "unknown";
                brokerInfo.StartedAt = DateTimeOffset.UtcNow;
            }

            return brokerInfo;
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    /// <summary>
    /// Enters lame duck mode, which stops accepting new connections while allowing existing connections to drain gracefully.
    /// This is useful for performing zero-downtime deployments and graceful shutdowns.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when lame duck mode has been entered.</returns>
    public async Task EnterLameDuckModeAsync(CancellationToken cancellationToken = default)
    {
        await _operationSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Cannot enter lame duck mode - broker is not running. Call ConfigureAsync first.");
            }

            // Set the current port to ensure we operate on the correct server instance
            _bindings.SetCurrentPort(_currentConfiguration!.Port);

            IntPtr resultPtr = IntPtr.Zero;
            try
            {
                resultPtr = _bindings.EnterLameDuckMode();
                var response = MarshalResponseString(resultPtr);

                if (IsErrorResponse(response))
                {
                    throw new InvalidOperationException($"Failed to enter lame duck mode: {response}");
                }
            }
            finally
            {
                if (resultPtr != IntPtr.Zero)
                {
                    _bindings.FreeString(resultPtr);
                }
            }
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    /// <summary>
    /// Gracefully shuts down the broker instance.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous shutdown operation.</returns>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await _operationSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!IsRunning)
            {
                return;
            }

            _bindings.ShutdownServer();
            IsRunning = false;

            // Clear current configuration (optional - keep for inspection)
            // _currentConfiguration = null;
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    /// <summary>
    /// Serializes a ServerConfig to JSON for passing to NATS bindings.
    /// Uses camelCase naming policy to match NATS expectations.
    /// </summary>
    /// <param name="config">The ServerConfig to serialize.</param>
    /// <returns>JSON string representation of the configuration.</returns>
    private string SerializeConfig(ServerConfig config)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        return JsonSerializer.Serialize(config, options);
    }

    /// <summary>
    /// Marshals a pointer returned from native bindings to a managed string.
    /// </summary>
    /// <param name="ptr">The pointer to marshal.</param>
    /// <returns>The marshaled string, or empty string if pointer is null.</returns>
    private string MarshalResponseString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return string.Empty;
        }

        return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
    }

    /// <summary>
    /// Checks if a binding response indicates an error.
    /// </summary>
    /// <param name="response">The response string to check.</param>
    /// <returns>True if the response indicates an error; otherwise, false.</returns>
    private bool IsErrorResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        var lowerResponse = response.ToLowerInvariant();
        return lowerResponse.Contains("error") ||
               lowerResponse.Contains("failed") ||
               lowerResponse.Contains("exception") ||
               lowerResponse.Contains("invalid");
    }

    /// <summary>
    /// Disposes the controller and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the controller and releases all resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Shutdown the server if still running
            if (IsRunning)
            {
                try
                {
                    // Use a timeout to prevent deadlock during disposal
                    // Try to acquire semaphore with 5 second timeout
                    if (_operationSemaphore.Wait(TimeSpan.FromSeconds(5)))
                    {
                        try
                        {
                            _bindings.ShutdownServer();
                            IsRunning = false;
                        }
                        finally
                        {
                            _operationSemaphore.Release();
                        }
                    }
                    else
                    {
                        // If we can't acquire the semaphore, force shutdown anyway
                        // This prevents disposal from hanging forever
                        _bindings.ShutdownServer();
                        IsRunning = false;
                    }
                }
                catch
                {
                    // Ignore errors during disposal
                }
            }

            _operationSemaphore.Dispose();
        }

        _disposed = true;
    }
}
