using MessageBroker.Core.Configuration;
using MessageBroker.Core.Interfaces;

namespace MessageBroker.Core.Validation;

/// <summary>
/// Default implementation of configuration validation.
/// Validates broker configurations for correctness and consistency.
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private const int MinPort = 1;
    private const int MaxPort = 65535;
    private const int MinMaxPayload = 1;
    private const int MaxMaxPayload = 10485760; // 10MB

    /// <summary>
    /// Validates a broker configuration for correctness and consistency.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>A validation result containing any errors or warnings found.</returns>
    public ValidationResult Validate(BrokerConfiguration config)
    {
        var result = new ValidationResult();

        if (config == null)
        {
            result.AddError("Configuration", "Configuration cannot be null");
            return result;
        }

        // Validate Host
        if (string.IsNullOrWhiteSpace(config.Host))
        {
            result.AddError(nameof(config.Host), "Host cannot be empty");
        }

        // Validate Port
        if (config.Port < MinPort || config.Port > MaxPort)
        {
            result.AddError(nameof(config.Port), $"Port must be between {MinPort} and {MaxPort}");
        }

        // Validate MaxPayload
        if (config.MaxPayload <= 0)
        {
            result.AddError(nameof(config.MaxPayload), "MaxPayload must be greater than 0");
        }
        else if (config.MaxPayload > MaxMaxPayload)
        {
            result.AddError(nameof(config.MaxPayload),
                $"MaxPayload should not exceed {MaxMaxPayload} bytes ({MaxMaxPayload / 1024 / 1024}MB)",
                ValidationSeverity.Warning);
        }

        // Validate MaxControlLine
        if (config.MaxControlLine <= 0)
        {
            result.AddError(nameof(config.MaxControlLine), "MaxControlLine must be greater than 0");
        }

        // Validate PingInterval
        if (config.PingInterval < 0)
        {
            result.AddError(nameof(config.PingInterval), "PingInterval cannot be negative");
        }

        // Validate MaxPingsOut
        if (config.MaxPingsOut <= 0)
        {
            result.AddError(nameof(config.MaxPingsOut), "MaxPingsOut must be greater than 0");
        }

        // Validate WriteDeadline
        if (config.WriteDeadline <= 0)
        {
            result.AddError(nameof(config.WriteDeadline), "WriteDeadline must be greater than 0");
        }

        // Validate JetStream configuration
        if (config.Jetstream)
        {
            if (string.IsNullOrWhiteSpace(config.JetstreamStoreDir))
            {
                result.AddError(nameof(config.JetstreamStoreDir),
                    "JetstreamStoreDir must be specified when JetStream is enabled");
            }

            if (config.JetstreamMaxMemory == 0)
            {
                result.AddError(nameof(config.JetstreamMaxMemory),
                    "JetstreamMaxMemory cannot be 0 (use -1 for unlimited)");
            }

            if (config.JetstreamMaxStore == 0)
            {
                result.AddError(nameof(config.JetstreamMaxStore),
                    "JetstreamMaxStore cannot be 0 (use -1 for unlimited)");
            }
        }

        // Validate HTTP monitoring ports
        if (config.HttpPort != 0)
        {
            if (config.HttpPort < MinPort || config.HttpPort > MaxPort)
            {
                result.AddError(nameof(config.HttpPort), $"HttpPort must be between {MinPort} and {MaxPort} or 0 to disable");
            }

            if (config.HttpPort == config.Port)
            {
                result.AddError(nameof(config.HttpPort), "HttpPort cannot be the same as the main server Port");
            }
        }

        if (config.HttpsPort != 0)
        {
            if (config.HttpsPort < MinPort || config.HttpsPort > MaxPort)
            {
                result.AddError(nameof(config.HttpsPort), $"HttpsPort must be between {MinPort} and {MaxPort} or 0 to disable");
            }

            if (config.HttpsPort == config.Port)
            {
                result.AddError(nameof(config.HttpsPort), "HttpsPort cannot be the same as the main server Port");
            }

            if (config.HttpsPort == config.HttpPort && config.HttpPort != 0)
            {
                result.AddError(nameof(config.HttpsPort), "HttpsPort cannot be the same as HttpPort");
            }
        }

        // Validate authentication
        ValidateAuth(config.Auth, result);

        // Validate leaf node configuration
        ValidateLeafNode(config.LeafNode, config.Port, result);

        // Check for trace without debug
        if (config.Trace && !config.Debug)
        {
            result.AddError(nameof(config.Trace),
                "Trace logging is typically only useful when Debug is also enabled",
                ValidationSeverity.Warning);
        }

        return result;
    }

    /// <summary>
    /// Validates that changes from one configuration to another are safe and valid.
    /// </summary>
    /// <param name="current">The current configuration.</param>
    /// <param name="proposed">The proposed new configuration.</param>
    /// <returns>A validation result containing any errors or warnings about the proposed changes.</returns>
    public ValidationResult ValidateChanges(BrokerConfiguration current, BrokerConfiguration proposed)
    {
        // First validate the proposed configuration itself
        var result = Validate(proposed);

        if (current == null || proposed == null)
        {
            return result;
        }

        // Check for potentially disruptive changes
        if (current.Port != proposed.Port)
        {
            result.AddError("Port",
                "Changing the server port requires a restart and will disconnect all clients",
                ValidationSeverity.Warning);
        }

        if (current.Host != proposed.Host)
        {
            result.AddError("Host",
                "Changing the server host requires a restart and will disconnect all clients",
                ValidationSeverity.Warning);
        }

        // Check for JetStream changes that might be problematic
        if (current.Jetstream != proposed.Jetstream)
        {
            if (proposed.Jetstream)
            {
                result.AddError(nameof(proposed.Jetstream),
                    "Enabling JetStream after startup may require server restart",
                    ValidationSeverity.Warning);
            }
            else
            {
                result.AddError(nameof(proposed.Jetstream),
                    "Disabling JetStream will cause loss of all JetStream streams and consumers",
                    ValidationSeverity.Warning);
            }
        }

        if (current.Jetstream && proposed.Jetstream &&
            current.JetstreamStoreDir != proposed.JetstreamStoreDir)
        {
            result.AddError(nameof(proposed.JetstreamStoreDir),
                "Changing JetStream store directory may cause loss of existing data",
                ValidationSeverity.Warning);
        }

        // Check for significant MaxPayload changes
        if (current.MaxPayload != proposed.MaxPayload)
        {
            var changePercent = Math.Abs((proposed.MaxPayload - current.MaxPayload) / (double)current.MaxPayload) * 100;
            if (changePercent > 50)
            {
                result.AddError(nameof(proposed.MaxPayload),
                    $"MaxPayload is changing by {changePercent:F0}%, which may affect existing clients",
                    ValidationSeverity.Warning);
            }
        }

        return result;
    }

    private void ValidateAuth(AuthConfiguration auth, ValidationResult result)
    {
        if (auth == null)
            return;

        var hasUsername = !string.IsNullOrWhiteSpace(auth.Username);
        var hasPassword = !string.IsNullOrWhiteSpace(auth.Password);
        var hasToken = !string.IsNullOrWhiteSpace(auth.Token);

        if (hasUsername && !hasPassword)
        {
            result.AddError("Auth.Username", "Username is set but Password is not");
        }

        if (!hasUsername && hasPassword)
        {
            result.AddError("Auth.Password", "Password is set but Username is not");
        }

        if ((hasUsername || hasPassword) && hasToken)
        {
            result.AddError("Auth", "Cannot use both username/password and token authentication simultaneously");
        }
    }

    private void ValidateLeafNode(LeafNodeConfiguration leafNode, int mainPort, ValidationResult result)
    {
        if (leafNode == null || leafNode.Port == 0)
            return;

        if (leafNode.Port < MinPort || leafNode.Port > MaxPort)
        {
            result.AddError("LeafNode.Port", $"LeafNode port must be between {MinPort} and {MaxPort} or 0 to disable");
        }

        if (leafNode.Port == mainPort)
        {
            result.AddError("LeafNode.Port", "LeafNode port cannot be the same as the main server port");
        }

        // Check for TLS certificate consistency
        var hasCert = !string.IsNullOrWhiteSpace(leafNode.TlsCert);
        var hasKey = !string.IsNullOrWhiteSpace(leafNode.TlsKey);

        if (hasCert && !hasKey)
        {
            result.AddError("LeafNode.TlsCert", "TLS certificate is specified but key is not");
        }

        if (!hasCert && hasKey)
        {
            result.AddError("LeafNode.TlsKey", "TLS key is specified but certificate is not");
        }

        // Check for auth consistency
        var hasUsername = !string.IsNullOrWhiteSpace(leafNode.AuthUsername);
        var hasPassword = !string.IsNullOrWhiteSpace(leafNode.AuthPassword);

        if (hasUsername && !hasPassword)
        {
            result.AddError("LeafNode.AuthUsername", "LeafNode username is set but password is not");
        }

        if (!hasUsername && hasPassword)
        {
            result.AddError("LeafNode.AuthPassword", "LeafNode password is set but username is not");
        }
    }
}
