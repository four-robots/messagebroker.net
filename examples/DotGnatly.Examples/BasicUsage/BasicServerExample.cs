using DotGnatly.Core.Configuration;

namespace DotGnatly.Examples.BasicUsage;

/// <summary>
/// Demonstrates basic server startup, configuration, and shutdown operations.
/// </summary>
public static class BasicServerExample
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Basic Server Startup Example ===\n");
        Console.ResetColor();

        try
        {
            Console.WriteLine("Creating a new NATS broker with default configuration...\n");

            // Create a basic configuration
            var config = new BrokerConfiguration
            {
                Host = "localhost",
                Port = 4222,
                MaxPayload = 1048576, // 1MB
                Debug = true,
                Description = "Basic NATS server configuration"
            };

            Console.WriteLine("Configuration:");
            Console.WriteLine($"  Host: {config.Host}");
            Console.WriteLine($"  Port: {config.Port}");
            Console.WriteLine($"  Max Payload: {config.MaxPayload / 1024}KB");
            Console.WriteLine($"  Debug: {config.Debug}");
            Console.WriteLine();

            // Create and start the broker
            var broker = new MockBrokerController(config);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server started successfully!");
            Console.ResetColor();

            await Task.Delay(500);

            // Get server information
            Console.WriteLine("\nFetching server information...");
            var info = await broker.GetInfoAsync();

            Console.WriteLine("\nServer Information:");
            Console.WriteLine($"  Client URL: {info.ClientUrl}");
            Console.WriteLine($"  Server ID: {info.ServerId}");
            Console.WriteLine($"  Active Connections: {info.Connections}");
            Console.WriteLine($"  Started At: {info.StartedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  NATS Version: {info.Version}");
            Console.WriteLine($"  JetStream Enabled: {info.JetstreamEnabled}");

            await Task.Delay(500);

            // Graceful shutdown
            Console.WriteLine("\nShutting down server gracefully...");
            await broker.ShutdownAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nServer shutdown completed.");
            Console.WriteLine("\n✓ Example completed successfully");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(true);
    }
}
