using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Bindings;

namespace MessageBroker.Nats.Implementation;

/// <summary>
/// Provides bidirectional mapping between Core BrokerConfiguration and Bindings ServerConfig models.
/// Handles conversion of property names and nested configuration objects.
/// </summary>
public static class ConfigurationMapper
{
    /// <summary>
    /// Maps a BrokerConfiguration from the Core layer to a ServerConfig for the Bindings layer.
    /// </summary>
    /// <param name="config">The BrokerConfiguration to map.</param>
    /// <returns>A ServerConfig instance with the same configuration values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    public static ServerConfig MapToServerConfig(BrokerConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        return new ServerConfig
        {
            // Core server settings
            Host = config.Host,
            Port = config.Port,
            MaxPayload = config.MaxPayload,
            MaxControlLine = config.MaxControlLine,
            PingInterval = config.PingInterval,
            MaxPingsOut = config.MaxPingsOut,
            WriteDeadline = config.WriteDeadline,
            Debug = config.Debug,
            Trace = config.Trace,

            // JetStream settings
            Jetstream = config.Jetstream,
            JetstreamStoreDir = config.JetstreamStoreDir,
            JetstreamMaxMemory = config.JetstreamMaxMemory,
            JetstreamMaxStore = config.JetstreamMaxStore,

            // HTTP monitoring settings
            HTTPPort = config.HttpPort,
            HTTPHost = config.HttpHost,
            HTTPSPort = config.HttpsPort,

            // Authentication configuration
            Auth = MapToAuthConfig(config.Auth),

            // Leaf node configuration
            LeafNode = MapToLeafNodeConfig(config.LeafNode)
        };
    }

    /// <summary>
    /// Maps a ServerConfig from the Bindings layer to a BrokerConfiguration for the Core layer.
    /// </summary>
    /// <param name="config">The ServerConfig to map.</param>
    /// <returns>A BrokerConfiguration instance with the same configuration values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    public static BrokerConfiguration MapToBrokerConfiguration(ServerConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        return new BrokerConfiguration
        {
            // Core server settings
            Host = config.Host,
            Port = config.Port,
            MaxPayload = config.MaxPayload,
            MaxControlLine = config.MaxControlLine,
            PingInterval = config.PingInterval,
            MaxPingsOut = config.MaxPingsOut,
            WriteDeadline = config.WriteDeadline,
            Debug = config.Debug,
            Trace = config.Trace,

            // JetStream settings
            Jetstream = config.Jetstream,
            JetstreamStoreDir = config.JetstreamStoreDir,
            JetstreamMaxMemory = config.JetstreamMaxMemory,
            JetstreamMaxStore = config.JetstreamMaxStore,

            // HTTP monitoring settings
            HttpPort = config.HTTPPort,
            HttpHost = config.HTTPHost,
            HttpsPort = config.HTTPSPort,

            // Authentication configuration
            Auth = MapToAuthConfiguration(config.Auth),

            // Leaf node configuration
            LeafNode = MapToLeafNodeConfiguration(config.LeafNode)
        };
    }

    /// <summary>
    /// Maps an AuthConfiguration to an AuthConfig.
    /// </summary>
    /// <param name="auth">The AuthConfiguration to map.</param>
    /// <returns>An AuthConfig instance with the same values.</returns>
    private static AuthConfig MapToAuthConfig(AuthConfiguration auth)
    {
        if (auth == null)
        {
            return new AuthConfig();
        }

        return new AuthConfig
        {
            Username = auth.Username,
            Password = auth.Password,
            Token = auth.Token,
            AllowedUsers = new List<string>(auth.AllowedUsers)
        };
    }

    /// <summary>
    /// Maps an AuthConfig to an AuthConfiguration.
    /// </summary>
    /// <param name="auth">The AuthConfig to map.</param>
    /// <returns>An AuthConfiguration instance with the same values.</returns>
    private static AuthConfiguration MapToAuthConfiguration(AuthConfig auth)
    {
        if (auth == null)
        {
            return new AuthConfiguration();
        }

        return new AuthConfiguration
        {
            Username = auth.Username,
            Password = auth.Password,
            Token = auth.Token,
            AllowedUsers = new List<string>(auth.AllowedUsers)
        };
    }

    /// <summary>
    /// Maps a LeafNodeConfiguration to a LeafNodeConfig.
    /// </summary>
    /// <param name="leafNode">The LeafNodeConfiguration to map.</param>
    /// <returns>A LeafNodeConfig instance with the same values.</returns>
    private static LeafNodeConfig MapToLeafNodeConfig(LeafNodeConfiguration leafNode)
    {
        if (leafNode == null)
        {
            return new LeafNodeConfig();
        }

        return new LeafNodeConfig
        {
            Host = leafNode.Host,
            Port = leafNode.Port,
            RemoteURLs = new List<string>(leafNode.RemoteUrls),
            AuthUsername = leafNode.AuthUsername,
            AuthPassword = leafNode.AuthPassword,
            TLSCert = leafNode.TlsCert,
            TLSKey = leafNode.TlsKey,
            TLSCACert = leafNode.TlsCaCert,
            ImportSubjects = new List<string>(leafNode.ImportSubjects),
            ExportSubjects = new List<string>(leafNode.ExportSubjects)
        };
    }

    /// <summary>
    /// Maps a LeafNodeConfig to a LeafNodeConfiguration.
    /// </summary>
    /// <param name="leafNode">The LeafNodeConfig to map.</param>
    /// <returns>A LeafNodeConfiguration instance with the same values.</returns>
    private static LeafNodeConfiguration MapToLeafNodeConfiguration(LeafNodeConfig leafNode)
    {
        if (leafNode == null)
        {
            return new LeafNodeConfiguration();
        }

        return new LeafNodeConfiguration
        {
            Host = leafNode.Host,
            Port = leafNode.Port,
            RemoteUrls = new List<string>(leafNode.RemoteURLs),
            AuthUsername = leafNode.AuthUsername,
            AuthPassword = leafNode.AuthPassword,
            TlsCert = leafNode.TLSCert,
            TlsKey = leafNode.TLSKey,
            TlsCaCert = leafNode.TLSCACert,
            ImportSubjects = new List<string>(leafNode.ImportSubjects),
            ExportSubjects = new List<string>(leafNode.ExportSubjects)
        };
    }
}
