using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests running multiple NATS servers in a single process.
/// Ensures proper isolation and no port conflicts.
/// </summary>
public class MultiServerTests
{
    [Fact]
    public async Task MultipleServersOnDifferentPortsCanStartSimultaneously()
    {
        using var server1 = new NatsController();
        using var server2 = new NatsController();
        using var server3 = new NatsController();

        var config1 = new BrokerConfiguration { Port = 14222, Description = "Server 1" };
        var config2 = new BrokerConfiguration { Port = 14223, Description = "Server 2" };
        var config3 = new BrokerConfiguration { Port = 14224, Description = "Server 3" };

        var result1 = await server1.ConfigureAsync(config1, TestContext.Current.CancellationToken);
        var result2 = await server2.ConfigureAsync(config2, TestContext.Current.CancellationToken);
        var result3 = await server3.ConfigureAsync(config3, TestContext.Current.CancellationToken);

        Assert.True(result1.Success, "Server 1 should start successfully");
        Assert.True(result2.Success, "Server 2 should start successfully");
        Assert.True(result3.Success, "Server 3 should start successfully");

        await Task.Delay(100, TestContext.Current.CancellationToken); // Let servers stabilize

        await server1.ShutdownAsync(TestContext.Current.CancellationToken);
        await server2.ShutdownAsync(TestContext.Current.CancellationToken);
        await server3.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MultipleServersMaintainIndependentConfigurations()
    {
        using var server1 = new NatsController();
        using var server2 = new NatsController();

        await server1.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            Debug = true,
            MaxPayload = 1024
        }, TestContext.Current.CancellationToken);

        await server2.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14223,
            Debug = false,
            MaxPayload = 2048
        }, TestContext.Current.CancellationToken);

        var info1 = await server1.GetInfoAsync(TestContext.Current.CancellationToken);
        var info2 = await server2.GetInfoAsync(TestContext.Current.CancellationToken);

        var config1 = info1.CurrentConfig;
        var config2 = info2.CurrentConfig;

        await server1.ShutdownAsync(TestContext.Current.CancellationToken);
        await server2.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Equal(14222, config1.Port);
        Assert.True(config1.Debug);
        Assert.Equal(1024, config1.MaxPayload);

        Assert.Equal(14223, config2.Port);
        Assert.False(config2.Debug);
        Assert.Equal(2048, config2.MaxPayload);
    }

    [Fact]
    public async Task ConcurrentHotReloadsOnMultipleServersWorkCorrectly()
    {
        using var server1 = new NatsController();
        using var server2 = new NatsController();

        await server1.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        await server2.ConfigureAsync(new BrokerConfiguration { Port = 14223 }, TestContext.Current.CancellationToken);

        // Perform concurrent hot reloads
        var task1 = server1.ApplyChangesAsync(c => c.Debug = true, TestContext.Current.CancellationToken);
        var task2 = server2.ApplyChangesAsync(c => c.Debug = false, TestContext.Current.CancellationToken);

        await Task.WhenAll(task1, task2);

        var result1 = await task1;
        var result2 = await task2;

        Assert.True(result1.Success, "Server 1 hot reload should succeed");
        Assert.True(result2.Success, "Server 2 hot reload should succeed");

        await server1.ShutdownAsync(TestContext.Current.CancellationToken);
        await server2.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SequentialServerLifecycleWorksCorrectly()
    {
        // Start first server
        using var server1 = new NatsController();
        await server1.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        await server1.ShutdownAsync(TestContext.Current.CancellationToken);

        // Start second server on same port after first is stopped
        using var server2 = new NatsController();
        await server2.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        await server2.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MultipleServersWithIndependentJetStreamConfigurations()
    {
        using var server1 = new NatsController();
        using var server2 = new NatsController();

        await server1.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            Jetstream = true,
            JetstreamStoreDir = "./jetstream1"
        }, TestContext.Current.CancellationToken);

        await server2.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14223,
            Jetstream = true,
            JetstreamStoreDir = "./jetstream2"
        }, TestContext.Current.CancellationToken);

        await Task.Delay(100, TestContext.Current.CancellationToken);

        await server1.ShutdownAsync(TestContext.Current.CancellationToken);
        await server2.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StressTest_TenConcurrentServers()
    {
        var servers = new List<NatsController>();
        var tasks = new List<Task>();

        try
        {
            for (int i = 0; i < 10; i++)
            {
                var server = new NatsController();
                servers.Add(server);
                tasks.Add(server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222 + i,
                    Description = $"Stress Test Server {i}"
                }));
            }

            await Task.WhenAll(tasks);

            await Task.Delay(200, TestContext.Current.CancellationToken); // Let servers stabilize

            // Verify all are running
            foreach (var server in servers)
            {
                var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
                Assert.True(server.IsRunning, "Server should be running");
            }
        }
        finally
        {
            // Cleanup
            foreach (var server in servers)
            {
                try { await server.ShutdownAsync(TestContext.Current.CancellationToken); } catch { }
                server.Dispose();
            }
        }
    }
}
