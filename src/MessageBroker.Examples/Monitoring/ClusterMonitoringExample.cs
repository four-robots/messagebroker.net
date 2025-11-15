using System.Text.Json;
using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Implementation;

namespace MessageBroker.Examples.Monitoring;

/// <summary>
/// Demonstrates cluster and leaf node monitoring capabilities.
/// Shows how to monitor NATS clustering and topology.
/// </summary>
public static class ClusterMonitoringExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== NATS Cluster Monitoring Example ===\n");

        // Start a clustered NATS server
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4222,
            Cluster = new ClusterConfiguration
            {
                Name = "demo-cluster",
                Host = "127.0.0.1",
                Port = 6222,
                // In a real cluster, you'd add routes to other nodes:
                // Routes = new[] { "nats://node2:6222", "nats://node3:6222" }
            },
            Description = "Cluster monitoring example"
        };

        Console.WriteLine("Starting NATS server with cluster configuration...");
        var result = await controller.ConfigureAsync(config);

        if (!result.Success)
        {
            Console.WriteLine($"Failed to start server: {result.ErrorMessage}");
            return;
        }

        Console.WriteLine($"✓ Server started on port {config.Port}");
        Console.WriteLine($"✓ Cluster port: {config.Cluster.Port}");
        Console.WriteLine($"✓ Cluster name: {config.Cluster.Name}");
        Console.WriteLine();

        await Task.Delay(1000);

        try
        {
            // Monitor cluster routes
            await DemonstrateRoutezAsync(controller);

            Console.WriteLine("\n=== Cluster Monitoring Example Complete ===");
            Console.WriteLine("Press any key to shutdown...");
            Console.ReadKey();
        }
        finally
        {
            Console.WriteLine("\nShutting down server...");
            await controller.ShutdownAsync();
        }
    }

    private static async Task DemonstrateRoutezAsync(NatsController controller)
    {
        Console.WriteLine("--- Cluster Route Monitoring (Routez) ---");

        try
        {
            var routez = await controller.GetRoutezAsync();
            using var doc = JsonDocument.Parse(routez);
            var root = doc.RootElement;

            Console.WriteLine($"Server ID: {GetJsonProperty(root, "server_id", "unknown")}");
            Console.WriteLine($"Now: {GetJsonProperty(root, "now", "unknown")}");
            Console.WriteLine($"Number of Routes: {GetJsonInt(root, "num_routes", 0)}");

            if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
            {
                Console.WriteLine($"\nCluster Routes ({routes.GetArrayLength()}):");
                foreach (var route in routes.EnumerateArray())
                {
                    var rid = GetJsonProperty(route, "rid", "?");
                    var remoteId = GetJsonProperty(route, "remote_id", "?");
                    var ip = GetJsonProperty(route, "ip", "?");
                    var port = GetJsonInt(route, "port", 0);
                    var pending = GetJsonLong(route, "pending_size", 0);
                    var inMsgs = GetJsonLong(route, "in_msgs", 0);
                    var outMsgs = GetJsonLong(route, "out_msgs", 0);
                    var subs = GetJsonInt(route, "subscriptions", 0);

                    Console.WriteLine($"\n  Route {rid}:");
                    Console.WriteLine($"    Remote ID: {remoteId}");
                    Console.WriteLine($"    Address: {ip}:{port}");
                    Console.WriteLine($"    Subscriptions: {subs}");
                    Console.WriteLine($"    In Messages: {inMsgs:N0}");
                    Console.WriteLine($"    Out Messages: {outMsgs:N0}");
                    Console.WriteLine($"    Pending: {pending:N0} bytes");
                }
            }
            else
            {
                Console.WriteLine("\nNo cluster routes established.");
                Console.WriteLine("This is normal for a standalone server.");
                Console.WriteLine("\nTo see routes, you would need to:");
                Console.WriteLine("  1. Start multiple NATS servers with cluster configuration");
                Console.WriteLine("  2. Configure routes between them");
                Console.WriteLine("  3. Wait for them to connect");
                Console.WriteLine("\nExample cluster configuration:");
                Console.WriteLine("  Server 1: Port 4222, Cluster 6222");
                Console.WriteLine("  Server 2: Port 4223, Cluster 6223, Routes: [nats://127.0.0.1:6222]");
                Console.WriteLine("  Server 3: Port 4224, Cluster 6224, Routes: [nats://127.0.0.1:6222]");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    public static async Task RunLeafNodeExample()
    {
        Console.WriteLine("=== NATS Leaf Node Monitoring Example ===\n");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Host = "127.0.0.1",
                Port = 7422,
                // In production, you'd connect to a remote hub:
                // RemoteURLs = new[] { "nats://hub.example.com:7422" }
            },
            Description = "Leaf node monitoring example"
        };

        Console.WriteLine("Starting NATS server with leaf node configuration...");
        var result = await controller.ConfigureAsync(config);

        if (!result.Success)
        {
            Console.WriteLine($"Failed to start server: {result.ErrorMessage}");
            return;
        }

        Console.WriteLine($"✓ Server started on port {config.Port}");
        Console.WriteLine($"✓ Leaf node port: {config.LeafNode.Port}");
        Console.WriteLine();

        await Task.Delay(1000);

        try
        {
            await DemonstrateLeafzAsync(controller);

            Console.WriteLine("\n=== Leaf Node Monitoring Example Complete ===");
            Console.WriteLine("Press any key to shutdown...");
            Console.ReadKey();
        }
        finally
        {
            Console.WriteLine("\nShutting down server...");
            await controller.ShutdownAsync();
        }
    }

    private static async Task DemonstrateLeafzAsync(NatsController controller)
    {
        Console.WriteLine("--- Leaf Node Monitoring (Leafz) ---");

        try
        {
            var leafz = await controller.GetLeafzAsync();
            using var doc = JsonDocument.Parse(leafz);
            var root = doc.RootElement;

            Console.WriteLine($"Server ID: {GetJsonProperty(root, "server_id", "unknown")}");
            Console.WriteLine($"Now: {GetJsonProperty(root, "now", "unknown")}");
            Console.WriteLine($"Number of Leaf Nodes: {GetJsonInt(root, "num_leafs", 0)}");

            if (root.TryGetProperty("leafs", out var leafs) && leafs.GetArrayLength() > 0)
            {
                Console.WriteLine($"\nLeaf Nodes ({leafs.GetArrayLength()}):");
                foreach (var leaf in leafs.EnumerateArray())
                {
                    var account = GetJsonProperty(leaf, "account", "?");
                    var ip = GetJsonProperty(leaf, "ip", "?");
                    var port = GetJsonInt(leaf, "port", 0);
                    var subs = GetJsonInt(leaf, "subscriptions", 0);
                    var inMsgs = GetJsonLong(leaf, "in_msgs", 0);
                    var outMsgs = GetJsonLong(leaf, "out_msgs", 0);

                    Console.WriteLine($"\n  Leaf Node:");
                    Console.WriteLine($"    Account: {account}");
                    Console.WriteLine($"    Address: {ip}:{port}");
                    Console.WriteLine($"    Subscriptions: {subs}");
                    Console.WriteLine($"    In Messages: {inMsgs:N0}");
                    Console.WriteLine($"    Out Messages: {outMsgs:N0}");
                }
            }
            else
            {
                Console.WriteLine("\nNo leaf node connections established.");
                Console.WriteLine("This is normal for a server without leaf node remotes configured.");
                Console.WriteLine("\nLeaf nodes are used for:");
                Console.WriteLine("  - Connecting edge servers to a central hub");
                Console.WriteLine("  - Creating multi-tenant isolation");
                Console.WriteLine("  - Geographic distribution");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    // Helper methods
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
