using DotGnatly.Extensions.JetStream.Models;
using DotGnatly.Nats.Implementation;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace DotGnatly.Extensions.JetStream.Extensions;

/// <summary>
/// Provides JetStream extension methods for NatsController to enable fluent stream management.
/// These methods use the official NATS.Net client library to manage JetStream streams, consumers, and KV stores.
/// </summary>
public static class JetStreamExtensions
{
    /// <summary>
    /// Creates a JetStream context that can be used to manage streams and consumers.
    /// This method establishes a client connection to the NATS server controlled by this NatsController.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A JetStreamContext for managing streams and consumers.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the server is not running or JetStream is not enabled.</exception>
    public static async Task<JetStreamContext> GetJetStreamContextAsync(
        this NatsController controller,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        // Verify JetStream is enabled
        var isJsEnabled = await controller.IsJetStreamEnabledAsync(cancellationToken);
        if (!isJsEnabled)
        {
            throw new InvalidOperationException(
                "JetStream is not enabled on this server. Enable it using EnableJetStreamAsync() before creating streams.");
        }

        // Get connection URL from controller configuration
        var config = controller.CurrentConfiguration;
        if (config == null)
        {
            throw new InvalidOperationException("Server is not configured. Call ConfigureAsync() first.");
        }

        var url = $"nats://{config.Host}:{config.Port}";

        // Create NATS connection options
        var opts = new NatsOpts
        {
            Url = url,
            Name = "DotGnatly.JetStream",
        };

        // Add authentication if configured
        if (!string.IsNullOrWhiteSpace(config.Auth.Username))
        {
            opts = opts with
            {
                AuthOpts = new NatsAuthOpts
                {
                    Username = config.Auth.Username,
                    Password = config.Auth.Password
                }
            };
        }
        else if (!string.IsNullOrWhiteSpace(config.Auth.Token))
        {
            opts = opts with
            {
                AuthOpts = new NatsAuthOpts
                {
                    Token = config.Auth.Token
                }
            };
        }

        // Create connection and JetStream context
        var connection = new NatsConnection(opts);
        await connection.ConnectAsync();

        var js = new NatsJSContext(connection);

        return new JetStreamContext(connection, js);
    }

    /// <summary>
    /// Creates a new JetStream stream with the specified configuration.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="streamName">The name of the stream to create.</param>
    /// <param name="configure">A function to configure the stream.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the created stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or streamName is null.</exception>
    public static async Task<StreamInfo> CreateStreamAsync(
        this NatsController controller,
        string streamName,
        Action<StreamConfigBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(streamName))
        {
            throw new ArgumentNullException(nameof(streamName));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);

        // Build stream configuration
        var builder = new StreamConfigBuilder(streamName);
        configure(builder);
        var streamConfig = builder.Build();

        // Create the stream
        var stream = await context.JetStream.CreateStreamAsync(streamConfig, cancellationToken);

        return await stream.GetInfoAsync(cancellationToken);
    }

    /// <summary>
    /// Gets an existing JetStream stream.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="streamName">The name of the stream to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or streamName is null.</exception>
    public static async Task<StreamInfo> GetStreamAsync(
        this NatsController controller,
        string streamName,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(streamName))
        {
            throw new ArgumentNullException(nameof(streamName));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var stream = await context.JetStream.GetStreamAsync(streamName, cancellationToken);

        return await stream.GetInfoAsync(cancellationToken);
    }

    /// <summary>
    /// Lists all JetStream streams.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of stream names.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static async Task<List<string>> ListStreamsAsync(
        this NatsController controller,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);

        var streams = new List<string>();
        await foreach (var name in context.JetStream.ListStreamNamesAsync(cancellationToken: cancellationToken))
        {
            streams.Add(name);
        }

        return streams;
    }

    /// <summary>
    /// Deletes a JetStream stream.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="streamName">The name of the stream to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the stream was deleted successfully.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or streamName is null.</exception>
    public static async Task<bool> DeleteStreamAsync(
        this NatsController controller,
        string streamName,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(streamName))
        {
            throw new ArgumentNullException(nameof(streamName));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        return await context.JetStream.DeleteStreamAsync(streamName, cancellationToken);
    }

    /// <summary>
    /// Updates an existing JetStream stream with new configuration.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="streamName">The name of the stream to update.</param>
    /// <param name="configure">A function to configure the stream.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the updated stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or streamName is null.</exception>
    public static async Task<StreamInfo> UpdateStreamAsync(
        this NatsController controller,
        string streamName,
        Action<StreamConfigBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(streamName))
        {
            throw new ArgumentNullException(nameof(streamName));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);

        // Build stream configuration
        var builder = new StreamConfigBuilder(streamName);
        configure(builder);
        var streamConfig = builder.Build();

        // Update the stream
        var stream = await context.JetStream.CreateStreamAsync(streamConfig, cancellationToken);

        return await stream.GetInfoAsync(cancellationToken);
    }

    /// <summary>
    /// Purges all messages from a JetStream stream.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="streamName">The name of the stream to purge.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The purge response from the server.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or streamName is null.</exception>
    public static async Task<StreamPurgeResponse> PurgeStreamAsync(
        this NatsController controller,
        string streamName,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(streamName))
        {
            throw new ArgumentNullException(nameof(streamName));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var stream = await context.JetStream.GetStreamAsync(streamName, cancellationToken);

        return await stream.PurgeAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates a JetStream stream from a JSON configuration string.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="json">The JSON configuration string.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the created stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or json is null.</exception>
    public static async Task<StreamInfo> CreateStreamFromJsonAsync(
        this NatsController controller,
        string json,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentNullException(nameof(json));
        }

        var configJson = StreamConfigJson.FromJson(json);
        var builder = configJson.ToBuilder();

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var streamConfig = builder.Build();
        var stream = await context.JetStream.CreateStreamAsync(streamConfig, cancellationToken);

        return await stream.GetInfoAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a JetStream stream from a JSON configuration file.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="filePath">The path to the JSON configuration file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the created stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or filePath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static async Task<StreamInfo> CreateStreamFromFileAsync(
        this NatsController controller,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        var configJson = await StreamConfigJson.FromFileAsync(filePath);
        var builder = configJson.ToBuilder();

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var streamConfig = builder.Build();
        var stream = await context.JetStream.CreateStreamAsync(streamConfig, cancellationToken);

        return await stream.GetInfoAsync(cancellationToken);
    }

    /// <summary>
    /// Creates multiple JetStream streams from JSON configuration files in a directory.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="directoryPath">The directory containing JSON configuration files.</param>
    /// <param name="pattern">The file pattern to match (default: *.json).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of StreamInfo for all created streams.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or directoryPath is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    public static async Task<List<StreamInfo>> CreateStreamsFromDirectoryAsync(
        this NatsController controller,
        string directoryPath,
        string pattern = "*.json",
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentNullException(nameof(directoryPath));
        }

        var configs = await StreamConfigJson.FromDirectoryAsync(directoryPath, pattern);
        var streamInfos = new List<StreamInfo>();

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);

        foreach (var configJson in configs)
        {
            var builder = configJson.ToBuilder();
            var streamConfig = builder.Build();
            var stream = await context.JetStream.CreateStreamAsync(streamConfig, cancellationToken);
            var info = await stream.GetInfoAsync(cancellationToken);
            streamInfos.Add(info);
        }

        return streamInfos;
    }

    /// <summary>
    /// Updates a JetStream stream from a JSON configuration file.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="filePath">The path to the JSON configuration file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the updated stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or filePath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static async Task<StreamInfo> UpdateStreamFromFileAsync(
        this NatsController controller,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        var configJson = await StreamConfigJson.FromFileAsync(filePath);
        var builder = configJson.ToBuilder();

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var streamConfig = builder.Build();
        var stream = await context.JetStream.CreateStreamAsync(streamConfig, cancellationToken);

        return await stream.GetInfoAsync(cancellationToken);
    }
}
