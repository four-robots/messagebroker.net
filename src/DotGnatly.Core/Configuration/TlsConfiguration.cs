using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DotGnatly.Core.Configuration;

/// <summary>
/// Represents TLS configuration for secure connections.
/// </summary>
public class TlsConfiguration
{
    /// <summary>
    /// Gets or sets the certificate file path.
    /// </summary>
    [JsonPropertyName("certFile")]
    public string? CertFile { get; set; }

    /// <summary>
    /// Gets or sets the key file path.
    /// </summary>
    [JsonPropertyName("keyFile")]
    public string? KeyFile { get; set; }

    /// <summary>
    /// Gets or sets the CA certificate file path.
    /// </summary>
    [JsonPropertyName("caCertFile")]
    public string? CaCertFile { get; set; }

    /// <summary>
    /// Gets or sets whether to verify client certificates.
    /// </summary>
    [JsonPropertyName("verifyClientCerts")]
    public bool VerifyClientCerts { get; set; }

    /// <summary>
    /// Gets or sets the TLS timeout in seconds.
    /// </summary>
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    /// <summary>
    /// Gets or sets whether handshake should occur first.
    /// </summary>
    [JsonPropertyName("handshakeFirst")]
    public bool? HandshakeFirst { get; set; }

    /// <summary>
    /// Gets or sets whether to skip certificate verification (insecure).
    /// </summary>
    [JsonPropertyName("insecure")]
    public bool? Insecure { get; set; }

    /// <summary>
    /// Gets or sets the certificate store to use (Windows).
    /// </summary>
    [JsonPropertyName("certStore")]
    public string? CertStore { get; set; }

    /// <summary>
    /// Gets or sets how to match certificates.
    /// </summary>
    [JsonPropertyName("certMatchBy")]
    public string? CertMatchBy { get; set; }

    /// <summary>
    /// Gets or sets the certificate match pattern.
    /// </summary>
    [JsonPropertyName("certMatch")]
    public string? CertMatch { get; set; }

    /// <summary>
    /// Gets or sets pinned certificate fingerprints.
    /// </summary>
    [JsonPropertyName("pinnedCerts")]
    public List<string> PinnedCerts { get; set; } = new();
}
