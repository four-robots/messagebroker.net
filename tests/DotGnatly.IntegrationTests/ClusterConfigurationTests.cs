using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests cluster configuration including hot reload of cluster settings.
/// </summary>
public class ClusterConfigurationTests : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        // Test 1: Configure cluster with routes
        await results.AssertNoExceptionAsync(
            "Configure cluster with name and routes",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Cluster = new ClusterConfiguration
                    {
                        Name = "test-cluster",
                        Port = 16222,
                        Routes = new List<string> { "nats-route://server1:16222", "nats-route://server2:16222" }
                    }
                });

                var info = await server.GetInfoAsync();
                var cluster = info.CurrentConfig.Cluster;

                if (cluster.Name != "test-cluster")
                {
                    throw new Exception("Cluster name not configured correctly");
                }

                if (cluster.Port != 16222)
                {
                    throw new Exception("Cluster port not configured correctly");
                }

                if (cluster.Routes.Count != 2 ||
                    !cluster.Routes.Contains("nats-route://server1:16222") ||
                    !cluster.Routes.Contains("nats-route://server2:16222"))
                {
                    throw new Exception("Cluster routes not configured correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 2: Enable clustering using fluent API
        await results.AssertNoExceptionAsync(
            "Enable clustering using fluent API",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 });

                var result = await server.EnableClusteringAsync("production-cluster", 16222);

                if (!result.Success)
                {
                    throw new Exception("Failed to enable clustering");
                }

                var info = await server.GetInfoAsync();
                var cluster = info.CurrentConfig.Cluster;

                if (cluster.Name != "production-cluster" || cluster.Port != 16222)
                {
                    throw new Exception("Cluster not enabled correctly via fluent API");
                }

                await server.ShutdownAsync();
            });

        // Test 3: Hot reload - Add cluster routes
        await results.AssertNoExceptionAsync(
            "Hot reload: Add routes to cluster",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Cluster = new ClusterConfiguration
                    {
                        Name = "test-cluster",
                        Port = 16222,
                        Routes = new List<string> { "nats-route://server1:16222" }
                    }
                });

                var result = await server.AddClusterRoutesAsync(
                    "nats-route://server2:16222",
                    "nats-route://server3:16222");

                if (!result.Success)
                {
                    throw new Exception("Failed to add cluster routes");
                }

                var info = await server.GetInfoAsync();
                var routes = info.CurrentConfig.Cluster.Routes;

                if (routes.Count != 3 ||
                    !routes.Contains("nats-route://server1:16222") ||
                    !routes.Contains("nats-route://server2:16222") ||
                    !routes.Contains("nats-route://server3:16222"))
                {
                    throw new Exception("Cluster routes not updated correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 4: Hot reload - Remove cluster routes
        await results.AssertNoExceptionAsync(
            "Hot reload: Remove routes from cluster",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Cluster = new ClusterConfiguration
                    {
                        Name = "test-cluster",
                        Port = 16222,
                        Routes = new List<string>
                        {
                            "nats-route://server1:16222",
                            "nats-route://server2:16222",
                            "nats-route://server3:16222"
                        }
                    }
                });

                var result = await server.RemoveClusterRoutesAsync("nats-route://server2:16222");

                if (!result.Success)
                {
                    throw new Exception("Failed to remove cluster route");
                }

                var info = await server.GetInfoAsync();
                var routes = info.CurrentConfig.Cluster.Routes;

                if (routes.Count != 2 ||
                    !routes.Contains("nats-route://server1:16222") ||
                    !routes.Contains("nats-route://server3:16222") ||
                    routes.Contains("nats-route://server2:16222"))
                {
                    throw new Exception("Cluster routes not updated correctly after removal");
                }

                await server.ShutdownAsync();
            });

        // Test 5: Configure cluster authentication
        await results.AssertNoExceptionAsync(
            "Configure cluster with authentication",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Cluster = new ClusterConfiguration
                    {
                        Name = "secure-cluster",
                        Port = 16222,
                        AuthUsername = "clusteradmin",
                        AuthPassword = "secretpass"
                    }
                });

                var info = await server.GetInfoAsync();
                var cluster = info.CurrentConfig.Cluster;

                if (cluster.AuthUsername != "clusteradmin" || cluster.AuthPassword != "secretpass")
                {
                    throw new Exception("Cluster authentication not configured correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 6: Update cluster authentication using fluent API
        await results.AssertNoExceptionAsync(
            "Update cluster authentication using fluent API",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Cluster = new ClusterConfiguration
                    {
                        Name = "test-cluster",
                        Port = 16222
                    }
                });

                var result = await server.SetClusterAuthenticationAsync("newuser", "newpass");

                if (!result.Success)
                {
                    throw new Exception("Failed to set cluster authentication");
                }

                var info = await server.GetInfoAsync();
                var cluster = info.CurrentConfig.Cluster;

                if (cluster.AuthUsername != "newuser" || cluster.AuthPassword != "newpass")
                {
                    throw new Exception("Cluster authentication not updated correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 7: Disable clustering
        await results.AssertNoExceptionAsync(
            "Disable clustering using fluent API",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Cluster = new ClusterConfiguration
                    {
                        Name = "test-cluster",
                        Port = 16222,
                        Routes = new List<string> { "nats-route://server1:16222" }
                    }
                });

                var result = await server.DisableClusteringAsync();

                if (!result.Success)
                {
                    throw new Exception("Failed to disable clustering");
                }

                var info = await server.GetInfoAsync();
                var cluster = info.CurrentConfig.Cluster;

                if (cluster.Port != 0 || cluster.Name != null || cluster.Routes.Count != 0)
                {
                    throw new Exception("Clustering not disabled correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 8: Validation - Cluster port conflicts with main port
        await results.AssertExceptionAsync<Exception>(
            "Validation: Cluster port cannot be same as main port",
            async () =>
            {
                using var server = new NatsController();
                var result = await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Cluster = new ClusterConfiguration
                    {
                        Name = "test-cluster",
                        Port = 14222  // Same as main port
                    }
                });

                if (result.Success)
                {
                    throw new Exception("Validation should have failed for port conflict");
                }

                await server.ShutdownAsync();
            });

        // Test 9: Validation - Cluster requires name
        await results.AssertExceptionAsync<Exception>(
            "Validation: Cluster requires name when enabled",
            async () =>
            {
                using var server = new NatsController();
                var result = await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Cluster = new ClusterConfiguration
                    {
                        Port = 16222,
                        Name = null  // Missing required name
                    }
                });

                if (result.Success)
                {
                    throw new Exception("Validation should have failed for missing cluster name");
                }

                await server.ShutdownAsync();
            });
    }
}
