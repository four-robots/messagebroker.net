using MessageBroker.Core.Configuration;

namespace MessageBroker.Examples.Advanced;

/// <summary>
/// Demonstrates lame duck mode for graceful server shutdown.
/// Lame duck mode stops accepting new connections while allowing existing connections to drain.
/// </summary>
public static class LameDuckModeExample
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Lame Duck Mode Example ===\n");
        Console.ResetColor();

        Console.WriteLine("This example demonstrates lame duck mode, which allows for");
        Console.WriteLine("graceful server shutdown by draining existing connections");
        Console.WriteLine("while refusing new ones.\n");

        try
        {
            Console.WriteLine("Step 1: Starting NATS server...\n");

            // Create a basic configuration
            var config = new BrokerConfiguration
            {
                Host = "localhost",
                Port = 4222,
                Debug = true,
                Description = "Server configured for lame duck mode demonstration"
            };

            // Create and start the broker
            var broker = new MockBrokerController(config);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Server started successfully!");
            Console.ResetColor();

            await Task.Delay(1000);

            // Get initial server information
            Console.WriteLine("\nStep 2: Checking server status...\n");
            var info = await broker.GetInfoAsync();
            Console.WriteLine($"  Client URL: {info.ClientUrl}");
            Console.WriteLine($"  Active Connections: {info.Connections}");
            Console.WriteLine($"  Status: Accepting new connections");

            await Task.Delay(1000);

            // Enter lame duck mode
            Console.WriteLine("\nStep 3: Entering lame duck mode...\n");
            Console.WriteLine("Lame duck mode is useful for:");
            Console.WriteLine("  • Zero-downtime deployments");
            Console.WriteLine("  • Graceful server migrations");
            Console.WriteLine("  • Planned maintenance windows");
            Console.WriteLine("  • Load balancer draining\n");

            await broker.EnterLameDuckModeAsync();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("✓ Server entered lame duck mode!");
            Console.ResetColor();
            Console.WriteLine("\nServer is now:");
            Console.WriteLine("  • Refusing new client connections");
            Console.WriteLine("  • Allowing existing connections to drain");
            Console.WriteLine("  • Ready for graceful shutdown");

            await Task.Delay(2000);

            // Simulate waiting for connections to drain
            Console.WriteLine("\nStep 4: Waiting for connections to drain...");
            for (int i = 3; i > 0; i--)
            {
                Console.WriteLine($"  Draining... {i} seconds remaining");
                await Task.Delay(1000);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ All connections drained!");
            Console.ResetColor();

            await Task.Delay(500);

            // Final shutdown
            Console.WriteLine("\nStep 5: Performing final shutdown...");
            await broker.ShutdownAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ Server shutdown completed gracefully");
            Console.WriteLine("\n✓ Example completed successfully");
            Console.ResetColor();

            Console.WriteLine("\n" + "═".PadRight(60, '═'));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Key Takeaways:");
            Console.ResetColor();
            Console.WriteLine("  1. Lame duck mode enables zero-downtime deployments");
            Console.WriteLine("  2. Existing connections continue to work normally");
            Console.WriteLine("  3. New connections are refused gracefully");
            Console.WriteLine("  4. Ideal for rolling updates and planned maintenance");
            Console.WriteLine("═".PadRight(60, '═'));
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Inner: {ex.InnerException.Message}");
            }
            Console.ResetColor();
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(true);
    }
}
