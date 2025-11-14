using System.Text.Json.Serialization;

namespace MessageBroker.Core.Configuration;

/// <summary>
/// Represents the complete configuration for a message broker instance.
/// Based on NATS ServerConfig with additional metadata for version tracking and management.
/// </summary>
public class BrokerConfiguration : ICloneable
{
    /// <summary>
    /// Gets or sets the unique identifier for this configuration instance.
    /// </summary>
    [JsonPropertyName("configurationId")]
    public Guid ConfigurationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the timestamp when this configuration was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets a human-readable description of this configuration or what changes it contains.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    // Core NATS Server Configuration

    /// <summary>
    /// Gets or sets the hostname or IP address the server will bind to.
    /// </summary>
    [JsonPropertyName("host")]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the port number the server will listen on.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 4222;

    /// <summary>
    /// Gets or sets the maximum message payload size in bytes.
    /// </summary>
    [JsonPropertyName("maxPayload")]
    public int MaxPayload { get; set; } = 1048576; // 1MB

    /// <summary>
    /// Gets or sets the maximum protocol control line size.
    /// </summary>
    [JsonPropertyName("maxControlLine")]
    public int MaxControlLine { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the interval in seconds between server ping messages to clients.
    /// </summary>
    [JsonPropertyName("pingInterval")]
    public int PingInterval { get; set; } = 120;

    /// <summary>
    /// Gets or sets the maximum number of pending pings before considering a client unresponsive.
    /// </summary>
    [JsonPropertyName("maxPingsOut")]
    public int MaxPingsOut { get; set; } = 2;

    /// <summary>
    /// Gets or sets the write deadline in seconds for client connections.
    /// </summary>
    [JsonPropertyName("writeDeadline")]
    public int WriteDeadline { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether debug logging is enabled.
    /// </summary>
    [JsonPropertyName("debug")]
    public bool Debug { get; set; } = false;

    /// <summary>
    /// Gets or sets whether trace logging is enabled (more verbose than debug).
    /// </summary>
    [JsonPropertyName("trace")]
    public bool Trace { get; set; } = false;

    // JetStream Configuration

    /// <summary>
    /// Gets or sets whether JetStream is enabled.
    /// </summary>
    [JsonPropertyName("jetstream")]
    public bool Jetstream { get; set; } = false;

    /// <summary>
    /// Gets or sets the directory path for JetStream storage.
    /// </summary>
    [JsonPropertyName("jetstreamStoreDir")]
    public string JetstreamStoreDir { get; set; } = "./jetstream";

    /// <summary>
    /// Gets or sets the maximum memory JetStream can use. -1 means unlimited.
    /// </summary>
    [JsonPropertyName("jetstreamMaxMemory")]
    public long JetstreamMaxMemory { get; set; } = -1;

    /// <summary>
    /// Gets or sets the maximum disk storage JetStream can use. -1 means unlimited.
    /// </summary>
    [JsonPropertyName("jetstreamMaxStore")]
    public long JetstreamMaxStore { get; set; } = -1;

    // HTTP Monitoring Configuration

    /// <summary>
    /// Gets or sets the HTTP monitoring port. 0 means disabled.
    /// </summary>
    [JsonPropertyName("httpPort")]
    public int HttpPort { get; set; } = 0;

    /// <summary>
    /// Gets or sets the hostname for the HTTP monitoring interface.
    /// </summary>
    [JsonPropertyName("httpHost")]
    public string HttpHost { get; set; } = "0.0.0.0";

    /// <summary>
    /// Gets or sets the HTTPS monitoring port. 0 means disabled.
    /// </summary>
    [JsonPropertyName("httpsPort")]
    public int HttpsPort { get; set; } = 0;

    // Authentication Configuration

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    [JsonPropertyName("auth")]
    public AuthConfiguration Auth { get; set; } = new();

    // Leaf Node Configuration

    /// <summary>
    /// Gets or sets the leaf node configuration for clustering.
    /// </summary>
    [JsonPropertyName("leafNode")]
    public LeafNodeConfiguration LeafNode { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of this configuration instance.
    /// </summary>
    /// <returns>A new BrokerConfiguration instance with the same values.</returns>
    public object Clone()
    {
        return new BrokerConfiguration
        {
            ConfigurationId = Guid.NewGuid(), // New ID for the clone
            CreatedAt = DateTimeOffset.UtcNow,
            Description = Description,
            Host = Host,
            Port = Port,
            MaxPayload = MaxPayload,
            MaxControlLine = MaxControlLine,
            PingInterval = PingInterval,
            MaxPingsOut = MaxPingsOut,
            WriteDeadline = WriteDeadline,
            Debug = Debug,
            Trace = Trace,
            Jetstream = Jetstream,
            JetstreamStoreDir = JetstreamStoreDir,
            JetstreamMaxMemory = JetstreamMaxMemory,
            JetstreamMaxStore = JetstreamMaxStore,
            HttpPort = HttpPort,
            HttpHost = HttpHost,
            HttpsPort = HttpsPort,
            Auth = (AuthConfiguration)Auth.Clone(),
            LeafNode = (LeafNodeConfiguration)LeafNode.Clone()
        };
    }
}

/// <summary>
/// Represents authentication configuration for the broker.
/// </summary>
public class AuthConfiguration : ICloneable
{
    /// <summary>
    /// Gets or sets the username for basic authentication.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for basic authentication.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the list of allowed users.
    /// </summary>
    [JsonPropertyName("allowedUsers")]
    public List<string> AllowedUsers { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of this authentication configuration.
    /// </summary>
    public object Clone()
    {
        return new AuthConfiguration
        {
            Username = Username,
            Password = Password,
            Token = Token,
            AllowedUsers = new List<string>(AllowedUsers)
        };
    }
}

/// <summary>
/// Represents leaf node configuration for clustering.
/// </summary>
public class LeafNodeConfiguration : ICloneable
{
    /// <summary>
    /// Gets or sets the hostname for the leaf node interface.
    /// </summary>
    [JsonPropertyName("host")]
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// Gets or sets the port for the leaf node interface. 0 means disabled.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 0;

    /// <summary>
    /// Gets or sets the list of remote URLs to connect to as a leaf node.
    /// </summary>
    [JsonPropertyName("remoteUrls")]
    public List<string> RemoteUrls { get; set; } = new();

    /// <summary>
    /// Gets or sets the username for leaf node authentication.
    /// </summary>
    [JsonPropertyName("authUsername")]
    public string? AuthUsername { get; set; }

    /// <summary>
    /// Gets or sets the password for leaf node authentication.
    /// </summary>
    [JsonPropertyName("authPassword")]
    public string? AuthPassword { get; set; }

    /// <summary>
    /// Gets or sets the TLS certificate file path for leaf node connections.
    /// </summary>
    [JsonPropertyName("tlsCert")]
    public string? TlsCert { get; set; }

    /// <summary>
    /// Gets or sets the TLS key file path for leaf node connections.
    /// </summary>
    [JsonPropertyName("tlsKey")]
    public string? TlsKey { get; set; }

    /// <summary>
    /// Gets or sets the TLS CA certificate file path for leaf node connections.
    /// </summary>
    [JsonPropertyName("tlsCaCert")]
    public string? TlsCaCert { get; set; }

    /// <summary>
    /// Gets or sets the list of subjects to import from remote leaf nodes.
    /// These subjects will be visible to clients connected to this server.
    /// </summary>
    [JsonPropertyName("importSubjects")]
    public List<string> ImportSubjects { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of subjects to export to remote leaf nodes.
    /// These subjects will be visible to clients on remote servers.
    /// </summary>
    [JsonPropertyName("exportSubjects")]
    public List<string> ExportSubjects { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of this leaf node configuration.
    /// </summary>
    public object Clone()
    {
        return new LeafNodeConfiguration
        {
            Host = Host,
            Port = Port,
            RemoteUrls = new List<string>(RemoteUrls),
            AuthUsername = AuthUsername,
            AuthPassword = AuthPassword,
            TlsCert = TlsCert,
            TlsKey = TlsKey,
            TlsCaCert = TlsCaCert,
            ImportSubjects = new List<string>(ImportSubjects),
            ExportSubjects = new List<string>(ExportSubjects)
        };
    }
}
