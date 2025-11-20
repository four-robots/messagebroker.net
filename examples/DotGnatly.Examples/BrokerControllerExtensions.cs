using DotGnatly.Core.Configuration;
using DotGnatly.Core.Interfaces;

namespace DotGnatly.Examples;

/// <summary>
/// Extension methods for IBrokerController to provide a fluent API for common configuration tasks.
/// </summary>
public static class BrokerControllerExtensions
{
    /// <summary>
    /// Changes the server port using a fluent API.
    /// </summary>
    public static async Task<IBrokerController> WithPortAsync(this IBrokerController broker, int port, CancellationToken cancellationToken = default)
    {
        var result = await broker.ApplyChangesAsync(config => config.Port = port, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to set port: {result.ErrorMessage}");
        }
        return broker;
    }

    /// <summary>
    /// Changes the max payload size using a fluent API.
    /// </summary>
    public static async Task<IBrokerController> WithMaxPayloadAsync(this IBrokerController broker, int maxPayloadBytes, CancellationToken cancellationToken = default)
    {
        var result = await broker.ApplyChangesAsync(config => config.MaxPayload = maxPayloadBytes, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to set max payload: {result.ErrorMessage}");
        }
        return broker;
    }

    /// <summary>
    /// Enables or disables debug mode using a fluent API.
    /// </summary>
    public static async Task<IBrokerController> WithDebugAsync(this IBrokerController broker, bool enabled, CancellationToken cancellationToken = default)
    {
        var result = await broker.ApplyChangesAsync(config => config.Debug = enabled, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to set debug mode: {result.ErrorMessage}");
        }
        return broker;
    }

    /// <summary>
    /// Enables JetStream with specified storage directory using a fluent API.
    /// </summary>
    public static async Task<IBrokerController> WithJetStreamAsync(this IBrokerController broker, string storeDir = "./jetstream", CancellationToken cancellationToken = default)
    {
        var result = await broker.ApplyChangesAsync(config =>
        {
            config.Jetstream = true;
            config.JetstreamStoreDir = storeDir;
        }, cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to enable JetStream: {result.ErrorMessage}");
        }
        return broker;
    }

    /// <summary>
    /// Enables HTTP monitoring on the specified port using a fluent API.
    /// </summary>
    public static async Task<IBrokerController> WithMonitoringAsync(this IBrokerController broker, int httpPort, CancellationToken cancellationToken = default)
    {
        var result = await broker.ApplyChangesAsync(config => config.HttpPort = httpPort, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to enable monitoring: {result.ErrorMessage}");
        }
        return broker;
    }

    /// <summary>
    /// Configures basic authentication using a fluent API.
    /// </summary>
    public static async Task<IBrokerController> WithAuthenticationAsync(this IBrokerController broker, string username, string password, CancellationToken cancellationToken = default)
    {
        var result = await broker.ApplyChangesAsync(config =>
        {
            config.Auth.Username = username;
            config.Auth.Password = password;
        }, cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to set authentication: {result.ErrorMessage}");
        }
        return broker;
    }

    /// <summary>
    /// Configures multiple settings at once using a fluent API.
    /// </summary>
    public static async Task<IBrokerController> ConfigureAsync(this IBrokerController broker, Action<BrokerConfiguration> configure, CancellationToken cancellationToken = default)
    {
        var result = await broker.ApplyChangesAsync(configure, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to apply configuration: {result.ErrorMessage}");
        }
        return broker;
    }
}
