using System.Text.Json.Serialization;

namespace DotGnatly.Nats.Bindings;

/// <summary>
/// Server configuration for NATS server.
/// </summary>
public class ServerConfig
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = "localhost";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 4222;

    [JsonPropertyName("max_payload")]
    public long MaxPayload { get; set; } = 1048576; // 1MB

    [JsonPropertyName("max_control_line")]
    public int MaxControlLine { get; set; } = 4096;

    [JsonPropertyName("ping_interval")]
    public int PingInterval { get; set; } = 120; // seconds

    [JsonPropertyName("max_pings_out")]
    public int MaxPingsOut { get; set; } = 2;

    [JsonPropertyName("write_deadline")]
    public int WriteDeadline { get; set; } = 2; // seconds

    [JsonPropertyName("debug")]
    public bool Debug { get; set; } = false;

    [JsonPropertyName("trace")]
    public bool Trace { get; set; } = false;

    [JsonPropertyName("log_file")]
    public string? LogFile { get; set; }

    [JsonPropertyName("log_time_utc")]
    public bool LogTimeUtc { get; set; } = true;

    [JsonPropertyName("log_file_size")]
    public long LogFileSize { get; set; } = 0;

    [JsonPropertyName("jetstream")]
    public bool Jetstream { get; set; } = false;

    [JsonPropertyName("jetstream_store_dir")]
    public string JetstreamStoreDir { get; set; } = "./jetstream";

    [JsonPropertyName("jetstream_max_memory")]
    public long JetstreamMaxMemory { get; set; } = -1; // unlimited

    [JsonPropertyName("jetstream_max_store")]
    public long JetstreamMaxStore { get; set; } = -1; // unlimited

    [JsonPropertyName("jetstream_domain")]
    public string? JetstreamDomain { get; set; }

    [JsonPropertyName("jetstream_unique_tag")]
    public string? JetstreamUniqueTag { get; set; }

    [JsonPropertyName("http_port")]
    public int HTTPPort { get; set; } = 0; // 0 means disabled

    [JsonPropertyName("http_host")]
    public string HTTPHost { get; set; } = "0.0.0.0";

    [JsonPropertyName("https_port")]
    public int HTTPSPort { get; set; } = 0; // 0 means disabled

    [JsonPropertyName("auth")]
    public AuthConfig Auth { get; set; } = new AuthConfig();

    [JsonPropertyName("leaf_node")]
    public LeafNodeConfig LeafNode { get; set; } = new LeafNodeConfig();

    [JsonPropertyName("cluster")]
    public ClusterConfig Cluster { get; set; } = new ClusterConfig();
}

/// <summary>
/// Authentication configuration for NATS server.
/// </summary>
public class AuthConfig
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("allowed_users")]
    public List<string> AllowedUsers { get; set; } = new List<string>();
}

/// <summary>
/// TLS configuration for NATS server.
/// </summary>
public class TLSConfig
{
    [JsonPropertyName("cert_file")]
    public string? CertFile { get; set; }

    [JsonPropertyName("key_file")]
    public string? KeyFile { get; set; }

    [JsonPropertyName("ca_cert_file")]
    public string? CACertFile { get; set; }

    [JsonPropertyName("verify_client_certs")]
    public bool VerifyClientCerts { get; set; } = false;
}

/// <summary>
/// Leaf node configuration for NATS server.
/// </summary>
public class LeafNodeConfig
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = "0.0.0.0";

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("remote_urls")]
    public List<string> RemoteURLs { get; set; } = new List<string>();

    [JsonPropertyName("auth_username")]
    public string? AuthUsername { get; set; }

    [JsonPropertyName("auth_password")]
    public string? AuthPassword { get; set; }

    [JsonPropertyName("tls_cert")]
    public string? TLSCert { get; set; }

    [JsonPropertyName("tls_key")]
    public string? TLSKey { get; set; }

    [JsonPropertyName("tls_ca_cert")]
    public string? TLSCACert { get; set; }

    [JsonPropertyName("import_subjects")]
    public List<string> ImportSubjects { get; set; } = new List<string>();

    [JsonPropertyName("export_subjects")]
    public List<string> ExportSubjects { get; set; } = new List<string>();
}

/// <summary>
/// Cluster configuration for NATS server full mesh clustering.
/// </summary>
public class ClusterConfig
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("host")]
    public string Host { get; set; } = "0.0.0.0";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 0;

    [JsonPropertyName("routes")]
    public List<string> Routes { get; set; } = new List<string>();

    [JsonPropertyName("auth_username")]
    public string? AuthUsername { get; set; }

    [JsonPropertyName("auth_password")]
    public string? AuthPassword { get; set; }

    [JsonPropertyName("auth_token")]
    public string? AuthToken { get; set; }

    [JsonPropertyName("connect_timeout")]
    public int ConnectTimeout { get; set; } = 2;

    [JsonPropertyName("tls_cert")]
    public string? TLSCert { get; set; }

    [JsonPropertyName("tls_key")]
    public string? TLSKey { get; set; }

    [JsonPropertyName("tls_ca_cert")]
    public string? TLSCACert { get; set; }

    [JsonPropertyName("tls_verify")]
    public bool TLSVerify { get; set; } = true;
}

/// <summary>
/// Account configuration for NATS server.
/// </summary>
public class AccountConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("max_connections")]
    public int MaxConnections { get; set; } = -1; // -1 means unlimited

    [JsonPropertyName("max_subscriptions")]
    public int MaxSubscriptions { get; set; } = -1;

    [JsonPropertyName("max_data")]
    public long MaxData { get; set; } = -1;

    [JsonPropertyName("max_payload")]
    public long MaxPayload { get; set; } = -1;
}

/// <summary>
/// User configuration for NATS server.
/// </summary>
public class UserConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("permissions")]
    public Permissions Permissions { get; set; } = new Permissions();
}

/// <summary>
/// Permissions configuration for NATS server users.
/// </summary>
public class Permissions
{
    [JsonPropertyName("publish")]
    public List<string> Publish { get; set; } = new List<string>();

    [JsonPropertyName("subscribe")]
    public List<string> Subscribe { get; set; } = new List<string>();
}
