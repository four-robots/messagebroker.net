using DotGnatly.Core.Configuration;
using DotGnatly.Extensions.JetStream.Extensions;
using DotGnatly.Extensions.JetStream.Models;
using DotGnatly.Nats.Implementation;

namespace DotGnatly.Examples.KeyValue;

/// <summary>
/// Demonstrates how to load and create Key-Value stores from JSON configuration files.
/// </summary>
public class KVJsonConfigExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Key-Value JSON Configuration Example ===\n");

        // Create and configure a NATS server with JetStream enabled
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Port = 4222,
            Host = "localhost",
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "dotgnatly-kv-json-example"),
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
            // Example 1: Load KV store from JSON string
            Console.WriteLine("2. Creating KV store from JSON string...");
            var sessionsJson = @"{
                ""bucket"": ""USER_SESSIONS"",
                ""description"": ""User session storage with TTL"",
                ""max_history_per_key"": 1,
                ""ttl"": 7200000000000,
                ""storage"": ""memory""
            }";

            var sessionsStore = await controller.CreateKeyValueFromJsonAsync(sessionsJson);
            Console.WriteLine($"   ✓ KV store created: {sessionsStore.Bucket}");
            Console.WriteLine($"   ✓ Storage: Memory");
            Console.WriteLine($"   ✓ TTL: 2 hours\n");

            // Example 2: Load KV store from a single JSON file
            Console.WriteLine("3. Creating KV store from JSON file (user-profiles.json)...");
            var configsPath = Path.Combine("KeyValue", "configs");
            var profilesJsonPath = Path.Combine(configsPath, "user-profiles.json");

            if (File.Exists(profilesJsonPath))
            {
                var profilesStore = await controller.CreateKeyValueFromFileAsync(profilesJsonPath);
                Console.WriteLine($"   ✓ KV store created: {profilesStore.Bucket}");

                // Get status to show details
                var status = await controller.GetKeyValueStatusAsync(profilesStore.Bucket);
                Console.WriteLine($"   ✓ History per key: {status.History}");
                Console.WriteLine($"   ✓ Storage: File\n");
            }
            else
            {
                Console.WriteLine($"   ⚠ File not found: {profilesJsonPath}\n");
            }

            // Example 3: Load multiple KV stores from directory
            Console.WriteLine("4. Loading all KV configs from directory...");
            if (Directory.Exists(configsPath))
            {
                var stores = await controller.CreateKeyValuesFromDirectoryAsync(configsPath);
                Console.WriteLine($"   ✓ Created {stores.Count} KV stores from directory:");
                foreach (var store in stores)
                {
                    // Skip ones we already created
                    if (store.Bucket == "USER_SESSIONS" || store.Bucket == "USER_PROFILES")
                        continue;

                    Console.WriteLine($"   - {store.Bucket}");
                    var status = await controller.GetKeyValueStatusAsync(store.Bucket);
                    Console.WriteLine($"     History: {status.History}, Values: {status.Values}");
                }
                Console.WriteLine();
            }

            // Example 4: Parse JSON and modify configuration before creating
            Console.WriteLine("5. Parsing JSON, modifying config, and creating KV store...");
            var cacheJson = @"{
                ""bucket"": ""APP_CACHE"",
                ""description"": ""Application cache"",
                ""max_history_per_key"": 1,
                ""ttl"": 3600000000000,
                ""max_bucket_size"": 10485760,
                ""storage"": ""memory""
            }";

            var configJson = KVConfigJson.FromJson(cacheJson);
            var builder = configJson.ToBuilder();

            // Modify the configuration using fluent API
            builder
                .WithTTL(TimeSpan.FromMinutes(30)) // Changed from 1 hour to 30 minutes
                .WithDescription("Modified application cache with shorter TTL");

            await using var context = await controller.GetJetStreamContextAsync();
            var kvConfig = builder.Build();

            // Create KV store
            var kvContext = new NATS.Client.KeyValueStore.NatsKVContext(context.Connection);
            var cacheStore = await kvContext.CreateStoreAsync(kvConfig);

            Console.WriteLine($"   ✓ KV store created: {cacheStore.Bucket}");
            Console.WriteLine($"   ✓ Description: Modified cache");
            Console.WriteLine($"   ✓ TTL: 30 minutes (modified from 1 hour)\n");

            // Example 5: Demonstrate JSON with all properties
            Console.WriteLine("6. Creating KV store with comprehensive configuration...");
            var fullConfigJson = @"{
                ""bucket"": ""CONFIG_STORE"",
                ""description"": ""Application configuration with full options"",
                ""max_history_per_key"": 10,
                ""ttl"": 0,
                ""max_bucket_size"": 104857600,
                ""replicas"": 1,
                ""storage"": ""file""
            }";

            var fullStore = await controller.CreateKeyValueFromJsonAsync(fullConfigJson);
            Console.WriteLine($"   ✓ KV store created: {fullStore.Bucket}");

            var fullStatus = await controller.GetKeyValueStatusAsync(fullStore.Bucket);
            Console.WriteLine($"   ✓ History per key: {fullStatus.History}");
            Console.WriteLine($"   ✓ Max bucket size: 100MB");
            Console.WriteLine($"   ✓ TTL: None (0)\n");

            // Example 6: List all created KV stores
            Console.WriteLine("7. Listing all KV stores...");
            var buckets = await controller.ListKeyValuesAsync();
            Console.WriteLine($"   ✓ Total KV stores: {buckets.Count}");
            foreach (var bucketName in buckets)
            {
                var status = await controller.GetKeyValueStatusAsync(bucketName);
                Console.WriteLine($"   - {bucketName}");
                Console.WriteLine($"     Values: {status.Values}, Bytes: {status.Bytes}");
            }
            Console.WriteLine();

            // Example 7: Put some test data
            Console.WriteLine("8. Storing test data in USER_SESSIONS...");
            await sessionsStore.PutAsync("session:abc123", "user:john");
            await sessionsStore.PutAsync("session:def456", "user:jane");

            var sessionStatus = await controller.GetKeyValueStatusAsync("USER_SESSIONS");
            Console.WriteLine($"   ✓ Stored 2 sessions");
            Console.WriteLine($"   ✓ Total values: {sessionStatus.Values}\n");

            // Example 8: Demonstrate versioning with CONFIG_STORE
            Console.WriteLine("9. Demonstrating versioning in CONFIG_STORE...");
            await fullStore.PutAsync("app.timeout", "30");
            await fullStore.PutAsync("app.timeout", "60"); // Update
            await fullStore.PutAsync("app.timeout", "90"); // Update again

            var timeoutEntry = await fullStore.GetEntryAsync("app.timeout");
            Console.WriteLine($"   ✓ Current value: {timeoutEntry.Value}");
            Console.WriteLine($"   ✓ Revision: {timeoutEntry.Revision}");
            Console.WriteLine($"   ✓ History tracking enabled (max 10 versions)\n");
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
