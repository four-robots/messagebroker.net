using System.Text.Json.Serialization;

namespace MessageBroker.Core.Configuration;

/// <summary>
/// Represents current information about a running broker instance.
/// Contains runtime statistics and connection details.
/// </summary>
public class BrokerInfo
{
    /// <summary>
    /// Gets or sets the client connection URL for the broker.
    /// This is the URL clients should use to connect.
    /// </summary>
    [JsonPropertyName("clientUrl")]
    public string ClientUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique server identifier.
    /// </summary>
    [JsonPropertyName("serverId")]
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of active client connections.
    /// </summary>
    [JsonPropertyName("connections")]
    public int Connections { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the broker was started.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the current active configuration of the broker.
    /// </summary>
    [JsonPropertyName("currentConfig")]
    public BrokerConfiguration CurrentConfig { get; set; } = null!;

    /// <summary>
    /// Gets or sets the version string of the underlying NATS server.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether JetStream is enabled and active.
    /// </summary>
    [JsonPropertyName("jetstreamEnabled")]
    public bool JetstreamEnabled { get; set; }
}
