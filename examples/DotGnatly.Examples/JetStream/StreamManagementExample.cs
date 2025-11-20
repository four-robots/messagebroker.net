using DotGnatly.Core.Configuration;
using DotGnatly.Extensions.JetStream.Extensions;
using DotGnatly.Nats.Implementation;
using NATS.Client.JetStream.Models;

namespace DotGnatly.Examples.JetStream;

/// <summary>
/// Demonstrates how to use the JetStream extension methods to manage streams.
/// </summary>
public class StreamManagementExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== JetStream Stream Management Example ===\n");

        // Create and configure a NATS server with JetStream enabled
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Port = 4222,
            Host = "localhost",
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "dotgnatly-jetstream-example"),
            Debug = true
        };

        Console.WriteLine($"1. Starting NATS server with JetStream...");
        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"   ❌ Failed to start server: {result.ErrorMessage}");
            return;
        }
        Console.WriteLine($"   ✓ Server started on port {config.Port}");
        Console.WriteLine($"   ✓ JetStream storage: {config.JetstreamStoreDir}\n");

        // Wait for server to be ready
        await controller.WaitForReadyAsync(timeoutSeconds: 10);

        try
        {
            // Example 1: Create a simple stream
            Console.WriteLine("2. Creating a simple ORDERS stream...");
            var ordersStream = await controller.CreateStreamAsync("ORDERS", builder =>
            {
                builder
                    .WithSubjects("orders.*")
                    .WithDescription("Order events stream")
                    .WithStorage(StreamConfigStorage.File)
                    .WithRetention(StreamConfigRetention.Limits)
                    .WithMaxMessages(1000)
                    .WithMaxAge(TimeSpan.FromDays(7));
            });

            Console.WriteLine($"   ✓ Stream created: {ordersStream.Config.Name}");
            Console.WriteLine($"   ✓ Subjects: {string.Join(", ", ordersStream.Config.Subjects ?? [])}");
            Console.WriteLine($"   ✓ Storage: {ordersStream.Config.Storage}");
            Console.WriteLine($"   ✓ Max Messages: {ordersStream.Config.MaxMsgs}");
            Console.WriteLine($"   ✓ Max Age: {ordersStream.Config.MaxAge.TotalDays} days\n");

            // Example 2: Create a work queue stream
            Console.WriteLine("3. Creating a TASKS work queue stream...");
            var tasksStream = await controller.CreateStreamAsync("TASKS", builder =>
            {
                builder
                    .WithSubjects("tasks.>")
                    .WithDescription("Task processing work queue")
                    .WithStorage(StreamConfigStorage.File)
                    .WithRetention(StreamConfigRetention.Workqueue)
                    .WithMaxMessages(10000)
                    .WithDiscard(StreamConfigDiscard.Old);
            });

            Console.WriteLine($"   ✓ Stream created: {tasksStream.Config.Name}");
            Console.WriteLine($"   ✓ Retention: {tasksStream.Config.Retention}");
            Console.WriteLine($"   ✓ Discard policy: {tasksStream.Config.Discard}\n");

            // Example 3: Create a memory-based stream for real-time events
            Console.WriteLine("4. Creating an EVENTS memory stream...");
            var eventsStream = await controller.CreateStreamAsync("EVENTS", builder =>
            {
                builder
                    .WithSubjects("events.>")
                    .WithDescription("Real-time event stream")
                    .WithStorage(StreamConfigStorage.Memory)
                    .WithRetention(StreamConfigRetention.Limits)
                    .WithMaxAge(TimeSpan.FromHours(1))
                    .WithMaxBytes(10 * 1024 * 1024); // 10MB
            });

            Console.WriteLine($"   ✓ Stream created: {eventsStream.Config.Name}");
            Console.WriteLine($"   ✓ Storage: {eventsStream.Config.Storage}");
            Console.WriteLine($"   ✓ Max Age: {eventsStream.Config.MaxAge.TotalHours} hours");
            Console.WriteLine($"   ✓ Max Bytes: {eventsStream.Config.MaxBytes / 1024 / 1024}MB\n");

            // Example 4: List all streams
            Console.WriteLine("5. Listing all streams...");
            var streams = await controller.ListStreamsAsync();
            Console.WriteLine($"   ✓ Total streams: {streams.Count}");
            foreach (var streamName in streams)
            {
                Console.WriteLine($"   - {streamName}");
            }
            Console.WriteLine();

            // Example 5: Get stream info
            Console.WriteLine("6. Getting ORDERS stream info...");
            var ordersInfo = await controller.GetStreamAsync("ORDERS");
            Console.WriteLine($"   ✓ Name: {ordersInfo.Config.Name}");
            Console.WriteLine($"   ✓ Messages: {ordersInfo.State.Messages}");
            Console.WriteLine($"   ✓ Bytes: {ordersInfo.State.Bytes}");
            Console.WriteLine($"   ✓ First Sequence: {ordersInfo.State.FirstSeq}");
            Console.WriteLine($"   ✓ Last Sequence: {ordersInfo.State.LastSeq}\n");

            // Example 6: Update a stream
            Console.WriteLine("7. Updating ORDERS stream max messages...");
            var updatedStream = await controller.UpdateStreamAsync("ORDERS", builder =>
            {
                builder
                    .WithSubjects("orders.*")
                    .WithDescription("Order events stream (updated)")
                    .WithStorage(StreamConfigStorage.File)
                    .WithRetention(StreamConfigRetention.Limits)
                    .WithMaxMessages(2000) // Updated from 1000
                    .WithMaxAge(TimeSpan.FromDays(7));
            });

            Console.WriteLine($"   ✓ Stream updated");
            Console.WriteLine($"   ✓ New max messages: {updatedStream.Config.MaxMsgs}\n");

            // Example 7: Delete a stream
            Console.WriteLine("8. Deleting EVENTS stream...");
            var deleted = await controller.DeleteStreamAsync("EVENTS");
            Console.WriteLine($"   ✓ Stream deleted: {deleted}\n");

            // Example 8: List remaining streams
            Console.WriteLine("9. Listing remaining streams...");
            var remainingStreams = await controller.ListStreamsAsync();
            Console.WriteLine($"   ✓ Remaining streams: {remainingStreams.Count}");
            foreach (var streamName in remainingStreams)
            {
                Console.WriteLine($"   - {streamName}");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
        finally
        {
            // Cleanup
            Console.WriteLine("\n10. Shutting down server...");
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
