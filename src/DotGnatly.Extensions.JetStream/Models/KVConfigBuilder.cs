using NATS.Client.KeyValueStore;

namespace DotGnatly.Extensions.JetStream.Models;

/// <summary>
/// Fluent builder for creating NATS Key-Value store configurations.
/// KV stores are built on top of JetStream streams with specific optimizations.
/// </summary>
public class KVConfigBuilder
{
    private readonly string _bucket;
    private string? _description;
    private long _maxHistoryPerKey = 1;
    private TimeSpan _ttl = TimeSpan.Zero;
    private long _maxBucketSize = -1;
    private int _replicas = 1;
    private NatsKVStorageType _storage = NatsKVStorageType.File;

    /// <summary>
    /// Initializes a new instance of the KVConfigBuilder class.
    /// </summary>
    /// <param name="bucket">The name of the KV bucket.</param>
    public KVConfigBuilder(string bucket)
    {
        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new ArgumentNullException(nameof(bucket));
        }

        _bucket = bucket;
    }

    /// <summary>
    /// Sets the description for the KV store.
    /// </summary>
    /// <param name="description">The description text.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KVConfigBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of historical values to keep per key.
    /// </summary>
    /// <param name="maxHistory">The maximum history count (default: 1).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KVConfigBuilder WithMaxHistoryPerKey(long maxHistory)
    {
        _maxHistoryPerKey = maxHistory;
        return this;
    }

    /// <summary>
    /// Sets the time-to-live for keys in the store.
    /// After this duration, keys will be automatically deleted.
    /// </summary>
    /// <param name="ttl">The TTL duration (TimeSpan.Zero for no expiration).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KVConfigBuilder WithTTL(TimeSpan ttl)
    {
        _ttl = ttl;
        return this;
    }

    /// <summary>
    /// Sets the maximum size in bytes for the entire bucket.
    /// </summary>
    /// <param name="maxBucketSize">The maximum size in bytes (-1 for unlimited).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KVConfigBuilder WithMaxBucketSize(long maxBucketSize)
    {
        _maxBucketSize = maxBucketSize;
        return this;
    }

    /// <summary>
    /// Sets the number of replicas for the KV store (for clustered JetStream).
    /// </summary>
    /// <param name="replicas">The number of replicas (1-5).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KVConfigBuilder WithReplicas(int replicas)
    {
        if (replicas < 1 || replicas > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(replicas), "Replicas must be between 1 and 5");
        }

        _replicas = replicas;
        return this;
    }

    /// <summary>
    /// Sets the storage type for the KV store.
    /// </summary>
    /// <param name="storage">The storage type (File or Memory).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KVConfigBuilder WithStorage(NatsKVStorageType storage)
    {
        _storage = storage;
        return this;
    }

    /// <summary>
    /// Builds the NatsKVConfig instance with the configured settings.
    /// </summary>
    /// <returns>A configured NatsKVConfig instance.</returns>
    public NatsKVConfig Build()
    {
        return new NatsKVConfig(_bucket)
        {
            Description = _description,
            History = _maxHistoryPerKey,
            LimitMarkerTTL = _ttl,
            MaxBytes = _maxBucketSize,
            NumberOfReplicas = _replicas,
            Storage = _storage
        };
    }
}
