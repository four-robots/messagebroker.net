using System.Runtime.InteropServices;

namespace MessageBroker.Nats.Bindings;

/// <summary>
/// Factory for creating the appropriate NATS bindings implementation based on the current platform.
/// </summary>
internal static class NatsBindingsFactory
{
    /// <summary>
    /// Creates the appropriate INatsBindings implementation for the current operating system.
    /// </summary>
    /// <returns>An INatsBindings implementation for the current platform.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not supported.</exception>
    public static INatsBindings Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsNatsBindings();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxNatsBindings();
        }

        throw new PlatformNotSupportedException(
            $"NATS bindings are not supported on the current platform: {RuntimeInformation.OSDescription}. " +
            "Supported platforms are Windows and Linux.");
    }
}
