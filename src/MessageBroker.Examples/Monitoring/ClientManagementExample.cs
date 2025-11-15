using System.Text.Json;
using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Implementation;

namespace MessageBroker.Examples.Monitoring;

/// <summary>
/// Demonstrates client connection management capabilities.
/// Shows how to monitor and disconnect client connections.
/// </summary>
public static class ClientManagementExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== NATS Client Management Example ===\n");
        Console.WriteLine("This example demonstrates monitoring and managing client connections.");
        Console.WriteLine("In a production scenario, you would have actual NATS clients connecting.");
        Console.WriteLine();

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4222,
            MaxPayload = 1048576,
            Description = "Client management example"
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

        await Task.Delay(1000);

        try
        {
            // 1. List all connections
            await ListConnectionsAsync(controller);

            // 2. Demonstrate client info retrieval (simulated)
            await DemonstrateClientInfoAsync(controller);

            // 3. Demonstrate connection filtering
            await DemonstrateConnectionFilteringAsync(controller);

            // 4. Show use cases for client management
            ShowUseCases();

            Console.WriteLine("\n=== Client Management Example Complete ===");
            Console.WriteLine("\nTo see this in action with real clients:");
            Console.WriteLine("  1. Run this server");
            Console.WriteLine("  2. Connect NATS clients from another terminal:");
            Console.WriteLine("     nats sub 'test.>' --server=nats://localhost:4222");
            Console.WriteLine("  3. Run the monitoring again to see active connections");
            Console.WriteLine("\nPress any key to shutdown...");
            Console.ReadKey();
        }
        finally
        {
            Console.WriteLine("\nShutting down server...");
            await controller.ShutdownAsync();
        }
    }

    private static async Task ListConnectionsAsync(NatsController controller)
    {
        Console.WriteLine("--- 1. Listing All Connections ---");

        try
        {
            // Get connections with subscription details
            var connz = await controller.GetConnzAsync("include-subscriptions");
            using var doc = JsonDocument.Parse(connz);
            var root = doc.RootElement;

            var total = GetJsonInt(root, "total", 0);
            var numConns = GetJsonInt(root, "num_connections", 0);

            Console.WriteLine($"Total connections ever: {total}");
            Console.WriteLine($"Current active connections: {numConns}");

            if (root.TryGetProperty("connections", out var connections) && connections.GetArrayLength() > 0)
            {
                Console.WriteLine($"\nActive Connections:");
                foreach (var conn in connections.EnumerateArray())
                {
                    DisplayConnectionInfo(conn);
                }
            }
            else
            {
                Console.WriteLine("\nNo active client connections.");
                Console.WriteLine("(This is normal when only the server is running)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static async Task DemonstrateClientInfoAsync(NatsController controller)
    {
        Console.WriteLine("--- 2. Getting Specific Client Information ---");

        try
        {
            // First, get the list of connections to find a client ID
            var connz = await controller.GetConnzAsync();
            using var doc = JsonDocument.Parse(connz);
            var root = doc.RootElement;

            if (root.TryGetProperty("connections", out var connections) && connections.GetArrayLength() > 0)
            {
                // Get the first connection's CID
                var firstConn = connections[0];
                var cid = (ulong)GetJsonLong(firstConn, "cid", 0);

                Console.WriteLine($"Retrieving details for client {cid}...");

                // Get detailed information about this specific client
                var clientInfo = await controller.GetClientInfoAsync(cid);
                using var clientDoc = JsonDocument.Parse(clientInfo);
                var client = clientDoc.RootElement;

                Console.WriteLine("\nDetailed Client Information:");
                DisplayConnectionInfo(client, detailed: true);
            }
            else
            {
                Console.WriteLine("No clients connected to demonstrate client info retrieval.");
                Console.WriteLine("\nThe GetClientInfoAsync() method would return detailed information including:");
                Console.WriteLine("  - Connection ID (CID)");
                Console.WriteLine("  - Client IP address and port");
                Console.WriteLine("  - Number of subscriptions");
                Console.WriteLine("  - Bytes in/out");
                Console.WriteLine("  - Messages in/out");
                Console.WriteLine("  - Connection uptime");
                Console.WriteLine("  - Client library name and version");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static async Task DemonstrateConnectionFilteringAsync(NatsController controller)
    {
        Console.WriteLine("--- 3. Connection Filtering and Analysis ---");

        try
        {
            var connz = await controller.GetConnzAsync("include-subscriptions");
            using var doc = JsonDocument.Parse(connz);
            var root = doc.RootElement;

            if (root.TryGetProperty("connections", out var connections) && connections.GetArrayLength() > 0)
            {
                Console.WriteLine("Analyzing connections...\n");

                // Example: Find connections with high message counts
                var highTrafficConns = new List<(ulong cid, long inMsgs, long outMsgs)>();

                foreach (var conn in connections.EnumerateArray())
                {
                    var cid = (ulong)GetJsonLong(conn, "cid", 0);
                    var inMsgs = GetJsonLong(conn, "in_msgs", 0);
                    var outMsgs = GetJsonLong(conn, "out_msgs", 0);

                    if (inMsgs > 1000 || outMsgs > 1000)
                    {
                        highTrafficConns.Add((cid, inMsgs, outMsgs));
                    }
                }

                if (highTrafficConns.Count > 0)
                {
                    Console.WriteLine("High-traffic connections (>1000 messages):");
                    foreach (var (cid, inMsgs, outMsgs) in highTrafficConns)
                    {
                        Console.WriteLine($"  CID {cid}: {inMsgs:N0} in, {outMsgs:N0} out");
                    }
                }
                else
                {
                    Console.WriteLine("No high-traffic connections detected.");
                }

                // Example: Find idle connections
                Console.WriteLine("\nYou could also filter for:");
                Console.WriteLine("  - Idle connections (no messages in X minutes)");
                Console.WriteLine("  - Connections with many subscriptions");
                Console.WriteLine("  - Connections from specific IP addresses");
                Console.WriteLine("  - Connections with high pending bytes (slow consumers)");
            }
            else
            {
                Console.WriteLine("No connections to analyze.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static void ShowUseCases()
    {
        Console.WriteLine("--- 4. Client Management Use Cases ---\n");

        Console.WriteLine("Common use cases for client management:\n");

        Console.WriteLine("1. MONITORING & ALERTING");
        Console.WriteLine("   - Track connection counts over time");
        Console.WriteLine("   - Alert on connection spikes or drops");
        Console.WriteLine("   - Monitor message throughput per client");
        Console.WriteLine();

        Console.WriteLine("2. SECURITY & COMPLIANCE");
        Console.WriteLine("   - Identify unauthorized connection attempts");
        Console.WriteLine("   - Force disconnect compromised clients");
        Console.WriteLine("   - Audit client activity");
        Console.WriteLine();

        Console.WriteLine("3. PERFORMANCE OPTIMIZATION");
        Console.WriteLine("   - Identify slow consumers (high pending bytes)");
        Console.WriteLine("   - Disconnect idle connections");
        Console.WriteLine("   - Balance load across clients");
        Console.WriteLine();

        Console.WriteLine("4. CAPACITY PLANNING");
        Console.WriteLine("   - Track connection patterns");
        Console.WriteLine("   - Identify peak usage times");
        Console.WriteLine("   - Plan for scaling needs");
        Console.WriteLine();

        Console.WriteLine("Example: Disconnecting a misbehaving client");
        Console.WriteLine("```csharp");
        Console.WriteLine("// Get connections");
        Console.WriteLine("var connz = await controller.GetConnzAsync();");
        Console.WriteLine("");
        Console.WriteLine("// Find problematic client (e.g., high pending bytes)");
        Console.WriteLine("// ... parse JSON to find CID ...");
        Console.WriteLine("");
        Console.WriteLine("// Disconnect the client");
        Console.WriteLine("await controller.DisconnectClientAsync(clientId);");
        Console.WriteLine("Console.WriteLine($\"Disconnected client {clientId}\");");
        Console.WriteLine("```");
        Console.WriteLine();
    }

    private static void DisplayConnectionInfo(JsonElement conn, bool detailed = false)
    {
        var cid = GetJsonLong(conn, "cid", 0);
        var name = GetJsonProperty(conn, "name", "unnamed");
        var ip = GetJsonProperty(conn, "ip", "?");
        var port = GetJsonInt(conn, "port", 0);
        var subscriptions = GetJsonInt(conn, "subscriptions", 0);

        Console.WriteLine($"\n  Connection {cid}:");
        Console.WriteLine($"    Name: {name}");
        Console.WriteLine($"    Address: {ip}:{port}");
        Console.WriteLine($"    Subscriptions: {subscriptions}");

        if (detailed)
        {
            var inMsgs = GetJsonLong(conn, "in_msgs", 0);
            var outMsgs = GetJsonLong(conn, "out_msgs", 0);
            var inBytes = GetJsonLong(conn, "in_bytes", 0);
            var outBytes = GetJsonLong(conn, "out_bytes", 0);
            var pending = GetJsonLong(conn, "pending_bytes", 0);
            var idle = GetJsonProperty(conn, "idle", "unknown");
            var uptime = GetJsonProperty(conn, "uptime", "unknown");
            var lang = GetJsonProperty(conn, "lang", "unknown");
            var version = GetJsonProperty(conn, "version", "unknown");

            Console.WriteLine($"    Messages In: {inMsgs:N0}");
            Console.WriteLine($"    Messages Out: {outMsgs:N0}");
            Console.WriteLine($"    Bytes In: {inBytes:N0}");
            Console.WriteLine($"    Bytes Out: {outBytes:N0}");
            Console.WriteLine($"    Pending: {pending:N0} bytes");
            Console.WriteLine($"    Idle: {idle}");
            Console.WriteLine($"    Uptime: {uptime}");
            Console.WriteLine($"    Client: {lang} {version}");

            if (conn.TryGetProperty("subscriptions_list", out var subsList))
            {
                Console.WriteLine($"    Subscription Details:");
                foreach (var sub in subsList.EnumerateArray())
                {
                    var subject = GetJsonProperty(sub, "subject", "?");
                    Console.WriteLine($"      - {subject}");
                }
            }
        }
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
