using DotGnatly.Core.Configuration;

namespace DotGnatly.Examples.Advanced;

/// <summary>
/// Demonstrates the fluent API for configuring the broker.
/// Shows how extension methods make configuration more readable and chainable.
/// </summary>
public static class FluentApiExample
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Fluent API Example ===\n");
        Console.ResetColor();

        try
        {
            Console.WriteLine("Comparing traditional approach vs. fluent API approach...\n");

            await Task.Delay(500);

            // Traditional approach
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TRADITIONAL APPROACH:");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            var broker1 = new MockBrokerController();

            Console.WriteLine("Step 1: Change port to 4223...");
            var result = await broker1.ApplyChangesAsync(config => config.Port = 4223);
            if (result.Success) Console.WriteLine("  ✓ Port changed");

            await Task.Delay(300);

            Console.WriteLine("Step 2: Set max payload to 5MB...");
            result = await broker1.ApplyChangesAsync(config => config.MaxPayload = 5242880);
            if (result.Success) Console.WriteLine("  ✓ Max payload changed");

            await Task.Delay(300);

            Console.WriteLine("Step 3: Enable debug mode...");
            result = await broker1.ApplyChangesAsync(config => config.Debug = true);
            if (result.Success) Console.WriteLine("  ✓ Debug mode enabled");

            await Task.Delay(300);

            Console.WriteLine("Step 4: Enable JetStream...");
            result = await broker1.ApplyChangesAsync(config =>
            {
                config.Jetstream = true;
                config.JetstreamStoreDir = "./data/jetstream";
            });
            if (result.Success) Console.WriteLine("  ✓ JetStream enabled");

            await broker1.ShutdownAsync();
            Console.WriteLine();

            await Task.Delay(1000);

            // Fluent approach
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("FLUENT API APPROACH:");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            Console.WriteLine("Chaining all configuration calls together...\n");

            var broker2 = new MockBrokerController();

            await broker2
                .WithPortAsync(4223)
                .Result
                .WithMaxPayloadAsync(5242880)
                .Result
                .WithDebugAsync(true)
                .Result
                .WithJetStreamAsync("./data/jetstream");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ All configurations applied in a single fluent chain!");
            Console.ResetColor();

            await Task.Delay(1000);
            Console.WriteLine();

            // Show final configuration
            var info = await broker2.GetInfoAsync();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Final Configuration:");
            Console.ResetColor();
            Console.WriteLine($"  Client URL: {info.ClientUrl}");
            Console.WriteLine($"  Port: {broker2.CurrentConfiguration.Port}");
            Console.WriteLine($"  Max Payload: {broker2.CurrentConfiguration.MaxPayload / 1024 / 1024}MB");
            Console.WriteLine($"  Debug: {broker2.CurrentConfiguration.Debug}");
            Console.WriteLine($"  JetStream: {broker2.CurrentConfiguration.Jetstream}");
            Console.WriteLine($"  JetStream Store: {broker2.CurrentConfiguration.JetstreamStoreDir}");

            await broker2.ShutdownAsync();
            Console.WriteLine();

            await Task.Delay(1000);

            // Advanced fluent example with monitoring and auth
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("ADVANCED FLUENT EXAMPLE:");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────");

            Console.WriteLine("Setting up production-ready configuration...\n");

            var broker3 = new MockBrokerController();

            await broker3
                .WithPortAsync(4222)
                .Result
                .WithMaxPayloadAsync(10485760) // 10MB
                .Result
                .WithJetStreamAsync("./production/jetstream")
                .Result
                .WithMonitoringAsync(8222)
                .Result
                .WithAuthenticationAsync("admin", "secure_password")
                .Result
                .WithDebugAsync(false);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Production configuration applied!");
            Console.ResetColor();

            info = await broker3.GetInfoAsync();
            Console.WriteLine("\nProduction Configuration:");
            Console.WriteLine($"  Client URL: {info.ClientUrl}");
            Console.WriteLine($"  HTTP Monitoring Port: {broker3.CurrentConfiguration.HttpPort}");
            Console.WriteLine($"  Authentication: {(string.IsNullOrEmpty(broker3.CurrentConfiguration.Auth.Username) ? "Disabled" : "Enabled")}");
            Console.WriteLine($"  Max Payload: {broker3.CurrentConfiguration.MaxPayload / 1024 / 1024}MB");
            Console.WriteLine($"  JetStream: {broker3.CurrentConfiguration.Jetstream}");

            await broker3.ShutdownAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ Example completed successfully");
            Console.WriteLine("\nKey Takeaway: Fluent API makes configuration more readable and allows method chaining!");
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
