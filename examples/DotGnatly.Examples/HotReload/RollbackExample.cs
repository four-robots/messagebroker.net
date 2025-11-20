using DotGnatly.Core.Configuration;

namespace DotGnatly.Examples.HotReload;

/// <summary>
/// Demonstrates configuration rollback to previous versions.
/// This is crucial for recovering from problematic configuration changes.
/// </summary>
public static class RollbackExample
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Configuration Rollback Example ===\n");
        Console.ResetColor();

        try
        {
            Console.WriteLine("Starting server and creating multiple configuration versions...\n");

            // Version 1: Initial configuration
            var initialConfig = new BrokerConfiguration
            {
                Host = "localhost",
                Port = 4222,
                MaxPayload = 1048576, // 1MB
                Debug = false,
                Description = "Version 1: Initial stable configuration"
            };

            var broker = new MockBrokerController(initialConfig);
            var history = await broker.GetHistoryAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Version {history[0].Version}: Initial configuration applied");
            Console.ResetColor();
            Console.WriteLine($"  Port: {initialConfig.Port}");
            Console.WriteLine($"  Max Payload: {initialConfig.MaxPayload / 1024}KB");
            Console.WriteLine();

            await Task.Delay(1000);

            // Version 2: Performance optimization
            Console.WriteLine("Applying Version 2: Performance optimization...");
            var result = await broker.ApplyChangesAsync(config =>
            {
                config.MaxPayload = 5242880; // 5MB
                config.PingInterval = 60;
                config.Description = "Version 2: Performance optimization";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Version {result.Version?.Version}: Performance config applied");
                Console.ResetColor();
                Console.WriteLine($"  Max Payload: {broker.CurrentConfiguration.MaxPayload / 1024 / 1024}MB");
                Console.WriteLine($"  Ping Interval: {broker.CurrentConfiguration.PingInterval}s");
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Version 3: Debug configuration
            Console.WriteLine("Applying Version 3: Debug configuration...");
            result = await broker.ApplyChangesAsync(config =>
            {
                config.Debug = true;
                config.Trace = true;
                config.Port = 4223;
                config.Description = "Version 3: Debug configuration";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Version {result.Version?.Version}: Debug config applied");
                Console.ResetColor();
                Console.WriteLine($"  Port: {broker.CurrentConfiguration.Port}");
                Console.WriteLine($"  Debug: {broker.CurrentConfiguration.Debug}");
                Console.WriteLine($"  Trace: {broker.CurrentConfiguration.Trace}");
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Version 4: Problematic configuration (simulate)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Applying Version 4: Experimental configuration (simulating problem)...");
            Console.ResetColor();

            result = await broker.ApplyChangesAsync(config =>
            {
                config.Port = 4224;
                config.MaxPayload = 10485760; // 10MB - might be too large
                config.WriteDeadline = 1; // Very short deadline - might cause issues
                config.Description = "Version 4: Experimental (problematic)";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Version {result.Version?.Version}: Experimental config applied");
                Console.ResetColor();
                Console.WriteLine("  This configuration is causing performance issues!");
                Console.WriteLine($"  Max Payload: {broker.CurrentConfiguration.MaxPayload / 1024 / 1024}MB");
                Console.WriteLine($"  Write Deadline: {broker.CurrentConfiguration.WriteDeadline}s");
            }
            Console.WriteLine();

            await Task.Delay(1500);

            // Show version history before rollback
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Current Version History:");
            Console.ResetColor();
            history = await broker.GetHistoryAsync();
            for (int i = 0; i < history.Count; i++)
            {
                var v = history[i];
                string marker = i == 0 ? " ← CURRENT" : "";
                Console.WriteLine($"  Version {v.Version}: {v.Configuration.Description}{marker}");
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Rollback to previous version
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("ROLLBACK: Rolling back to previous version...");
            Console.ResetColor();

            result = await broker.RollbackAsync();

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Rollback successful! New version: {result.Version?.Version}");
                Console.ResetColor();
                Console.WriteLine($"  Restored configuration: {broker.CurrentConfiguration.Description}");
                Console.WriteLine($"  Port: {broker.CurrentConfiguration.Port}");
                Console.WriteLine($"  Max Payload: {broker.CurrentConfiguration.MaxPayload / 1024 / 1024}MB");
                Console.WriteLine($"  Write Deadline: {broker.CurrentConfiguration.WriteDeadline}s");
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Rollback to specific version (Version 2)
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("ROLLBACK: Rolling back to Version 2 (Performance optimization)...");
            Console.ResetColor();

            result = await broker.RollbackAsync(toVersion: 2);

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Rollback to Version 2 successful! New version: {result.Version?.Version}");
                Console.ResetColor();
                Console.WriteLine($"  Restored configuration: {broker.CurrentConfiguration.Description}");
                Console.WriteLine($"  Port: {broker.CurrentConfiguration.Port}");
                Console.WriteLine($"  Max Payload: {broker.CurrentConfiguration.MaxPayload / 1024 / 1024}MB");
                Console.WriteLine($"  Debug: {broker.CurrentConfiguration.Debug}");
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Show final version history
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Final Version History (showing rollback versions):");
            Console.ResetColor();
            history = await broker.GetHistoryAsync();
            for (int i = 0; i < history.Count; i++)
            {
                var v = history[i];
                string marker = i == 0 ? " ← CURRENT" : "";
                string typeMarker = v.ChangeType == Core.Configuration.ConfigurationChangeType.Rollback ? " [ROLLBACK]" : "";
                Console.WriteLine($"  Version {v.Version}: {v.Configuration.Description}{typeMarker}{marker}");
            }

            await broker.ShutdownAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ Example completed successfully");
            Console.WriteLine("\nKey Takeaway: Rollback allows you to quickly recover from problematic configurations!");
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
