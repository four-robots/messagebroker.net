using System.Text.Json;
using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Integration tests for NATS server monitoring endpoints (Connz, Subsz, Jsz, Routez, Leafz).
/// </summary>
public class MonitoringTests
{
    [Fact]
    public async Task TestConnzMonitoring()
    {
        using var controller = new NatsController();

        // Start server
        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 14222,
            Description = "Connz monitoring test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        // Give server time to start
        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get connection information
            var connz = await controller.GetConnzAsync(cancellationToken: TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(connz);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("num_connections", out _));
            Assert.True(root.TryGetProperty("total", out _));
            Assert.True(root.TryGetProperty("now", out _));
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestSubszMonitoring()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 14223,
            Description = "Subsz monitoring test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get subscription information
            var subsz = await controller.GetSubszAsync(cancellationToken: TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(subsz);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("num_subscriptions", out _));
            Assert.True(root.TryGetProperty("total", out _));
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestJszMonitoring()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 14224,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-jsz-test"),
            Description = "Jsz monitoring test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get JetStream information
            var jsz = await controller.GetJszAsync(cancellationToken: TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(jsz);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("config", out _));
            Assert.True(root.TryGetProperty("streams", out _));
            Assert.True(root.TryGetProperty("consumers", out _));
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task TestRoutezMonitoring()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4225,
            Cluster = new ClusterConfiguration
            {
                Name = "test-cluster",
                Host = "127.0.0.1",
                Port = 16222
            },
            Description = "Routez monitoring test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get route information
            var routez = await controller.GetRoutezAsync(TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(routez);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("num_routes", out _));
            Assert.True(root.TryGetProperty("routes", out _));
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestLeafzMonitoring()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4226,
            LeafNode = new LeafNodeConfiguration
            {
                Host = "127.0.0.1",
                Port = 17422
            },
            Description = "Leafz monitoring test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get leaf node information
            var leafz = await controller.GetLeafzAsync(TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(leafz);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("num_leafs", out _));
            Assert.True(root.TryGetProperty("leafs", out _));
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestClientManagement()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4227,
            Description = "Client management test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get connection list to find a client ID
            var connz = await controller.GetConnzAsync(cancellationToken: TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(connz);
            var root = doc.RootElement;

            // For this test, we're just verifying the methods work even without active clients
            Assert.True(root.ValueKind == JsonValueKind.Object);

            // Test getting non-existent client (should throw or return error)
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await controller.GetClientInfoAsync(99999, TestContext.Current.CancellationToken);
            });

            // Test disconnecting non-existent client (should throw or return error)
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await controller.DisconnectClientAsync(99999, TestContext.Current.CancellationToken);
            });
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestMonitoringWithSubscriptionFilter()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4228,
            Description = "Subscription filter test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Test Connz with subscription filter (wildcards allowed)
            var connzWithSubs = await controller.GetConnzAsync("test.*", TestContext.Current.CancellationToken);
            using var doc1 = JsonDocument.Parse(connzWithSubs);
            Assert.True(doc1.RootElement.ValueKind == JsonValueKind.Object);

            // Test Subsz with filter
            // Note: In NATS 2.12+, the Test field must be a valid publish subject (no wildcards)
            var subszFiltered = await controller.GetSubszAsync("test.example", TestContext.Current.CancellationToken);
            using var doc2 = JsonDocument.Parse(subszFiltered);
            Assert.True(doc2.RootElement.ValueKind == JsonValueKind.Object);
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestJszWithAccountFilter()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4229,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-jsz-account-test"),
            Description = "Jsz account filter test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Test Jsz with account filter
            var jszWithAccount = await controller.GetJszAsync("$G", TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(jszWithAccount);
            Assert.True(doc.RootElement.ValueKind == JsonValueKind.Object);
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task TestAccountzMonitoring()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4230,
            Description = "Accountz monitoring test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get account information
            var accountz = await controller.GetAccountzAsync(cancellationToken: TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(accountz);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("system_account", out _));
            Assert.True(root.TryGetProperty("accounts", out _));
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestVarzMonitoring()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4231,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-varz-test"),
            Description = "Varz monitoring test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get full server variables
            var varz = await controller.GetVarzAsync(TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(varz);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("server_id", out _));
            Assert.True(root.TryGetProperty("version", out _));
            Assert.True(root.TryGetProperty("go", out _));
            Assert.True(root.TryGetProperty("cores", out _));
            Assert.True(root.TryGetProperty("mem", out _));
            Assert.True(root.TryGetProperty("connections", out _));
            Assert.True(root.TryGetProperty("jetstream", out _));
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task TestGatewayzMonitoring()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4232,
            Description = "Gatewayz monitoring test"
            // Note: Gateway configuration would be needed for actual gateway connections
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get gateway information
            var gatewayz = await controller.GetGatewayzAsync(cancellationToken: TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(gatewayz);
            var root = doc.RootElement;

            // Even without gateway configuration, the endpoint should return valid data
            Assert.True(root.TryGetProperty("server_id", out _));
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }
}
