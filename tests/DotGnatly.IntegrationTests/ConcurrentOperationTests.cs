using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests concurrent operations on brokers.
/// </summary>
public class ConcurrentOperationTests
{
    [Fact]
    public async Task TestConcurrentConfigurationChanges()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                await server.ApplyChangesAsync(c => c.MaxPayload = 1024 + (index * 100));
            }));
        }

        await Task.WhenAll(tasks);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TestConcurrentReadsDuringConfigurationChanges()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        var writeTask = Task.Run(async () =>
        {
            for (int i = 0; i < 5; i++)
            {
                await server.ApplyChangesAsync(c => c.Debug = !c.Debug);
                await Task.Delay(10);
            }
        }, TestContext.Current.CancellationToken);

        var readTasks = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            readTasks.Add(Task.Run(async () =>
            {
                await server.GetInfoAsync();
                await Task.Delay(5);
            }));
        }

        await Task.WhenAll(readTasks);
        await writeTask;
        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    // Test 3: Concurrent leaf node subject modifications
    // DISABLED: Known NATS server limitation - concurrent leaf node permission updates don't synchronize properly
    // See: https://github.com/nats-io/nats-server/issues/2949
    // See: https://github.com/nats-io/nats-server/issues/4608
    // Workaround: Configure subjects at startup or modify them sequentially rather than concurrently
    /*
    [Fact]
    public async Task TestConcurrentLeafNodeSubjectModifications()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration { Port = 17422 }
        });

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            tasks.Add(server.AddLeafNodeImportSubjectsAsync($"subject{index}.>"));
        }

        await Task.WhenAll(tasks);

        var info = await server.GetInfoAsync();
        await server.ShutdownAsync();

        Assert.Equal(5, info.CurrentConfig.LeafNode.ImportSubjects.Count);
    }
    */

    [Fact]
    public async Task TestConcurrentOperationsOnMultipleServers()
    {
        var servers = new List<NatsController>();
        var tasks = new List<Task>();

        try
        {
            for (int i = 0; i < 5; i++)
            {
                var server = new NatsController();
                servers.Add(server);
                int port = 14222 + i;
                tasks.Add(server.ConfigureAsync(new BrokerConfiguration { Port = port }));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();

            // Perform concurrent operations
            foreach (var server in servers)
            {
                tasks.Add(server.ApplyChangesAsync(c => c.Debug = true));
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            foreach (var server in servers)
            {
                try { await server.ShutdownAsync(TestContext.Current.CancellationToken); } catch { }
                server.Dispose();
            }
        }
    }

    [Fact]
    public async Task TestRapidSequentialChanges()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        for (int i = 0; i < 20; i++)
        {
            await server.ApplyChangesAsync(c => c.MaxPayload = 1024 + (i * 100), TestContext.Current.CancellationToken);
        }

        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TestConcurrentRollbacks()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        // Create some version history
        for (int i = 0; i < 5; i++)
        {
            await server.ApplyChangesAsync(c => c.MaxPayload = 1024 + (i * 100), TestContext.Current.CancellationToken);
        }

        // Try concurrent rollbacks
        var tasks = new List<Task<ConfigurationResult>>();
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(server.RollbackAsync(toVersion: 2));
        }

        var rollbackResults = await Task.WhenAll(tasks);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        // At least one should succeed
        Assert.True(rollbackResults.Any(r => r.Success), "At least one rollback should succeed");
    }

    [Fact]
    public async Task TestStressTest_100RapidOperations()
    {
        var servers = new List<NatsController>();
        try
        {
            // Create 3 servers
            for (int i = 0; i < 3; i++)
            {
                var server = new NatsController();
                servers.Add(server);
                var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 + i }, TestContext.Current.CancellationToken);
                Assert.True(result.Success, $"Failed to start server {i}: {result.ErrorMessage}");
            }

            // Perform 100 operations across the servers
            var tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                var server = servers[i % servers.Count];
                int value = i;
                tasks.Add(server.ApplyChangesAsync(c => c.MaxPayload = 1024 + value));
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            foreach (var server in servers)
            {
                try { await server.ShutdownAsync(TestContext.Current.CancellationToken); } catch { }
                server.Dispose();
            }
        }
    }
}
