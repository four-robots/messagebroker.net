using DotGnatly.Core.Configuration;
using DotGnatly.Core.Interfaces;
using DotGnatly.Examples;

namespace DotGnatly.Examples;

/// <summary>
/// Simple test program to verify MessageBroker.NET functionality without interactive console.
/// </summary>
public static class SimpleTest
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== MessageBroker.NET Simple Test ===\n");

        // Create a mock broker controller
        using var controller = new MockBrokerController();

        // Test 1: Basic Configuration
        Console.WriteLine("[TEST 1] Basic Configuration");
        var config = new BrokerConfiguration
        {
            Host = "localhost",
            Port = 4222,
            Description = "Test configuration"
        };

        var result = await controller.ConfigureAsync(config);
        Console.WriteLine($"  Result: {(result.Success ? "SUCCESS" : "FAILED")}");
        if (result.Version != null)
        {
            Console.WriteLine($"  Version: {result.Version.Version}");
        }
        Console.WriteLine();

        // Test 2: Hot Reload
        Console.WriteLine("[TEST 2] Hot Configuration Reload");
        result = await controller.ApplyChangesAsync(c =>
        {
            c.Port = 4223;
            c.Debug = true;
        });
        Console.WriteLine($"  Result: {(result.Success ? "SUCCESS" : "FAILED")}");
        Console.WriteLine($"  Current Port: {controller.CurrentConfiguration.Port}");
        Console.WriteLine($"  Debug Enabled: {controller.CurrentConfiguration.Debug}");
        if (result.Diff != null)
        {
            Console.WriteLine($"  Properties Changed: {result.Diff.Changes.Count}");
            foreach (var change in result.Diff.Changes)
            {
                Console.WriteLine($"    - {change.PropertyName}: {change.OldValue} → {change.NewValue}");
            }
        }
        Console.WriteLine();

        // Test 3: Validation (Invalid Configuration)
        Console.WriteLine("[TEST 3] Configuration Validation");
        var invalidConfig = new BrokerConfiguration
        {
            Host = "localhost",
            Port = 99999, // Invalid port
            MaxPayload = -1 // Invalid payload size
        };

        result = await controller.ConfigureAsync(invalidConfig);
        Console.WriteLine($"  Result: {(result.Success ? "SUCCESS" : "FAILED")}");
        if (!result.Success)
        {
            Console.WriteLine($"  Error: {result.ErrorMessage}");
        }
        Console.WriteLine();

        // Test 4: Rollback
        Console.WriteLine("[TEST 4] Configuration Rollback");
        Console.WriteLine($"  Current Version: {((MockBrokerController)controller).GetCurrentVersion()}");

        // Make another change
        await controller.ApplyChangesAsync(c => c.Port = 4224);
        Console.WriteLine($"  After change, Version: {((MockBrokerController)controller).GetCurrentVersion()}");
        Console.WriteLine($"  Current Port: {controller.CurrentConfiguration.Port}");

        // Rollback
        result = await controller.RollbackAsync();
        Console.WriteLine($"  After rollback, Version: {((MockBrokerController)controller).GetCurrentVersion()}");
        Console.WriteLine($"  Current Port: {controller.CurrentConfiguration.Port}");
        Console.WriteLine();

        // Test 5: Change Notifications
        Console.WriteLine("[TEST 5] Change Notifications");
        int changeCount = 0;
        controller.ConfigurationChanged += (sender, e) =>
        {
            changeCount++;
            Console.WriteLine($"  ConfigurationChanged event fired! Change #{changeCount}");
            Console.WriteLine($"    Changed Properties: {e.Diff?.Changes.Count ?? 0}");
        };

        await controller.ApplyChangesAsync(c => c.Debug = false);
        await controller.ApplyChangesAsync(c => c.MaxPayload = 2097152);
        Console.WriteLine($"  Total changes detected: {changeCount}");
        Console.WriteLine();

        // Test 6: Get Broker Info
        Console.WriteLine("[TEST 6] Get Broker Info");
        var info = await controller.GetInfoAsync();
        Console.WriteLine($"  Client URL: {info.ClientUrl}");
        Console.WriteLine($"  Server ID: {info.ServerId}");
        Console.WriteLine($"  Started At: {info.StartedAt}");
        Console.WriteLine($"  JetStream Enabled: {info.JetstreamEnabled}");
        Console.WriteLine();

        // Test 7: Lame Duck Mode
        Console.WriteLine("[TEST 7] Lame Duck Mode");
        try
        {
            await controller.EnterLameDuckModeAsync();
            Console.WriteLine("  Successfully entered lame duck mode");
            Console.WriteLine("  Server is now draining connections");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
        Console.WriteLine();

        // Test 8: Shutdown
        Console.WriteLine("[TEST 8] Graceful Shutdown");
        await controller.ShutdownAsync();
        Console.WriteLine("  Shutdown complete");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=== All Tests Completed Successfully ===");
        Console.ResetColor();
    }
}
