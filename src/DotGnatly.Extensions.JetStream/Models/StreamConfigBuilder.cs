using NATS.Client.JetStream.Models;

namespace DotGnatly.Extensions.JetStream.Models;

/// <summary>
/// Fluent builder for creating JetStream StreamConfig instances.
/// Provides a convenient API for configuring streams with method chaining.
/// </summary>
public class StreamConfigBuilder
{
    private readonly string _name;
    private readonly List<string> _subjects = new();
    private StreamConfigRetention _retention = StreamConfigRetention.Limits;
    private StreamConfigStorage _storage = StreamConfigStorage.File;
    private int _replicas = 1;
    private long _maxMsgs = -1;
    private long _maxBytes = -1;
    private TimeSpan _maxAge = TimeSpan.Zero;
    private int _maxMsgSize = -1;
    private int _maxConsumers = -1;
    private StreamConfigDiscard _discard = StreamConfigDiscard.Old;
    private bool _noAck = false;
    private string? _description;
    private TimeSpan _duplicateWindow = TimeSpan.FromMinutes(2);

    // Advanced properties
    private long _maxMsgsPerSubject = -1;
    private Placement? _placement;
    private readonly List<StreamSource> _sources = new();
    private bool _sealed = false;
    private bool _denyDelete = false;
    private bool _denyPurge = false;
    private bool _allowRollup = false;
    private bool _allowDirect = false;

    /// <summary>
    /// Initializes a new instance of the StreamConfigBuilder class.
    /// </summary>
    /// <param name="name">The name of the stream.</param>
    public StreamConfigBuilder(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        _name = name;
    }

    /// <summary>
    /// Adds one or more subjects that the stream will listen to.
    /// </summary>
    /// <param name="subjects">The subjects to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithSubjects(params string[] subjects)
    {
        if (subjects == null || subjects.Length == 0)
        {
            throw new ArgumentNullException(nameof(subjects));
        }

        _subjects.AddRange(subjects);
        return this;
    }

    /// <summary>
    /// Sets the retention policy for the stream.
    /// </summary>
    /// <param name="retention">The retention policy (Limits, Interest, or WorkQueue).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithRetention(StreamConfigRetention retention)
    {
        _retention = retention;
        return this;
    }

    /// <summary>
    /// Sets the storage type for the stream.
    /// </summary>
    /// <param name="storage">The storage type (File or Memory).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithStorage(StreamConfigStorage storage)
    {
        _storage = storage;
        return this;
    }

    /// <summary>
    /// Sets the number of replicas for the stream (for clustered JetStream).
    /// </summary>
    /// <param name="replicas">The number of replicas (1-5).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithReplicas(int replicas)
    {
        if (replicas < 1 || replicas > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(replicas), "Replicas must be between 1 and 5");
        }

        _replicas = replicas;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of messages the stream can hold.
    /// </summary>
    /// <param name="maxMessages">The maximum number of messages (-1 for unlimited).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithMaxMessages(long maxMessages)
    {
        _maxMsgs = maxMessages;
        return this;
    }

    /// <summary>
    /// Sets the maximum total size in bytes the stream can hold.
    /// </summary>
    /// <param name="maxBytes">The maximum size in bytes (-1 for unlimited).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithMaxBytes(long maxBytes)
    {
        _maxBytes = maxBytes;
        return this;
    }

    /// <summary>
    /// Sets the maximum age of messages in the stream.
    /// </summary>
    /// <param name="maxAge">The maximum age (TimeSpan.Zero for unlimited).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithMaxAge(TimeSpan maxAge)
    {
        _maxAge = maxAge;
        return this;
    }

    /// <summary>
    /// Sets the maximum size of an individual message.
    /// </summary>
    /// <param name="maxMessageSize">The maximum message size in bytes (-1 for unlimited).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithMaxMessageSize(int maxMessageSize)
    {
        _maxMsgSize = maxMessageSize;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of consumers for the stream.
    /// </summary>
    /// <param name="maxConsumers">The maximum number of consumers (-1 for unlimited).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithMaxConsumers(int maxConsumers)
    {
        _maxConsumers = maxConsumers;
        return this;
    }

    /// <summary>
    /// Sets the discard policy when limits are reached.
    /// </summary>
    /// <param name="discard">The discard policy (Old or New).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithDiscard(StreamConfigDiscard discard)
    {
        _discard = discard;
        return this;
    }

    /// <summary>
    /// Disables message acknowledgement for the stream.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithNoAck()
    {
        _noAck = true;
        return this;
    }

    /// <summary>
    /// Sets a description for the stream.
    /// </summary>
    /// <param name="description">The description text.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the duplicate message detection window.
    /// </summary>
    /// <param name="window">The time window for duplicate detection.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithDuplicateWindow(TimeSpan window)
    {
        _duplicateWindow = window;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of messages per subject.
    /// </summary>
    /// <param name="maxMessagesPerSubject">The maximum messages per subject (-1 for unlimited).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithMaxMessagesPerSubject(long maxMessagesPerSubject)
    {
        _maxMsgsPerSubject = maxMessagesPerSubject;
        return this;
    }

    /// <summary>
    /// Sets the placement constraints for the stream.
    /// </summary>
    /// <param name="cluster">The cluster name for placement.</param>
    /// <param name="tags">Optional tags for placement.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithPlacement(string? cluster = null, string[]? tags = null)
    {
        _placement = new Placement
        {
            Cluster = cluster ?? string.Empty,
            Tags = tags?.ToList() ?? new List<string>()
        };
        return this;
    }

    /// <summary>
    /// Adds a stream source for sourcing messages from another stream.
    /// </summary>
    /// <param name="name">The name of the source stream.</param>
    /// <param name="filterSubject">Optional subject filter.</param>
    /// <param name="externalApi">External API prefix for hub-and-spoke.</param>
    /// <param name="externalDeliver">External deliver subject for hub-and-spoke.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder AddSource(string name, string? filterSubject = null, string? externalApi = null, string? externalDeliver = null)
    {
        var source = new StreamSource
        {
            Name = name,
            FilterSubject = filterSubject,
        };

        if (!string.IsNullOrWhiteSpace(externalApi) || !string.IsNullOrWhiteSpace(externalDeliver))
        {
            source.External = new External
            {
                ApiPrefix = externalApi ?? string.Empty,
                DeliverPrefix = externalDeliver ?? string.Empty
            };
        }

        _sources.Add(source);
        return this;
    }

    /// <summary>
    /// Seals the stream, preventing further modifications.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithSealed(bool @sealed = true)
    {
        _sealed = @sealed;
        return this;
    }

    /// <summary>
    /// Prevents message deletion from the stream.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithDenyDelete(bool denyDelete = true)
    {
        _denyDelete = denyDelete;
        return this;
    }

    /// <summary>
    /// Prevents purging messages from the stream.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithDenyPurge(bool denyPurge = true)
    {
        _denyPurge = denyPurge;
        return this;
    }

    /// <summary>
    /// Allows rollup headers for message aggregation.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithAllowRollup(bool allowRollup = true)
    {
        _allowRollup = allowRollup;
        return this;
    }

    /// <summary>
    /// Enables direct access to messages (bypassing consumer API).
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public StreamConfigBuilder WithAllowDirect(bool allowDirect = true)
    {
        _allowDirect = allowDirect;
        return this;
    }

    /// <summary>
    /// Builds the StreamConfig instance with the configured settings.
    /// </summary>
    /// <returns>A configured StreamConfig instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no subjects are configured.</exception>
    public StreamConfig Build()
    {
        if (_subjects.Count == 0)
        {
            throw new InvalidOperationException("At least one subject must be configured for the stream");
        }

        return new StreamConfig
        {
            Name = _name,
            Subjects = _subjects,
            Retention = _retention,
            Storage = _storage,
            Replicas = _replicas,
            MaxMsgs = _maxMsgs,
            MaxBytes = _maxBytes,
            MaxAge = _maxAge,
            MaxMsgSize = _maxMsgSize,
            MaxConsumers = _maxConsumers,
            Discard = _discard,
            NoAck = _noAck,
            Description = _description,
            DuplicateWindow = _duplicateWindow,

            // Advanced properties
            MaxMsgsPerSubject = _maxMsgsPerSubject,
            Placement = _placement,
            Sources = _sources.Count > 0 ? _sources : null,
            Sealed = _sealed,
            DenyDelete = _denyDelete,
            DenyPurge = _denyPurge,
            AllowRollup = _allowRollup,
            AllowDirect = _allowDirect
        };
    }
}
