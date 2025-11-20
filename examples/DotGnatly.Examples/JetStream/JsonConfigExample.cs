using DotGnatly.Core.Configuration;
using DotGnatly.Extensions.JetStream.Extensions;
using DotGnatly.Extensions.JetStream.Models;
using DotGnatly.Nats.Implementation;

namespace DotGnatly.Examples.JetStream;

/// <summary>
/// Demonstrates how to load and create JetStream streams from JSON configuration files.
/// </summary>
public class JsonConfigExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== JetStream JSON Configuration Example ===\n");

        // Create and configure a NATS server with JetStream enabled
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Port = 4222,
            Host = "localhost",
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "dotgnatly-jetstream-json-example"),
            Debug = false
        };

        Console.WriteLine($"1. Starting NATS server with JetStream...");
        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"   ❌ Failed to start server: {result.ErrorMessage}");
            return;
        }
        Console.WriteLine($"   ✓ Server started on port {config.Port}\n");

        // Wait for server to be ready
        await controller.WaitForReadyAsync(timeoutSeconds: 10);

        try
        {
            // Example 1: Load and create stream from a single JSON file
            Console.WriteLine("2. Creating stream from JSON file (orders.json)...");
            var configsPath = Path.Combine("JetStream", "configs");
            var ordersJsonPath = Path.Combine(configsPath, "orders.json");

            if (File.Exists(ordersJsonPath))
            {
                var ordersStream = await controller.CreateStreamFromFileAsync(ordersJsonPath);
                Console.WriteLine($"   ✓ Stream created: {ordersStream.Config.Name}");
                Console.WriteLine($"   ✓ Description: {ordersStream.Config.Description}");
                Console.WriteLine($"   ✓ Subjects: {string.Join(", ", ordersStream.Config.Subjects ?? [])}");
                Console.WriteLine($"   ✓ Max Messages: {ordersStream.Config.MaxMsgs:N0}");
                Console.WriteLine($"   ✓ Max Bytes: {ordersStream.Config.MaxBytes / 1024 / 1024}MB");
                Console.WriteLine($"   ✓ Storage: {ordersStream.Config.Storage}\n");
            }
            else
            {
                Console.WriteLine($"   ⚠ File not found: {ordersJsonPath}\n");
            }

            // Example 2: Load stream from JSON string
            Console.WriteLine("3. Creating stream from JSON string...");
            var eventsJson = @"{
                ""name"": ""EVENTS"",
                ""description"": ""Real-time events stream"",
                ""subjects"": [""events.>""],
                ""retention"": ""limits"",
                ""max_consumers"": -1,
                ""max_msgs"": -1,
                ""max_bytes"": 10485760,
                ""max_age"": 3600000000000,
                ""max_msg_size"": -1,
                ""storage"": ""memory"",
                ""discard"": ""old"",
                ""num_replicas"": 1,
                ""duplicate_window"": 0,
                ""sealed"": false,
                ""deny_delete"": false,
                ""deny_purge"": false,
                ""allow_rollup_hdrs"": false,
                ""allow_direct"": true
            }";

            var eventsStream = await controller.CreateStreamFromJsonAsync(eventsJson);
            Console.WriteLine($"   ✓ Stream created: {eventsStream.Config.Name}");
            Console.WriteLine($"   ✓ Storage: {eventsStream.Config.Storage}");
            Console.WriteLine($"   ✓ Allow Direct: {eventsStream.Config.AllowDirect}");
            Console.WriteLine($"   ✓ Max Bytes: {eventsStream.Config.MaxBytes / 1024 / 1024}MB\n");

            // Example 3: Load multiple streams from directory
            Console.WriteLine("4. Loading all stream configs from directory...");
            if (Directory.Exists(configsPath))
            {
                var streamInfos = await controller.CreateStreamsFromDirectoryAsync(configsPath);
                Console.WriteLine($"   ✓ Created {streamInfos.Count} streams from directory:");
                foreach (var streamInfo in streamInfos)
                {
                    // Skip ones we already created
                    if (streamInfo.Config.Name == "ORDERS" || streamInfo.Config.Name == "EVENTS")
                        continue;

                    Console.WriteLine($"   - {streamInfo.Config.Name}");
                    if (!string.IsNullOrWhiteSpace(streamInfo.Config.Description))
                    {
                        Console.WriteLine($"     Description: {streamInfo.Config.Description}");
                    }
                    if (streamInfo.Config.Sources?.Count > 0)
                    {
                        Console.WriteLine($"     Sources: {streamInfo.Config.Sources.Count}");
                        foreach (var source in streamInfo.Config.Sources)
                        {
                            Console.WriteLine($"       - {source.Name} (filter: {source.FilterSubject ?? "none"})");
                        }
                    }
                }
                Console.WriteLine();
            }

            // Example 4: Parse JSON and modify configuration before creating
            Console.WriteLine("5. Parsing JSON, modifying config, and creating stream...");
            var tasksJsonPath = Path.Combine(configsPath, "tasks.json");
            if (File.Exists(tasksJsonPath))
            {
                var configJson = await StreamConfigJson.FromFileAsync(tasksJsonPath);
                var builder = configJson.ToBuilder();

                // Modify the configuration using fluent API
                builder
                    .WithMaxMessages(75000) // Changed from 50000
                    .WithDescription("Modified task processing work queue")
                    .WithMaxAge(TimeSpan.FromDays(7)); // Added max age

                await using var context = await controller.GetJetStreamContextAsync();
                var streamConfig = builder.Build();

                // Check if stream already exists and skip if so
                var existingStreams = await controller.ListStreamsAsync();
                if (!existingStreams.Contains("TASKS"))
                {
                    var stream = await context.JetStream.CreateStreamAsync(streamConfig);

                    Console.WriteLine($"   ✓ Stream created: {stream.Info.Config.Name}");
                    Console.WriteLine($"   ✓ Description: {stream.Info.Config.Description}");
                    Console.WriteLine($"   ✓ Max Messages: {stream.Info.Config.MaxMsgs:N0} (modified)");
                    Console.WriteLine($"   ✓ Retention: {stream.Info.Config.Retention}\n");
                }
                else
                {
                    Console.WriteLine($"   ⚠ Stream TASKS already exists (loaded from directory)\n");
                }
            }

            // Example 5: List all created streams
            Console.WriteLine("6. Listing all streams...");
            var streams = await controller.ListStreamsAsync();
            Console.WriteLine($"   ✓ Total streams: {streams.Count}");
            foreach (var streamName in streams)
            {
                var streamInfo = await controller.GetStreamAsync(streamName);
                Console.WriteLine($"   - {streamName}");
                Console.WriteLine($"     Storage: {streamInfo.Config.Storage}, Messages: {streamInfo.State.Messages}");
            }
            Console.WriteLine();

            // Example 6: Demonstrate advanced features from KV_messages
            Console.WriteLine("7. Advanced configuration features (from kv-messages.json)...");
            var kvJsonPath = Path.Combine(configsPath, "kv-messages.json");
            if (File.Exists(kvJsonPath))
            {
                var kvJson = await StreamConfigJson.FromFileAsync(kvJsonPath);
                Console.WriteLine($"   Stream: {kvJson.Name}");
                Console.WriteLine($"   Max Messages Per Subject: {kvJson.MaxMsgsPerSubject}");
                Console.WriteLine($"   Discard Policy: {kvJson.Discard}");
                Console.WriteLine($"   Deny Delete: {kvJson.DenyDelete}");
                Console.WriteLine($"   Allow Rollup Headers: {kvJson.AllowRollupHdrs}");
                Console.WriteLine($"   Allow Direct: {kvJson.AllowDirect}");
                if (kvJson.Sources != null && kvJson.Sources.Count > 0)
                {
                    Console.WriteLine($"   External Sources: {kvJson.Sources.Count}");
                    foreach (var source in kvJson.Sources)
                    {
                        Console.WriteLine($"     - Name: {source.Name}");
                        Console.WriteLine($"       Filter: {source.FilterSubject}");
                        if (source.External != null)
                        {
                            Console.WriteLine($"       External API: {source.External.Api}");
                            Console.WriteLine($"       External Deliver: {source.External.Deliver}");
                        }
                    }
                }
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
        finally
        {
            // Cleanup
            Console.WriteLine("\n8. Shutting down server...");
            await controller.ShutdownAsync();
            Console.WriteLine("   ✓ Server shut down\n");

            // Clean up temp directory
            if (Directory.Exists(config.JetstreamStoreDir))
            {
                try
                {
                    Directory.Delete(config.JetstreamStoreDir, true);
                    Console.WriteLine("   ✓ Cleaned up JetStream storage\n");
                }
                catch
                {
                    Console.WriteLine("   ⚠ Could not clean up JetStream storage directory\n");
                }
            }
        }

        Console.WriteLine("=== Example Complete ===");
    }
}
