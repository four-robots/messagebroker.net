using System.Text.Json;
using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Implementation;

namespace MessageBroker.Examples.Monitoring;

/// <summary>
/// Demonstrates the NATS server monitoring capabilities.
/// Shows how to use Connz, Subsz, Jsz, Routez, and Leafz endpoints.
/// </summary>
public static class MonitoringExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== NATS Server Monitoring Example ===\n");

        // Create and start a NATS server with JetStream enabled
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4222,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-monitoring-example"),
            Debug = true,
            Description = "Monitoring example server"
        };

        Console.WriteLine("Starting NATS server...");
        var result = await controller.ConfigureAsync(config);

        if (!result.Success)
        {
            Console.WriteLine($"Failed to start server: {result.ErrorMessage}");
            return;
        }

        Console.WriteLine($"✓ Server started on port {config.Port}");
        Console.WriteLine();

        // Wait a moment for server to fully initialize
        await Task.Delay(1000);

        try
        {
            // 1. Connection Monitoring (Connz)
            await DemonstrateConnzAsync(controller);

            // 2. Subscription Monitoring (Subsz)
            await DemonstrateSubszAsync(controller);

            // 3. JetStream Monitoring (Jsz)
            await DemonstrateJszAsync(controller);

            // 4. Server Info
            await DemonstrateServerInfoAsync(controller);

            Console.WriteLine("\n=== Monitoring Example Complete ===");
            Console.WriteLine("Press any key to shutdown...");
            Console.ReadKey();
        }
        finally
        {
            Console.WriteLine("\nShutting down server...");
            await controller.ShutdownAsync();

            // Cleanup
            if (Directory.Exists(config.JetstreamStoreDir))
            {
                try
                {
                    Directory.Delete(config.JetstreamStoreDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    private static async Task DemonstrateConnzAsync(NatsController controller)
    {
        Console.WriteLine("--- 1. Connection Monitoring (Connz) ---");

        try
        {
            var connz = await controller.GetConnzAsync();
            using var doc = JsonDocument.Parse(connz);
            var root = doc.RootElement;

            Console.WriteLine($"Server ID: {GetJsonProperty(root, "server_id", "unknown")}");
            Console.WriteLine($"Now: {GetJsonProperty(root, "now", "unknown")}");
            Console.WriteLine($"Total Connections: {GetJsonInt(root, "total", 0)}");
            Console.WriteLine($"Active Connections: {GetJsonInt(root, "num_connections", 0)}");
            Console.WriteLine($"In Bytes: {GetJsonLong(root, "in_bytes", 0):N0}");
            Console.WriteLine($"Out Bytes: {GetJsonLong(root, "out_bytes", 0):N0}");
            Console.WriteLine($"In Messages: {GetJsonLong(root, "in_msgs", 0):N0}");
            Console.WriteLine($"Out Messages: {GetJsonLong(root, "out_msgs", 0):N0}");

            if (root.TryGetProperty("connections", out var conns) && conns.GetArrayLength() > 0)
            {
                Console.WriteLine($"\nActive Connections ({conns.GetArrayLength()}):");
                foreach (var conn in conns.EnumerateArray())
                {
                    var cid = GetJsonProperty(conn, "cid", "?");
                    var ip = GetJsonProperty(conn, "ip", "?");
                    var port = GetJsonInt(conn, "port", 0);
                    var subs = GetJsonInt(conn, "subscriptions", 0);
                    Console.WriteLine($"  - CID: {cid}, IP: {ip}:{port}, Subscriptions: {subs}");
                }
            }
            else
            {
                Console.WriteLine("No active client connections (server is standalone)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static async Task DemonstrateSubszAsync(NatsController controller)
    {
        Console.WriteLine("--- 2. Subscription Monitoring (Subsz) ---");

        try
        {
            var subsz = await controller.GetSubszAsync();
            using var doc = JsonDocument.Parse(subsz);
            var root = doc.RootElement;

            Console.WriteLine($"Total Subscriptions: {GetJsonInt(root, "total", 0)}");
            Console.WriteLine($"Active Subscriptions: {GetJsonInt(root, "num_subscriptions", 0)}");
            Console.WriteLine($"Number of Inserts: {GetJsonLong(root, "num_inserts", 0):N0}");
            Console.WriteLine($"Number of Removes: {GetJsonLong(root, "num_removes", 0):N0}");
            Console.WriteLine($"Number of Matches: {GetJsonLong(root, "num_matches", 0):N0}");

            if (root.TryGetProperty("subscriptions_list", out var subsList) && subsList.GetArrayLength() > 0)
            {
                Console.WriteLine($"\nSubscriptions ({Math.Min(5, subsList.GetArrayLength())} of {subsList.GetArrayLength()}):");
                int count = 0;
                foreach (var sub in subsList.EnumerateArray())
                {
                    if (count++ >= 5) break;
                    var subject = GetJsonProperty(sub, "subject", "?");
                    var queue = GetJsonProperty(sub, "queue", "");
                    Console.WriteLine($"  - Subject: {subject}" + (string.IsNullOrEmpty(queue) ? "" : $", Queue: {queue}"));
                }
            }
            else
            {
                Console.WriteLine("No active subscriptions");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static async Task DemonstrateJszAsync(NatsController controller)
    {
        Console.WriteLine("--- 3. JetStream Monitoring (Jsz) ---");

        try
        {
            var jsz = await controller.GetJszAsync();
            using var doc = JsonDocument.Parse(jsz);
            var root = doc.RootElement;

            if (root.TryGetProperty("config", out var config))
            {
                Console.WriteLine("JetStream Configuration:");
                Console.WriteLine($"  Max Memory: {GetJsonLong(config, "max_memory", 0):N0} bytes");
                Console.WriteLine($"  Max Storage: {GetJsonLong(config, "max_storage", 0):N0} bytes");
                Console.WriteLine($"  Store Dir: {GetJsonProperty(config, "store_dir", "unknown")}");
            }

            if (root.TryGetProperty("memory", out var memory))
            {
                Console.WriteLine("\nMemory Usage:");
                Console.WriteLine($"  Used: {GetJsonLong(root, "memory", 0):N0} bytes");
            }

            if (root.TryGetProperty("storage", out var storage))
            {
                Console.WriteLine($"  Storage: {GetJsonLong(root, "storage", 0):N0} bytes");
            }

            Console.WriteLine($"\nStreams: {GetJsonInt(root, "streams", 0)}");
            Console.WriteLine($"Consumers: {GetJsonInt(root, "consumers", 0)}");
            Console.WriteLine($"Messages: {GetJsonLong(root, "messages", 0):N0}");
            Console.WriteLine($"Bytes: {GetJsonLong(root, "bytes", 0):N0}");

            if (root.TryGetProperty("meta", out var meta))
            {
                if (meta.TryGetProperty("leader", out var leader))
                {
                    Console.WriteLine($"\nCluster Leader: {leader.GetString()}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static async Task DemonstrateServerInfoAsync(NatsController controller)
    {
        Console.WriteLine("--- 4. Server Information ---");

        try
        {
            var info = await controller.GetInfoAsync();
            Console.WriteLine($"Server ID: {info.ServerId}");
            Console.WriteLine($"Version: {info.Version ?? "unknown"}");
            Console.WriteLine($"Client URL: {info.ClientUrl}");
            Console.WriteLine($"JetStream Enabled: {info.JetstreamEnabled}");
            Console.WriteLine($"Started At: {info.StartedAt}");
            Console.WriteLine($"Connections: {info.Connections}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    // Helper methods for JSON parsing
    private static string GetJsonProperty(JsonElement element, string propertyName, string defaultValue)
    {
        return element.TryGetProperty(propertyName, out var prop) ? (prop.GetString() ?? defaultValue) : defaultValue;
    }

    private static int GetJsonInt(JsonElement element, string propertyName, int defaultValue)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetInt32() : defaultValue;
    }

    private static long GetJsonLong(JsonElement element, string propertyName, long defaultValue)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetInt64() : defaultValue;
    }
}
