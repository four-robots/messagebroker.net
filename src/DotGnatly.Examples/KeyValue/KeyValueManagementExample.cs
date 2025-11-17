using DotGnatly.Core.Configuration;
using DotGnatly.Extensions.JetStream.Extensions;
using DotGnatly.Nats.Implementation;
using NATS.Client.KeyValueStore;

namespace DotGnatly.Examples.KeyValue;

/// <summary>
/// Demonstrates how to use the Key-Value extension methods to manage KV stores.
/// </summary>
public class KeyValueManagementExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== JetStream Key-Value Management Example ===\n");

        // Create and configure a NATS server with JetStream enabled
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Port = 4222,
            Host = "localhost",
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "dotgnatly-kv-example"),
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
            // Example 1: Create a simple key-value store
            Console.WriteLine("2. Creating a simple USER_PROFILES KV store...");
            var userProfiles = await controller.CreateKeyValueAsync("USER_PROFILES", builder =>
            {
                builder
                    .WithDescription("User profile storage")
                    .WithMaxHistoryPerKey(1)
                    .WithStorage(NatsKVStorageType.File);
            });

            Console.WriteLine($"   ✓ KV store created: {userProfiles.Bucket}");
            Console.WriteLine($"   ✓ Storage: File");
            Console.WriteLine($"   ✓ History per key: 1\n");

            // Example 2: Create a KV store with TTL for session management
            Console.WriteLine("3. Creating a USER_SESSIONS KV store with TTL...");
            var userSessions = await controller.CreateKeyValueAsync("USER_SESSIONS", builder =>
            {
                builder
                    .WithDescription("User session storage with auto-expiration")
                    .WithMaxHistoryPerKey(1)
                    .WithTTL(TimeSpan.FromHours(2)) // Sessions expire after 2 hours
                    .WithStorage(NatsKVStorageType.Memory); // Use memory for faster access
            });

            Console.WriteLine($"   ✓ KV store created: {userSessions.Bucket}");
            Console.WriteLine($"   ✓ Storage: Memory");
            Console.WriteLine($"   ✓ TTL: 2 hours\n");

            // Example 3: Create a KV store with versioning (history)
            Console.WriteLine("4. Creating a CONFIG KV store with versioning...");
            var configStore = await controller.CreateKeyValueAsync("CONFIG", builder =>
            {
                builder
                    .WithDescription("Application configuration with version history")
                    .WithMaxHistoryPerKey(10) // Keep last 10 versions of each config key
                    .WithMaxBucketSize(10 * 1024 * 1024) // 10MB max
                    .WithStorage(NatsKVStorageType.File);
            });

            Console.WriteLine($"   ✓ KV store created: {configStore.Bucket}");
            Console.WriteLine($"   ✓ Max history per key: 10");
            Console.WriteLine($"   ✓ Max bucket size: 10MB\n");

            // Example 4: List all KV stores
            Console.WriteLine("5. Listing all KV stores...");
            var buckets = await controller.ListKeyValuesAsync();
            Console.WriteLine($"   ✓ Total KV stores: {buckets.Count}");
            foreach (var bucketName in buckets)
            {
                Console.WriteLine($"   - {bucketName}");
            }
            Console.WriteLine();

            // Example 5: Get KV store status
            Console.WriteLine("6. Getting USER_PROFILES status...");
            var status = await controller.GetKeyValueStatusAsync("USER_PROFILES");
            Console.WriteLine($"   ✓ Bucket: {status.Bucket}");
            Console.WriteLine($"   ✓ Values: {status.Values}");
            Console.WriteLine($"   ✓ History: {status.History}");
            Console.WriteLine($"   ✓ Bytes: {status.Bytes}\n");

            // Example 6: Put and get values
            Console.WriteLine("7. Storing and retrieving values...");
            await userProfiles.PutAsync("user:123", "John Doe");
            await userProfiles.PutAsync("user:456", "Jane Smith");

            var user123 = await userProfiles.GetEntryAsync("user:123");
            Console.WriteLine($"   ✓ Retrieved: {user123.Key} = {user123.Value}");

            var user456 = await userProfiles.GetEntryAsync("user:456");
            Console.WriteLine($"   ✓ Retrieved: {user456.Key} = {user456.Value}\n");

            // Example 7: Update KV store configuration
            Console.WriteLine("8. Updating USER_PROFILES max history...");
            var updatedStore = await controller.UpdateKeyValueAsync("USER_PROFILES", builder =>
            {
                builder
                    .WithDescription("User profile storage (updated)")
                    .WithMaxHistoryPerKey(3) // Updated from 1 to 3
                    .WithStorage(NatsKVStorageType.File);
            });

            Console.WriteLine($"   ✓ KV store updated");
            Console.WriteLine($"   ✓ New max history per key: 3\n");

            // Example 8: Watch for changes
            Console.WriteLine("9. Demonstrating watch functionality...");
            await userProfiles.PutAsync("user:789", "Bob Wilson");
            await userProfiles.PutAsync("user:789", "Bob Wilson Jr."); // Update

            var watchedEntry = await userProfiles.GetEntryAsync("user:789");
            Console.WriteLine($"   ✓ Latest value: {watchedEntry.Key} = {watchedEntry.Value}");
            Console.WriteLine($"   ✓ Revision: {watchedEntry.Revision}\n");

            // Example 9: Purge KV store (clear all values but keep bucket)
            Console.WriteLine("10. Purging USER_SESSIONS KV store...");
            var purged = await controller.PurgeKeyValueAsync("USER_SESSIONS");
            Console.WriteLine($"   ✓ KV store purged: {purged}\n");

            // Example 10: Delete KV store
            Console.WriteLine("11. Deleting CONFIG KV store...");
            var deleted = await controller.DeleteKeyValueAsync("CONFIG");
            Console.WriteLine($"   ✓ KV store deleted: {deleted}\n");

            // Example 11: List remaining KV stores
            Console.WriteLine("12. Listing remaining KV stores...");
            var remainingBuckets = await controller.ListKeyValuesAsync();
            Console.WriteLine($"   ✓ Remaining KV stores: {remainingBuckets.Count}");
            foreach (var bucketName in remainingBuckets)
            {
                Console.WriteLine($"   - {bucketName}");
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
            Console.WriteLine("\n13. Shutting down server...");
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
