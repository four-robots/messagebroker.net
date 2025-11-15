using MessageBroker.Core.Configuration;
using MessageBroker.Core.Events;
using MessageBroker.Core.Interfaces;
using MessageBroker.Core.Validation;

namespace MessageBroker.Examples;

/// <summary>
/// Mock implementation of IBrokerController for demonstration purposes.
/// This simulates a real NATS broker for the examples to run without requiring actual server infrastructure.
/// </summary>
internal class MockBrokerController : IBrokerController, IDisposable
{
    private BrokerConfiguration _currentConfiguration;
    private readonly IConfigurationStore _store;
    private readonly ConfigurationValidator _validator;
    private int _nextVersion = 1;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;
    private bool _isRunning = true;

    public BrokerConfiguration CurrentConfiguration => _currentConfiguration;

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    public event EventHandler<ConfigurationChangingEventArgs>? ConfigurationChanging;

    public MockBrokerController(BrokerConfiguration? initialConfig = null)
    {
        _currentConfiguration = initialConfig ?? new BrokerConfiguration();
        _store = new InMemoryConfigurationStore();
        _validator = new ConfigurationValidator();

        // Save initial configuration
        var initialVersion = new ConfigurationVersion
        {
            Version = _nextVersion++,
            Configuration = (BrokerConfiguration)_currentConfiguration.Clone(),
            AppliedAt = DateTimeOffset.UtcNow,
            ChangeType = ConfigurationChangeType.Initial
        };
        _store.SaveAsync(initialVersion).Wait();
    }

    public async Task<ConfigurationResult> ConfigureAsync(BrokerConfiguration config, CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return ConfigurationResult.Failed("Server is not running");
        }

        // Validate configuration
        var validationResult = _validator.Validate(config);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return ConfigurationResult.Failed($"Configuration validation failed: {errors}");
        }

        // Calculate diff
        var diff = ConfigurationDiffEngine.CalculateDiff(_currentConfiguration, config);

        // Fire ConfigurationChanging event
        var changingArgs = new ConfigurationChangingEventArgs(_currentConfiguration, config, diff);
        ConfigurationChanging?.Invoke(this, changingArgs);

        if (changingArgs.Cancel)
        {
            return ConfigurationResult.Failed("Configuration change was cancelled by event handler");
        }

        // Apply configuration
        var oldVersion = new ConfigurationVersion
        {
            Version = _nextVersion - 1,
            Configuration = (BrokerConfiguration)_currentConfiguration.Clone(),
            AppliedAt = DateTimeOffset.UtcNow,
            ChangeType = ConfigurationChangeType.Update
        };

        _currentConfiguration = (BrokerConfiguration)config.Clone();

        // Create new version
        var newVersion = new ConfigurationVersion
        {
            Version = _nextVersion++,
            Configuration = (BrokerConfiguration)_currentConfiguration.Clone(),
            AppliedAt = DateTimeOffset.UtcNow,
            ChangeType = ConfigurationChangeType.Update
        };

        await _store.SaveAsync(newVersion, cancellationToken);

        // Simulate applying changes (in real implementation, this would restart NATS server)
        await Task.Delay(100, cancellationToken);

        // Fire ConfigurationChanged event
        var changedArgs = new ConfigurationChangedEventArgs(oldVersion, newVersion, diff);
        ConfigurationChanged?.Invoke(this, changedArgs);

        return ConfigurationResult.Succeeded(newVersion, diff);
    }

    public async Task<ConfigurationResult> ApplyChangesAsync(Action<BrokerConfiguration> configure, CancellationToken cancellationToken = default)
    {
        var newConfig = (BrokerConfiguration)_currentConfiguration.Clone();
        configure(newConfig);
        return await ConfigureAsync(newConfig, cancellationToken);
    }

    public async Task<ConfigurationResult> RollbackAsync(int? toVersion = null, CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return ConfigurationResult.Failed("Server is not running");
        }

        ConfigurationVersion? targetVersion;

        if (toVersion.HasValue)
        {
            targetVersion = await _store.GetVersionAsync(toVersion.Value, cancellationToken);
            if (targetVersion == null)
            {
                return ConfigurationResult.Failed($"Version {toVersion} not found");
            }
        }
        else
        {
            var history = await _store.GetHistoryAsync(2, cancellationToken);
            if (history.Count < 2)
            {
                return ConfigurationResult.Failed("No previous version to rollback to");
            }
            targetVersion = history[1];
        }

        // Apply the rollback
        var oldVersion = new ConfigurationVersion
        {
            Version = _nextVersion - 1,
            Configuration = (BrokerConfiguration)_currentConfiguration.Clone(),
            AppliedAt = DateTimeOffset.UtcNow,
            ChangeType = ConfigurationChangeType.Update
        };

        _currentConfiguration = (BrokerConfiguration)targetVersion.Configuration.Clone();

        // Calculate diff
        var diff = ConfigurationDiffEngine.CalculateDiff(oldVersion.Configuration, _currentConfiguration);

        // Create rollback version
        var newVersion = new ConfigurationVersion
        {
            Version = _nextVersion++,
            Configuration = (BrokerConfiguration)_currentConfiguration.Clone(),
            AppliedAt = DateTimeOffset.UtcNow,
            ChangeType = ConfigurationChangeType.Rollback
        };

        await _store.SaveAsync(newVersion, cancellationToken);

        // Simulate applying changes
        await Task.Delay(100, cancellationToken);

        // Fire events
        var changedArgs = new ConfigurationChangedEventArgs(oldVersion, newVersion, diff);
        ConfigurationChanged?.Invoke(this, changedArgs);

        return ConfigurationResult.Succeeded(newVersion, diff);
    }

    public async Task<BrokerInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        return new BrokerInfo
        {
            ClientUrl = $"nats://{_currentConfiguration.Host}:{_currentConfiguration.Port}",
            ServerId = Guid.NewGuid().ToString("N")[..16],
            Connections = Random.Shared.Next(0, 10),
            StartedAt = _startedAt,
            CurrentConfig = _currentConfiguration,
            Version = "2.10.0",
            JetstreamEnabled = _currentConfiguration.Jetstream
        };
    }

    public async Task EnterLameDuckModeAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            throw new InvalidOperationException("Cannot enter lame duck mode - server is not running");
        }

        // Simulate entering lame duck mode
        await Task.Delay(50, cancellationToken);
        // In a real implementation, this would signal the server to stop accepting new connections
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        await Task.Delay(50, cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationVersion>> GetHistoryAsync(int count = 10)
    {
        return await _store.GetHistoryAsync(count);
    }

    public int GetCurrentVersion()
    {
        return _nextVersion - 1;
    }

    public void Dispose()
    {
        _ = ShutdownAsync().ConfigureAwait(false);
    }
}
