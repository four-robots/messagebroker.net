using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests cluster configuration including hot reload of cluster settings.
/// </summary>
public class ClusterConfigurationTests
{
    [Fact]
    public async Task ConfigureClusterWithNameAndRoutes()
    {
        using var server = new NatsController();
        try
        {
            await server.ConfigureAsync(new BrokerConfiguration
            {
                Port = 14222,
                Cluster = new ClusterConfiguration
                {
                    Name = "test-cluster",
                    Port = 16222,
                    Routes = new List<string> { "nats-route://server1:16222", "nats-route://server2:16222" }
                }
            }, TestContext.Current.CancellationToken);

            var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
            var cluster = info.CurrentConfig.Cluster;

            Assert.Equal("test-cluster", cluster.Name);
            Assert.Equal(16222, cluster.Port);
            Assert.Equal(2, cluster.Routes.Count);
            Assert.Contains("nats-route://server1:16222", cluster.Routes);
            Assert.Contains("nats-route://server2:16222", cluster.Routes);
        }
        finally
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task EnableClusteringUsingFluentApi()
    {
        using var server = new NatsController();
        try
        {
            await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);

            var result = await server.EnableClusteringAsync("production-cluster", 16222, cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success, "Failed to enable clustering");

            var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
            var cluster = info.CurrentConfig.Cluster;

            Assert.Equal("production-cluster", cluster.Name);
            Assert.Equal(16222, cluster.Port);
        }
        finally
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task HotReloadAddRoutesToCluster()
    {
        using var server = new NatsController();
        try
        {
            await server.ConfigureAsync(new BrokerConfiguration
            {
                Port = 14222,
                Cluster = new ClusterConfiguration
                {
                    Name = "test-cluster",
                    Port = 16222,
                    Routes = new List<string> { "nats-route://server1:16222" }
                }
            }, TestContext.Current.CancellationToken);

            var result = await server.AddClusterRoutesAsync(
                "nats-route://server2:16222",
                "nats-route://server3:16222");

            Assert.True(result.Success, "Failed to add cluster routes");

            var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
            var routes = info.CurrentConfig.Cluster.Routes;

            Assert.Equal(3, routes.Count);
            Assert.Contains("nats-route://server1:16222", routes);
            Assert.Contains("nats-route://server2:16222", routes);
            Assert.Contains("nats-route://server3:16222", routes);
        }
        finally
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task HotReloadRemoveRoutesFromCluster()
    {
        using var server = new NatsController();
        try
        {
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
            }, TestContext.Current.CancellationToken);

            var result = await server.RemoveClusterRoutesAsync("nats-route://server2:16222");

            Assert.True(result.Success, "Failed to remove cluster route");

            var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
            var routes = info.CurrentConfig.Cluster.Routes;

            Assert.Equal(2, routes.Count);
            Assert.Contains("nats-route://server1:16222", routes);
            Assert.Contains("nats-route://server3:16222", routes);
            Assert.DoesNotContain("nats-route://server2:16222", routes);
        }
        finally
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ConfigureClusterWithAuthentication()
    {
        using var server = new NatsController();
        try
        {
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
            }, TestContext.Current.CancellationToken);

            var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
            var cluster = info.CurrentConfig.Cluster;

            Assert.Equal("clusteradmin", cluster.AuthUsername);
            Assert.Equal("secretpass", cluster.AuthPassword);
        }
        finally
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task UpdateClusterAuthenticationUsingFluentApi()
    {
        using var server = new NatsController();
        try
        {
            await server.ConfigureAsync(new BrokerConfiguration
            {
                Port = 14222,
                Cluster = new ClusterConfiguration
                {
                    Name = "test-cluster",
                    Port = 16222
                }
            }, TestContext.Current.CancellationToken);

            var result = await server.SetClusterAuthenticationAsync("newuser", "newpass", cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success, "Failed to set cluster authentication");

            var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
            var cluster = info.CurrentConfig.Cluster;

            Assert.Equal("newuser", cluster.AuthUsername);
            Assert.Equal("newpass", cluster.AuthPassword);
        }
        finally
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task DisableClusteringUsingFluentApi()
    {
        using var server = new NatsController();
        try
        {
            await server.ConfigureAsync(new BrokerConfiguration
            {
                Port = 14222,
                Cluster = new ClusterConfiguration
                {
                    Name = "test-cluster",
                    Port = 16222,
                    Routes = new List<string> { "nats-route://server1:16222" }
                }
            }, TestContext.Current.CancellationToken);

            var result = await server.DisableClusteringAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success, "Failed to disable clustering");

            var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
            var cluster = info.CurrentConfig.Cluster;

            Assert.Equal(0, cluster.Port);
            Assert.Null(cluster.Name);
            Assert.Empty(cluster.Routes);
        }
        finally
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ValidationClusterPortCannotBeSameAsMainPort()
    {
        using var server = new NatsController();
        try
        {
            var result = await server.ConfigureAsync(new BrokerConfiguration
            {
                Port = 14222,
                Cluster = new ClusterConfiguration
                {
                    Name = "test-cluster",
                    Port = 14222  // Same as main port
                }
            }, TestContext.Current.CancellationToken);

            Assert.False(result.Success, "Validation should have failed for port conflict");
        }
        finally
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ValidationClusterRequiresNameWhenEnabled()
    {
        using var server = new NatsController();
        try
        {
            var result = await server.ConfigureAsync(new BrokerConfiguration
            {
                Port = 14222,
                Cluster = new ClusterConfiguration
                {
                    Port = 16222,
                    Name = null  // Missing required name
                }
            }, TestContext.Current.CancellationToken);

            Assert.False(result.Success, "Validation should have failed for missing cluster name");
        }
        finally
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }
}
