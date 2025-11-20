using DotGnatly.Core.Configuration;
using DotGnatly.Core.Events;

namespace DotGnatly.Examples.Advanced;

/// <summary>
/// Demonstrates a complete production-ready workflow combining all features:
/// - Server startup with production configuration
/// - Event monitoring
/// - Hot configuration reloads
/// - Validation and error handling
/// - Graceful shutdown
/// </summary>
public static class CompleteWorkflowExample
{
    private static readonly List<string> _eventLog = new();

    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Complete Production Workflow Example ===\n");
        Console.ResetColor();

        try
        {
            Console.WriteLine("This example demonstrates a complete production-ready workflow.\n");

            await Task.Delay(500);

            // Step 1: Create production configuration
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STEP 1: Creating production configuration");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            var productionConfig = new BrokerConfiguration
            {
                Host = "0.0.0.0",
                Port = 4222,
                MaxPayload = 10485760, // 10MB
                MaxControlLine = 4096,
                PingInterval = 120,
                WriteDeadline = 10,
                Debug = false,
                Trace = false,
                Jetstream = true,
                JetstreamStoreDir = "./production/jetstream",
                JetstreamMaxMemory = 1073741824, // 1GB
                JetstreamMaxStore = 10737418240, // 10GB
                HttpPort = 8222, // Monitoring port
                Description = "Production NATS configuration"
            };

            // Add basic authentication
            productionConfig.Auth.Username = "admin";
            productionConfig.Auth.Password = "secure_production_password";

            Console.WriteLine("Production settings:");
            Console.WriteLine($"  ✓ Host: {productionConfig.Host}:{productionConfig.Port}");
            Console.WriteLine($"  ✓ Max Payload: {productionConfig.MaxPayload / 1024 / 1024}MB");
            Console.WriteLine($"  ✓ JetStream: Enabled ({productionConfig.JetstreamStoreDir})");
            Console.WriteLine($"  ✓ JetStream Memory Limit: {productionConfig.JetstreamMaxMemory / 1024 / 1024}MB");
            Console.WriteLine($"  ✓ HTTP Monitoring: Port {productionConfig.HttpPort}");
            Console.WriteLine($"  ✓ Authentication: Enabled");
            Console.WriteLine();

            await Task.Delay(1000);

            // Step 2: Start server with event monitoring
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STEP 2: Starting server with event monitoring");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            var broker = new MockBrokerController(productionConfig);

            // Subscribe to configuration events
            broker.ConfigurationChanging += OnConfigurationChanging;
            broker.ConfigurationChanged += OnConfigurationChanged;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Server started successfully");
            Console.WriteLine("✓ Event monitoring active");
            Console.ResetColor();
            Console.WriteLine();

            await Task.Delay(1000);

            // Step 3: Get server information
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STEP 3: Retrieving server information");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            var info = await broker.GetInfoAsync();
            Console.WriteLine("Server status:");
            Console.WriteLine($"  Client URL: {info.ClientUrl}");
            Console.WriteLine($"  Server ID: {info.ServerId}");
            Console.WriteLine($"  Started: {info.StartedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Connections: {info.Connections}");
            Console.WriteLine($"  Version: {info.Version}");
            Console.WriteLine($"  JetStream: {(info.JetstreamEnabled ? "Active" : "Inactive")}");
            Console.WriteLine();

            await Task.Delay(1000);

            // Step 4: Simulate production traffic - adjust settings
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STEP 4: Adjusting settings for increased traffic (hot reload)");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            var result = await broker.ApplyChangesAsync(config =>
            {
                config.MaxPayload = 20971520; // 20MB
                config.PingInterval = 60; // More frequent pings
                config.Description = "Adjusted for high traffic";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Configuration updated (Version {result.Version?.Version})");
                Console.ResetColor();
                Console.WriteLine($"  Max Payload: {broker.CurrentConfiguration.MaxPayload / 1024 / 1024}MB");
                Console.WriteLine($"  Ping Interval: {broker.CurrentConfiguration.PingInterval}s");
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Step 5: Enable debug for troubleshooting
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STEP 5: Enabling debug mode for troubleshooting");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            result = await broker.ApplyChangesAsync(config =>
            {
                config.Debug = true;
                config.Description = "Debug mode for troubleshooting";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Debug mode enabled (Version {result.Version?.Version})");
                Console.ResetColor();
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Step 6: Attempt invalid configuration (demonstration of validation)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STEP 6: Testing configuration validation");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            result = await broker.ApplyChangesAsync(config =>
            {
                config.Port = -1; // Invalid!
                config.Description = "Invalid configuration test";
            });

            if (!result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Configuration rejected (as expected): {result.ErrorMessage}");
                Console.ResetColor();
                Console.WriteLine("  Server continues running with previous valid configuration");
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Step 7: Turn off debug after troubleshooting
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STEP 7: Disabling debug mode (issue resolved)");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            result = await broker.ApplyChangesAsync(config =>
            {
                config.Debug = false;
                config.Description = "Debug mode disabled";
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Debug mode disabled (Version {result.Version?.Version})");
                Console.ResetColor();
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Step 8: Show configuration history
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STEP 8: Reviewing configuration history");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            var history = await broker.GetHistoryAsync();
            Console.WriteLine($"Configuration versions: {history.Count}");
            Console.WriteLine();

            foreach (var version in history.Take(5))
            {
                string marker = version.Version == history[0].Version ? " ← CURRENT" : "";
                Console.WriteLine($"  Version {version.Version}: {version.Configuration.Description}{marker}");
                Console.WriteLine($"    Applied: {version.AppliedAt:HH:mm:ss}");
                Console.WriteLine($"    Type: {version.ChangeType}");
                Console.WriteLine();
            }

            await Task.Delay(1000);

            // Step 9: Show event log
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STEP 9: Event log summary");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            Console.WriteLine($"Total events captured: {_eventLog.Count}");
            foreach (var logEntry in _eventLog)
            {
                Console.WriteLine($"  {logEntry}");
            }
            Console.WriteLine();

            await Task.Delay(1000);

            // Step 10: Graceful shutdown
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STEP 10: Graceful shutdown");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            Console.WriteLine("Shutting down server...");
            await broker.ShutdownAsync();

            // Unsubscribe from events
            broker.ConfigurationChanging -= OnConfigurationChanging;
            broker.ConfigurationChanged -= OnConfigurationChanged;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Server shutdown completed");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("✓ Complete workflow example finished successfully!");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("This example demonstrated:");
            Console.WriteLine("  • Production-ready server configuration");
            Console.WriteLine("  • JetStream and HTTP monitoring setup");
            Console.WriteLine("  • Hot configuration reloads without downtime");
            Console.WriteLine("  • Real-time event monitoring");
            Console.WriteLine("  • Configuration validation and error handling");
            Console.WriteLine("  • Version history tracking");
            Console.WriteLine("  • Graceful shutdown procedures");
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
        var logEntry = $"[{DateTime.Now:HH:mm:ss}] ConfigurationChanging: {e.Current.Description} → {e.Proposed.Description}";
        _eventLog.Add(logEntry);
    }

    private static void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        var logEntry = $"[{DateTime.Now:HH:mm:ss}] ConfigurationChanged: Version {e.NewVersion.Version} applied ({e.Diff?.Changes.Count ?? 0} changes)";
        _eventLog.Add(logEntry);
    }
}
