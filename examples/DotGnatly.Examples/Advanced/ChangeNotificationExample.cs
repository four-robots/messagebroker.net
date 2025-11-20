using DotGnatly.Core.Configuration;
using DotGnatly.Core.Events;

namespace DotGnatly.Examples.Advanced;

/// <summary>
/// Demonstrates the event-driven notification system for configuration changes.
/// Shows how to monitor, react to, and even cancel configuration changes.
/// </summary>
public static class ChangeNotificationExample
{
    private static int _changingEventCount = 0;
    private static int _changedEventCount = 0;

    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Change Notification Example ===\n");
        Console.ResetColor();

        try
        {
            Console.WriteLine("Setting up event handlers for configuration changes...\n");

            var broker = new MockBrokerController();

            // Subscribe to ConfigurationChanging event (before change is applied)
            broker.ConfigurationChanging += OnConfigurationChanging;

            // Subscribe to ConfigurationChanged event (after change is applied)
            broker.ConfigurationChanged += OnConfigurationChanged;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Event handlers registered");
            Console.ResetColor();
            Console.WriteLine("  - ConfigurationChanging (fires BEFORE change)");
            Console.WriteLine("  - ConfigurationChanged (fires AFTER change)");
            Console.WriteLine();

            await Task.Delay(1000);

            // Change 1: Normal change
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("CHANGE #1: Updating port to 4223...");
            Console.ResetColor();

            var result = await broker.ApplyChangesAsync(config =>
            {
                config.Port = 4223;
                config.Description = "Port update to 4223";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Change applied successfully");
                Console.ResetColor();
            }
            Console.WriteLine();

            await Task.Delay(1500);

            // Change 2: Another normal change
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("CHANGE #2: Enabling debug mode...");
            Console.ResetColor();

            result = await broker.ApplyChangesAsync(config =>
            {
                config.Debug = true;
                config.Trace = true;
                config.Description = "Debug mode enabled";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Change applied successfully");
                Console.ResetColor();
            }
            Console.WriteLine();

            await Task.Delay(1500);

            // Change 3: This will be cancelled by the event handler
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("CHANGE #3: Attempting to change port to 9999 (will be cancelled)...");
            Console.ResetColor();

            result = await broker.ApplyChangesAsync(config =>
            {
                config.Port = 9999; // This will trigger our cancellation logic
                config.Description = "Dangerous port change";
            });

            if (!result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Change was cancelled: {result.ErrorMessage}");
                Console.ResetColor();
            }
            Console.WriteLine();

            await Task.Delay(1500);

            // Change 4: Valid change after cancellation
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("CHANGE #4: Updating max payload to 2MB...");
            Console.ResetColor();

            result = await broker.ApplyChangesAsync(config =>
            {
                config.MaxPayload = 2097152; // 2MB
                config.Description = "Increased payload size";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Change applied successfully");
                Console.ResetColor();
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Show statistics
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Event Statistics:");
            Console.ResetColor();
            Console.WriteLine($"  ConfigurationChanging events fired: {_changingEventCount}");
            Console.WriteLine($"  ConfigurationChanged events fired: {_changedEventCount}");
            Console.WriteLine($"  Cancelled changes: {_changingEventCount - _changedEventCount}");

            // Unsubscribe from events
            broker.ConfigurationChanging -= OnConfigurationChanging;
            broker.ConfigurationChanged -= OnConfigurationChanged;

            await broker.ShutdownAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ Example completed successfully");
            Console.WriteLine("\nKey Takeaway: Events allow you to monitor and control configuration changes in real-time!");
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

    private static void OnConfigurationChanging(object? sender, ConfigurationChangingEventArgs e)
    {
        _changingEventCount++;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  → ConfigurationChanging event #{_changingEventCount}");
        Console.ResetColor();

        // Show what's about to change
        Console.WriteLine($"     Old Port: {e.Current.Port}");
        Console.WriteLine($"     New Port: {e.Proposed.Port}");

        // Cancel changes to port 9999 (for demonstration)
        if (e.Proposed.Port == 9999)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"     ⚠ CANCELLING CHANGE: Port 9999 is not allowed!");
            Console.ResetColor();
            e.Cancel = true;
        }
    }

    private static void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        _changedEventCount++;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ ConfigurationChanged event #{_changedEventCount}");
        Console.ResetColor();

        Console.WriteLine($"     Version: {e.NewVersion.Version}");
        Console.WriteLine($"     Description: {e.NewVersion.Configuration.Description}");

        if (e.Diff?.Changes.Any() == true)
        {
            Console.WriteLine($"     Changes: {e.Diff.Changes.Count} properties modified");
            foreach (var change in e.Diff.Changes.Take(2))
            {
                Console.WriteLine($"       - {change.PropertyName}: {change.OldValue} → {change.NewValue}");
            }
        }
    }
}
