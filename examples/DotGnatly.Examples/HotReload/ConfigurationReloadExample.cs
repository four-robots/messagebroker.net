using DotGnatly.Core.Configuration;

namespace DotGnatly.Examples.HotReload;

/// <summary>
/// Demonstrates hot reloading of server configuration without downtime.
/// This is a key feature that MessageBroker.NET adds over the original nats-csharp implementation.
/// </summary>
public static class ConfigurationReloadExample
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Hot Configuration Reload Example ===\n");
        Console.ResetColor();

        try
        {
            Console.WriteLine("Starting server with initial configuration...\n");

            // Create initial configuration
            var initialConfig = new BrokerConfiguration
            {
                Host = "localhost",
                Port = 4222,
                MaxPayload = 1048576, // 1MB
                Debug = false,
                Description = "Initial configuration"
            };

            var broker = new MockBrokerController(initialConfig);
            var history = await broker.GetHistoryAsync();

            Console.WriteLine("Initial Configuration:");
            Console.WriteLine($"  Port: {initialConfig.Port}");
            Console.WriteLine($"  Max Payload: {initialConfig.MaxPayload / 1024}KB");
            Console.WriteLine($"  Debug: {initialConfig.Debug}");
            Console.WriteLine($"  Version: {history[0].Version}");
            Console.WriteLine();

            await Task.Delay(1000);

            // Hot reload - Change port
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("HOT RELOAD #1: Changing port from 4222 to 4223...");
            Console.ResetColor();

            var result = await broker.ApplyChangesAsync(config =>
            {
                config.Port = 4223;
                config.Description = "Updated port configuration";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Port change successful! (Version {result.Version?.Version})");
                Console.ResetColor();
                Console.WriteLine($"  Changes: {result.Diff?.Changes.Count ?? 0} properties modified");
                if (result.Diff?.Changes.Any() == true)
                {
                    foreach (var change in result.Diff.Changes.Take(3))
                    {
                        Console.WriteLine($"    - {change.PropertyName}: {change.OldValue} → {change.NewValue}");
                    }
                }
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Hot reload - Change max payload
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("HOT RELOAD #2: Increasing max payload to 5MB...");
            Console.ResetColor();

            result = await broker.ApplyChangesAsync(config =>
            {
                config.MaxPayload = 5242880; // 5MB
                config.Description = "Increased payload size";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Max payload change successful! (Version {result.Version?.Version})");
                Console.ResetColor();
                Console.WriteLine($"  New max payload: {broker.CurrentConfiguration.MaxPayload / 1024 / 1024}MB");
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Hot reload - Enable debug mode
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("HOT RELOAD #3: Enabling debug and trace logging...");
            Console.ResetColor();

            result = await broker.ApplyChangesAsync(config =>
            {
                config.Debug = true;
                config.Trace = true;
                config.Description = "Enabled debug logging";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Debug settings updated! (Version {result.Version?.Version})");
                Console.ResetColor();
                Console.WriteLine($"  Debug: {broker.CurrentConfiguration.Debug}");
                Console.WriteLine($"  Trace: {broker.CurrentConfiguration.Trace}");
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Show version history
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Configuration Version History:");
            Console.ResetColor();

            history = await broker.GetHistoryAsync();
            foreach (var version in history)
            {
                Console.WriteLine($"  Version {version.Version}: {version.Configuration.Description}");
                Console.WriteLine($"    Applied: {version.AppliedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"    Type: {version.ChangeType}");
                Console.WriteLine();
            }

            // Get final server info
            var info = await broker.GetInfoAsync();
            Console.WriteLine("Final Server State:");
            Console.WriteLine($"  Client URL: {info.ClientUrl}");
            Console.WriteLine($"  Port: {broker.CurrentConfiguration.Port}");
            Console.WriteLine($"  Max Payload: {broker.CurrentConfiguration.MaxPayload / 1024 / 1024}MB");
            Console.WriteLine($"  Debug: {broker.CurrentConfiguration.Debug}");

            await broker.ShutdownAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ Example completed successfully");
            Console.WriteLine("\nKey Takeaway: Configuration changes were applied WITHOUT restarting the server!");
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
