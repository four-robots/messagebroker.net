using MessageBroker.Core.Configuration;

namespace MessageBroker.Nats.Implementation;

/// <summary>
/// Provides extension methods for NatsController to enable fluent API configuration patterns.
/// Makes common configuration changes more concise and readable.
/// </summary>
public static class NatsControllerExtensions
{
    /// <summary>
    /// Changes the port the broker listens on.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="port">The new port number (1-65535).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> WithPortAsync(
        this NatsController controller,
        int port,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.Port = port, cancellationToken);
    }

    /// <summary>
    /// Changes the host address the broker binds to.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="host">The hostname or IP address to bind to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> WithHostAsync(
        this NatsController controller,
        string host,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.Host = host, cancellationToken);
    }

    /// <summary>
    /// Enables JetStream with the specified storage path.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="storePath">The directory path for JetStream storage.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or storePath is null.</exception>
    public static Task<ConfigurationResult> EnableJetStreamAsync(
        this NatsController controller,
        string storePath,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(storePath))
        {
            throw new ArgumentNullException(nameof(storePath));
        }

        return controller.ApplyChangesAsync(config =>
        {
            config.Jetstream = true;
            config.JetstreamStoreDir = storePath;
        }, cancellationToken);
    }

    /// <summary>
    /// Disables JetStream.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> DisableJetStreamAsync(
        this NatsController controller,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.Jetstream = false, cancellationToken);
    }

    /// <summary>
    /// Enables or disables debug logging.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="enabled">True to enable debug logging; false to disable.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetDebugAsync(
        this NatsController controller,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.Debug = enabled, cancellationToken);
    }

    /// <summary>
    /// Enables or disables trace logging.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="enabled">True to enable trace logging; false to disable.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetTraceAsync(
        this NatsController controller,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.Trace = enabled, cancellationToken);
    }

    /// <summary>
    /// Sets the maximum message payload size in bytes.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="bytes">The maximum payload size in bytes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetMaxPayloadAsync(
        this NatsController controller,
        int bytes,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.MaxPayload = bytes, cancellationToken);
    }

    /// <summary>
    /// Sets the maximum control line size.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="bytes">The maximum control line size in bytes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetMaxControlLineAsync(
        this NatsController controller,
        int bytes,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.MaxControlLine = bytes, cancellationToken);
    }

    /// <summary>
    /// Enables HTTP monitoring on the specified port.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="port">The port number for HTTP monitoring (1-65535).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> EnableHttpMonitoringAsync(
        this NatsController controller,
        int port,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.HttpPort = port, cancellationToken);
    }

    /// <summary>
    /// Disables HTTP monitoring.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> DisableHttpMonitoringAsync(
        this NatsController controller,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.HttpPort = 0, cancellationToken);
    }

    /// <summary>
    /// Sets basic authentication credentials.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetAuthenticationAsync(
        this NatsController controller,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config =>
        {
            config.Auth.Username = username;
            config.Auth.Password = password;
            config.Auth.Token = null; // Clear token when using username/password
        }, cancellationToken);
    }

    /// <summary>
    /// Sets token-based authentication.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="token">The authentication token.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetAuthenticationTokenAsync(
        this NatsController controller,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config =>
        {
            config.Auth.Token = token;
            config.Auth.Username = null; // Clear username/password when using token
            config.Auth.Password = null;
        }, cancellationToken);
    }

    /// <summary>
    /// Removes all authentication.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> ClearAuthenticationAsync(
        this NatsController controller,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config =>
        {
            config.Auth.Username = null;
            config.Auth.Password = null;
            config.Auth.Token = null;
            config.Auth.AllowedUsers.Clear();
        }, cancellationToken);
    }

    /// <summary>
    /// Sets JetStream memory limit.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="bytes">The maximum memory in bytes (-1 for unlimited).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetJetStreamMaxMemoryAsync(
        this NatsController controller,
        long bytes,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.JetstreamMaxMemory = bytes, cancellationToken);
    }

    /// <summary>
    /// Sets JetStream storage limit.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="bytes">The maximum storage in bytes (-1 for unlimited).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetJetStreamMaxStoreAsync(
        this NatsController controller,
        long bytes,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.JetstreamMaxStore = bytes, cancellationToken);
    }

    /// <summary>
    /// Configures ping interval and max pings out settings.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="intervalSeconds">The interval in seconds between pings.</param>
    /// <param name="maxPingsOut">The maximum number of outstanding pings before considering a client unresponsive.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetPingConfigurationAsync(
        this NatsController controller,
        int intervalSeconds,
        int maxPingsOut,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config =>
        {
            config.PingInterval = intervalSeconds;
            config.MaxPingsOut = maxPingsOut;
        }, cancellationToken);
    }

    /// <summary>
    /// Sets the write deadline for client connections.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="seconds">The write deadline in seconds.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetWriteDeadlineAsync(
        this NatsController controller,
        int seconds,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config => config.WriteDeadline = seconds, cancellationToken);
    }

    /// <summary>
    /// Sets the leaf node import subjects (subjects to receive from remote leaf nodes).
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="subjects">The list of subject patterns to import.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetLeafNodeImportSubjectsAsync(
        this NatsController controller,
        IEnumerable<string> subjects,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config =>
        {
            config.LeafNode.ImportSubjects = subjects?.ToList() ?? new List<string>();
        }, cancellationToken);
    }

    /// <summary>
    /// Sets the leaf node export subjects (subjects to send to remote leaf nodes).
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="subjects">The list of subject patterns to export.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> SetLeafNodeExportSubjectsAsync(
        this NatsController controller,
        IEnumerable<string> subjects,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config =>
        {
            config.LeafNode.ExportSubjects = subjects?.ToList() ?? new List<string>();
        }, cancellationToken);
    }

    /// <summary>
    /// Adds import subjects to the leaf node configuration.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="subjects">The subject patterns to add to the import list.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> AddLeafNodeImportSubjectsAsync(
        this NatsController controller,
        params string[] subjects)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config =>
        {
            foreach (var subject in subjects)
            {
                if (!config.LeafNode.ImportSubjects.Contains(subject))
                {
                    config.LeafNode.ImportSubjects.Add(subject);
                }
            }
        });
    }

    /// <summary>
    /// Adds export subjects to the leaf node configuration.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="subjects">The subject patterns to add to the export list.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> AddLeafNodeExportSubjectsAsync(
        this NatsController controller,
        params string[] subjects)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config =>
        {
            foreach (var subject in subjects)
            {
                if (!config.LeafNode.ExportSubjects.Contains(subject))
                {
                    config.LeafNode.ExportSubjects.Add(subject);
                }
            }
        });
    }

    /// <summary>
    /// Removes import subjects from the leaf node configuration.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="subjects">The subject patterns to remove from the import list.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> RemoveLeafNodeImportSubjectsAsync(
        this NatsController controller,
        params string[] subjects)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config =>
        {
            foreach (var subject in subjects)
            {
                config.LeafNode.ImportSubjects.Remove(subject);
            }
        });
    }

    /// <summary>
    /// Removes export subjects from the leaf node configuration.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    /// <param name="subjects">The subject patterns to remove from the export list.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure of the configuration change.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static Task<ConfigurationResult> RemoveLeafNodeExportSubjectsAsync(
        this NatsController controller,
        params string[] subjects)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        return controller.ApplyChangesAsync(config =>
        {
            foreach (var subject in subjects)
            {
                config.LeafNode.ExportSubjects.Remove(subject);
            }
        });
    }
}
