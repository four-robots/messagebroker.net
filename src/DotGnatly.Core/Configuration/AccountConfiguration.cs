using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DotGnatly.Core.Configuration;

/// <summary>
/// Represents a NATS account configuration with users, imports, exports, and permissions.
/// </summary>
public class AccountConfiguration
{
    /// <summary>
    /// Gets or sets the account name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether JetStream is enabled for this account.
    /// </summary>
    [JsonPropertyName("jetstream")]
    public bool? Jetstream { get; set; }

    /// <summary>
    /// Gets or sets the list of users in this account.
    /// </summary>
    [JsonPropertyName("users")]
    public List<UserConfiguration> Users { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of stream and service imports.
    /// </summary>
    [JsonPropertyName("imports")]
    public List<ImportExportConfiguration> Imports { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of stream and service exports.
    /// </summary>
    [JsonPropertyName("exports")]
    public List<ImportExportConfiguration> Exports { get; set; } = new();

    /// <summary>
    /// Gets or sets subject mappings for this account.
    /// </summary>
    [JsonPropertyName("mappings")]
    public Dictionary<string, string> Mappings { get; set; } = new();
}

/// <summary>
/// Represents a user configuration within an account.
/// </summary>
public class UserConfiguration
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password (can be bcrypt hash).
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the account this user belongs to.
    /// </summary>
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    /// <summary>
    /// Gets or sets user permissions.
    /// </summary>
    [JsonPropertyName("permissions")]
    public UserPermissions? Permissions { get; set; }
}

/// <summary>
/// Represents user permissions for publish and subscribe operations.
/// </summary>
public class UserPermissions
{
    /// <summary>
    /// Gets or sets the list of subjects this user can publish to.
    /// </summary>
    [JsonPropertyName("publish")]
    public List<string> Publish { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of subjects this user can subscribe to.
    /// </summary>
    [JsonPropertyName("subscribe")]
    public List<string> Subscribe { get; set; } = new();
}

/// <summary>
/// Represents an import or export configuration for streams and services.
/// </summary>
public class ImportExportConfiguration
{
    /// <summary>
    /// Gets or sets the type (stream or service).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "stream" or "service"

    /// <summary>
    /// Gets or sets the subject pattern.
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the source account (for imports).
    /// </summary>
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    /// <summary>
    /// Gets or sets the destination subject (for imports with 'to').
    /// </summary>
    [JsonPropertyName("to")]
    public string? To { get; set; }

    /// <summary>
    /// Gets or sets the response type for service exports.
    /// </summary>
    [JsonPropertyName("responseType")]
    public string? ResponseType { get; set; } // "single", "stream", "chunked"

    /// <summary>
    /// Gets or sets the response threshold (timeout) for services.
    /// </summary>
    [JsonPropertyName("responseThreshold")]
    public string? ResponseThreshold { get; set; }
}
