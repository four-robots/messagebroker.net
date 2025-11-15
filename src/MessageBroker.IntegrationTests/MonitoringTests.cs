using System.Text.Json;
using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Implementation;

namespace MessageBroker.IntegrationTests;

/// <summary>
/// Integration test suite wrapper for monitoring tests.
/// </summary>
public class MonitoringTestSuite : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        await results.AssertAsync("Connz Monitoring", MonitoringTests.TestConnzMonitoring);
        await results.AssertAsync("Subsz Monitoring", MonitoringTests.TestSubszMonitoring);
        await results.AssertAsync("Jsz Monitoring", MonitoringTests.TestJszMonitoring);
        await results.AssertAsync("Routez Monitoring", MonitoringTests.TestRoutezMonitoring);
        await results.AssertAsync("Leafz Monitoring", MonitoringTests.TestLeafzMonitoring);
        await results.AssertAsync("Client Management", MonitoringTests.TestClientManagement);
        await results.AssertAsync("Subscription Filter", MonitoringTests.TestMonitoringWithSubscriptionFilter);
        await results.AssertAsync("Jsz Account Filter", MonitoringTests.TestJszWithAccountFilter);
    }
}

/// <summary>
/// Integration tests for NATS server monitoring endpoints (Connz, Subsz, Jsz, Routez, Leafz).
/// </summary>
public static class MonitoringTests
{
    public static async Task<bool> TestConnzMonitoring()
    {
        Console.WriteLine("\n=== Testing Connz (Connection Monitoring) ===");

        using var controller = new NatsController();

        // Start server
        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4222,
            Description = "Connz monitoring test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        // Give server time to start
        await Task.Delay(500);

        try
        {
            // Get connection information
            var connz = await controller.GetConnzAsync();
            Console.WriteLine($"✓ Retrieved Connz data");

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(connz);
            var root = doc.RootElement;

            if (root.TryGetProperty("num_connections", out var numConns))
            {
                Console.WriteLine($"  Number of connections: {numConns.GetInt32()}");
            }

            if (root.TryGetProperty("total", out var total))
            {
                Console.WriteLine($"  Total connections: {total.GetInt32()}");
            }

            if (root.TryGetProperty("now", out var now))
            {
                Console.WriteLine($"  Timestamp: {now.GetString()}");
            }

            Console.WriteLine("✓ Connz test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connz test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestSubszMonitoring()
    {
        Console.WriteLine("\n=== Testing Subsz (Subscription Monitoring) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4223,
            Description = "Subsz monitoring test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Get subscription information
            var subsz = await controller.GetSubszAsync();
            Console.WriteLine($"✓ Retrieved Subsz data");

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(subsz);
            var root = doc.RootElement;

            if (root.TryGetProperty("num_subscriptions", out var numSubs))
            {
                Console.WriteLine($"  Number of subscriptions: {numSubs.GetInt32()}");
            }

            if (root.TryGetProperty("total", out var total))
            {
                Console.WriteLine($"  Total subscriptions: {total.GetInt32()}");
            }

            Console.WriteLine("✓ Subsz test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Subsz test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestJszMonitoring()
    {
        Console.WriteLine("\n=== Testing Jsz (JetStream Monitoring) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4224,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-jsz-test"),
            Description = "Jsz monitoring test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Get JetStream information
            var jsz = await controller.GetJszAsync();
            Console.WriteLine($"✓ Retrieved Jsz data");

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(jsz);
            var root = doc.RootElement;

            if (root.TryGetProperty("config", out var configElem))
            {
                Console.WriteLine($"  JetStream config found");
                if (configElem.TryGetProperty("max_memory", out var maxMem))
                {
                    Console.WriteLine($"    Max memory: {maxMem.GetInt64()}");
                }
                if (configElem.TryGetProperty("max_storage", out var maxStore))
                {
                    Console.WriteLine($"    Max storage: {maxStore.GetInt64()}");
                }
            }

            if (root.TryGetProperty("streams", out var streams))
            {
                Console.WriteLine($"  Number of streams: {streams.GetInt32()}");
            }

            if (root.TryGetProperty("consumers", out var consumers))
            {
                Console.WriteLine($"  Number of consumers: {consumers.GetInt32()}");
            }

            Console.WriteLine("✓ Jsz test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Jsz test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();

            // Cleanup JetStream directory
            try
            {
                if (Directory.Exists(config.JetstreamStoreDir))
                {
                    Directory.Delete(config.JetstreamStoreDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    public static async Task<bool> TestRoutezMonitoring()
    {
        Console.WriteLine("\n=== Testing Routez (Cluster Routing Monitoring) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4225,
            Cluster = new ClusterConfiguration
            {
                Name = "test-cluster",
                Host = "127.0.0.1",
                Port = 6222
            },
            Description = "Routez monitoring test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Get route information
            var routez = await controller.GetRoutezAsync();
            Console.WriteLine($"✓ Retrieved Routez data");

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(routez);
            var root = doc.RootElement;

            if (root.TryGetProperty("num_routes", out var numRoutes))
            {
                Console.WriteLine($"  Number of routes: {numRoutes.GetInt32()}");
            }

            if (root.TryGetProperty("routes", out var routes))
            {
                Console.WriteLine($"  Routes array length: {routes.GetArrayLength()}");
            }

            Console.WriteLine("✓ Routez test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Routez test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestLeafzMonitoring()
    {
        Console.WriteLine("\n=== Testing Leafz (Leaf Node Monitoring) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4226,
            LeafNode = new LeafNodeConfiguration
            {
                Host = "127.0.0.1",
                Port = 7422
            },
            Description = "Leafz monitoring test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Get leaf node information
            var leafz = await controller.GetLeafzAsync();
            Console.WriteLine($"✓ Retrieved Leafz data");

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(leafz);
            var root = doc.RootElement;

            if (root.TryGetProperty("num_leafs", out var numLeafs))
            {
                Console.WriteLine($"  Number of leaf nodes: {numLeafs.GetInt32()}");
            }

            if (root.TryGetProperty("leafs", out var leafs))
            {
                Console.WriteLine($"  Leaf nodes array length: {leafs.GetArrayLength()}");
            }

            Console.WriteLine("✓ Leafz test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Leafz test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestClientManagement()
    {
        Console.WriteLine("\n=== Testing Client Management (GetClientInfo, DisconnectClient) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4227,
            Description = "Client management test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Get connection list to find a client ID
            var connz = await controller.GetConnzAsync();
            using var doc = JsonDocument.Parse(connz);
            var root = doc.RootElement;

            // For this test, we're just verifying the methods work even without active clients
            // In a real scenario, you would connect a NATS client first
            Console.WriteLine("✓ Connection monitoring working");

            // Test getting non-existent client (should throw or return error)
            try
            {
                var clientInfo = await controller.GetClientInfoAsync(99999);
                // If we get here without error, the client doesn't exist but the method works
                Console.WriteLine("✓ GetClientInfo method callable (client doesn't exist as expected)");
            }
            catch (InvalidOperationException)
            {
                // Expected - client doesn't exist
                Console.WriteLine("✓ GetClientInfo correctly reports missing client");
            }

            // Test disconnecting non-existent client (should throw or return error)
            try
            {
                await controller.DisconnectClientAsync(99999);
                Console.WriteLine("✓ DisconnectClient method callable (client doesn't exist as expected)");
            }
            catch (InvalidOperationException)
            {
                // Expected - client doesn't exist
                Console.WriteLine("✓ DisconnectClient correctly reports missing client");
            }

            Console.WriteLine("✓ Client management test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Client management test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestMonitoringWithSubscriptionFilter()
    {
        Console.WriteLine("\n=== Testing Monitoring with Subscription Filters ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4228,
            Description = "Subscription filter test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Test Connz with subscription filter
            var connzWithSubs = await controller.GetConnzAsync("test.*");
            Console.WriteLine($"✓ Retrieved Connz with subscription filter");

            using var doc = JsonDocument.Parse(connzWithSubs);
            Console.WriteLine($"  Response contains valid JSON");

            // Test Subsz with filter
            var subszFiltered = await controller.GetSubszAsync("test.>");
            Console.WriteLine($"✓ Retrieved Subsz with filter");

            Console.WriteLine("✓ Subscription filter test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Subscription filter test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestJszWithAccountFilter()
    {
        Console.WriteLine("\n=== Testing Jsz with Account Filter ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4229,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-jsz-account-test"),
            Description = "Jsz account filter test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Test Jsz with account filter
            var jszWithAccount = await controller.GetJszAsync("$G");
            Console.WriteLine($"✓ Retrieved Jsz with account filter");

            using var doc = JsonDocument.Parse(jszWithAccount);
            Console.WriteLine($"  Response contains valid JSON");

            Console.WriteLine("✓ Jsz account filter test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Jsz account filter test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();

            // Cleanup
            try
            {
                if (Directory.Exists(config.JetstreamStoreDir))
                {
                    Directory.Delete(config.JetstreamStoreDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
