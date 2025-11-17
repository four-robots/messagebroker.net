using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DotGnatly.Core.Configuration;

/// <summary>
/// Represents a leaf node remote connection configuration.
/// </summary>
public class LeafNodeRemoteConfiguration
{
    /// <summary>
    /// Gets or sets the list of URLs to connect to.
    /// </summary>
    [JsonPropertyName("urls")]
    public List<string> Urls { get; set; } = new();

    /// <summary>
    /// Gets or sets the account to bind this connection to.
    /// </summary>
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    /// <summary>
    /// Gets or sets credentials for authentication.
    /// </summary>
    [JsonPropertyName("credentials")]
    public string? Credentials { get; set; }

    /// <summary>
    /// Gets or sets the TLS configuration for this remote.
    /// </summary>
    [JsonPropertyName("tls")]
    public TlsConfiguration? Tls { get; set; }

    /// <summary>
    /// Gets or sets the first info timeout.
    /// </summary>
    [JsonPropertyName("firstInfoTimeout")]
    public string? FirstInfoTimeout { get; set; }
}

/// <summary>
/// Represents authorization configuration for leaf nodes.
/// </summary>
public class AuthorizationConfiguration
{
    /// <summary>
    /// Gets or sets the authorization timeout.
    /// </summary>
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the account.
    /// </summary>
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the list of users.
    /// </summary>
    [JsonPropertyName("users")]
    public List<UserConfiguration> Users { get; set; } = new();
}
