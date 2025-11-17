using DotGnatly.Extensions.JetStream.Models;
using DotGnatly.Nats.Implementation;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;

namespace DotGnatly.Extensions.JetStream.Extensions;

/// <summary>
/// Provides Key-Value store extension methods for NatsController.
/// KV stores are built on top of JetStream and provide a simple key-value interface.
/// </summary>
public static class KeyValueExtensions
{
    /// <summary>
    /// Creates a new NATS Key-Value store.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="bucket">The name of the KV bucket.</param>
    /// <param name="configure">A function to configure the KV store.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created KV store.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or bucket is null.</exception>
    public static async Task<INatsKVStore> CreateKeyValueAsync(
        this NatsController controller,
        string bucket,
        Action<KVConfigBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new ArgumentNullException(nameof(bucket));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);

        // Build KV configuration
        var builder = new KVConfigBuilder(bucket);
        configure(builder);
        var kvConfig = builder.Build();

        // Create KV context
        var kvContext = new NatsKVContext(context.Connection);

        // Create the KV store
        return await kvContext.CreateStoreAsync(kvConfig, cancellationToken);
    }

    /// <summary>
    /// Gets an existing NATS Key-Value store.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="bucket">The name of the KV bucket.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The KV store.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or bucket is null.</exception>
    public static async Task<INatsKVStore> GetKeyValueAsync(
        this NatsController controller,
        string bucket,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new ArgumentNullException(nameof(bucket));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var kvContext = new NatsKVContext(context.Connection);

        return await kvContext.GetStoreAsync(bucket, cancellationToken);
    }

    /// <summary>
    /// Lists all Key-Value store buckets.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of bucket names.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static async Task<List<string>> ListKeyValuesAsync(
        this NatsController controller,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var kvContext = new NatsKVContext(context.Connection);

        var buckets = new List<string>();
        await foreach (var bucket in kvContext.ListStoreNamesAsync(cancellationToken: cancellationToken))
        {
            buckets.Add(bucket);
        }

        return buckets;
    }

    /// <summary>
    /// Deletes a Key-Value store.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="bucket">The name of the KV bucket to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the bucket was deleted successfully.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or bucket is null.</exception>
    public static async Task<bool> DeleteKeyValueAsync(
        this NatsController controller,
        string bucket,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new ArgumentNullException(nameof(bucket));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var kvContext = new NatsKVContext(context.Connection);

        return await kvContext.DeleteStoreAsync(bucket, cancellationToken);
    }

    /// <summary>
    /// Gets the status/information about a Key-Value store.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="bucket">The name of the KV bucket.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The KV store status.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or bucket is null.</exception>
    public static async Task<NatsKVStatus> GetKeyValueStatusAsync(
        this NatsController controller,
        string bucket,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new ArgumentNullException(nameof(bucket));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var kvContext = new NatsKVContext(context.Connection);

        return await kvContext.GetStatusAsync(bucket, cancellationToken);
    }

    /// <summary>
    /// Purges all keys from a Key-Value store while keeping the bucket.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="bucket">The name of the KV bucket to purge.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the bucket was purged successfully.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or bucket is null.</exception>
    public static async Task<bool> PurgeKeyValueAsync(
        this NatsController controller,
        string bucket,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new ArgumentNullException(nameof(bucket));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var kvContext = new NatsKVContext(context.Connection);

        return await kvContext.PurgeStoreAsync(bucket, cancellationToken);
    }

    /// <summary>
    /// Updates an existing Key-Value store configuration.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="bucket">The name of the KV bucket.</param>
    /// <param name="configure">A function to configure the KV store.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated KV store.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or bucket is null.</exception>
    public static async Task<INatsKVStore> UpdateKeyValueAsync(
        this NatsController controller,
        string bucket,
        Action<KVConfigBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new ArgumentNullException(nameof(bucket));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);

        // Build KV configuration
        var builder = new KVConfigBuilder(bucket);
        configure(builder);
        var kvConfig = builder.Build();

        // Create KV context
        var kvContext = new NatsKVContext(context.Connection);

        // Update the KV store
        return await kvContext.UpdateStoreAsync(kvConfig, cancellationToken);
    }

    /// <summary>
    /// Creates a Key-Value store from a JSON configuration string.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="json">The JSON configuration string.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created KV store.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or json is null.</exception>
    public static async Task<INatsKVStore> CreateKeyValueFromJsonAsync(
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

        var configJson = KVConfigJson.FromJson(json);
        var builder = configJson.ToBuilder();

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var kvContext = new NatsKVContext(context.Connection);
        var kvConfig = builder.Build();

        return await kvContext.CreateStoreAsync(kvConfig, cancellationToken);
    }

    /// <summary>
    /// Creates a Key-Value store from a JSON configuration file.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="filePath">The path to the JSON configuration file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created KV store.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or filePath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static async Task<INatsKVStore> CreateKeyValueFromFileAsync(
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

        var configJson = await KVConfigJson.FromFileAsync(filePath);
        var builder = configJson.ToBuilder();

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var kvContext = new NatsKVContext(context.Connection);
        var kvConfig = builder.Build();

        return await kvContext.CreateStoreAsync(kvConfig, cancellationToken);
    }

    /// <summary>
    /// Creates multiple Key-Value stores from JSON configuration files in a directory.
    /// </summary>
    /// <param name="controller">The NatsController instance.</param>
    /// <param name="directoryPath">The directory containing JSON configuration files.</param>
    /// <param name="pattern">The file pattern to match (default: *.json).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of created KV stores.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller or directoryPath is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    public static async Task<List<INatsKVStore>> CreateKeyValuesFromDirectoryAsync(
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

        var configs = await KVConfigJson.FromDirectoryAsync(directoryPath, pattern);
        var stores = new List<INatsKVStore>();

        await using var context = await controller.GetJetStreamContextAsync(cancellationToken);
        var kvContext = new NatsKVContext(context.Connection);

        foreach (var configJson in configs)
        {
            var builder = configJson.ToBuilder();
            var kvConfig = builder.Build();
            var store = await kvContext.CreateStoreAsync(kvConfig, cancellationToken);
            stores.Add(store);
        }

        return stores;
    }
}
