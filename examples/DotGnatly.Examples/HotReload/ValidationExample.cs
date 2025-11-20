using DotGnatly.Core.Configuration;

namespace DotGnatly.Examples.HotReload;

/// <summary>
/// Demonstrates configuration validation that prevents invalid settings from being applied.
/// This ensures system stability by catching configuration errors before they cause problems.
/// </summary>
public static class ValidationExample
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Configuration Validation Example ===\n");
        Console.ResetColor();

        try
        {
            Console.WriteLine("Starting server with valid configuration...\n");

            var broker = new MockBrokerController();
            var info = await broker.GetInfoAsync();

            Console.WriteLine($"Server running on: {info.ClientUrl}");
            Console.WriteLine();

            await Task.Delay(500);

            // Test 1: Invalid port (too low)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TEST 1: Attempting to set port to -1 (invalid)...");
            Console.ResetColor();

            var result = await broker.ApplyChangesAsync(config =>
            {
                config.Port = -1;
                config.Description = "Invalid port configuration";
            });

            if (!result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Validation failed (as expected): {result.ErrorMessage}");
                Console.ResetColor();
            }
            Console.WriteLine();

            await Task.Delay(800);

            // Test 2: Invalid port (too high)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TEST 2: Attempting to set port to 99999 (invalid)...");
            Console.ResetColor();

            result = await broker.ApplyChangesAsync(config =>
            {
                config.Port = 99999;
                config.Description = "Port out of range";
            });

            if (!result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Validation failed (as expected): {result.ErrorMessage}");
                Console.ResetColor();
            }
            Console.WriteLine();

            await Task.Delay(800);

            // Test 3: Invalid max payload (zero)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TEST 3: Attempting to set MaxPayload to 0 (invalid)...");
            Console.ResetColor();

            result = await broker.ApplyChangesAsync(config =>
            {
                config.MaxPayload = 0;
                config.Description = "Zero payload size";
            });

            if (!result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Validation failed (as expected): {result.ErrorMessage}");
                Console.ResetColor();
            }
            Console.WriteLine();

            await Task.Delay(800);

            // Test 4: Invalid max payload (negative)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TEST 4: Attempting to set MaxPayload to -1000 (invalid)...");
            Console.ResetColor();

            result = await broker.ApplyChangesAsync(config =>
            {
                config.MaxPayload = -1000;
                config.Description = "Negative payload size";
            });

            if (!result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Validation failed (as expected): {result.ErrorMessage}");
                Console.ResetColor();
            }
            Console.WriteLine();

            await Task.Delay(800);

            // Test 5: Valid configuration
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TEST 5: Applying valid configuration...");
            Console.ResetColor();

            result = await broker.ApplyChangesAsync(config =>
            {
                config.Port = 4223;
                config.MaxPayload = 2097152; // 2MB
                config.Debug = true;
                config.Description = "Valid configuration update";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Configuration applied successfully! (Version {result.Version?.Version})");
                Console.ResetColor();
                Console.WriteLine($"  Port: {broker.CurrentConfiguration.Port}");
                Console.WriteLine($"  Max Payload: {broker.CurrentConfiguration.MaxPayload / 1024 / 1024}MB");
                Console.WriteLine($"  Debug: {broker.CurrentConfiguration.Debug}");
            }
            Console.WriteLine();

            await Task.Delay(500);

            // Show current configuration status
            info = await broker.GetInfoAsync();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Final Server Status:");
            Console.ResetColor();
            Console.WriteLine($"  Client URL: {info.ClientUrl}");
            Console.WriteLine($"  Configuration is valid and running");
            Console.WriteLine($"  All invalid configurations were rejected");

            await broker.ShutdownAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ Example completed successfully");
            Console.WriteLine("\nKey Takeaway: Validation prevents invalid configurations from breaking your server!");
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
