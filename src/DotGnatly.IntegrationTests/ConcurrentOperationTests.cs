using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests concurrent operations on brokers.
/// </summary>
public class ConcurrentOperationTests : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        // Test 1: Concurrent configuration changes on same server
        await results.AssertNoExceptionAsync(
            "Concurrent configuration changes are handled safely",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 });

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
                await server.ShutdownAsync();
            });

        // Test 2: Concurrent reads during configuration change
        await results.AssertNoExceptionAsync(
            "Concurrent reads during configuration changes",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 });

                var writeTask = Task.Run(async () =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        await server.ApplyChangesAsync(c => c.Debug = !c.Debug);
                        await Task.Delay(10);
                    }
                });

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
                await server.ShutdownAsync();
            });

        // Test 3: Concurrent leaf node subject modifications
        await results.AssertNoExceptionAsync(
            "Concurrent leaf node subject modifications",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    LeafNode = new LeafNodeConfiguration { Port = 17422 }
                });

                var tasks = new List<Task>();
                for (int i = 0; i < 5; i++)
                {
                    int index = i;
                    tasks.Add(server.AddLeafNodeImportSubjectsAsync($"subject{index}.>"));
                }

                await Task.WhenAll(tasks);

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                if (info.CurrentConfig.LeafNode.ImportSubjects.Count != 5)
                {
                    throw new Exception($"Expected 5 subjects, got {info.CurrentConfig.LeafNode.ImportSubjects.Count}");
                }
            });

        // Test 4: Concurrent operations on multiple servers
        await results.AssertNoExceptionAsync(
            "Concurrent operations on multiple independent servers",
            async () =>
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
                        try { await server.ShutdownAsync(); } catch { }
                        server.Dispose();
                    }
                }
            });

        // Test 5: Rapid sequential changes
        await results.AssertNoExceptionAsync(
            "Rapid sequential configuration changes",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 });

                for (int i = 0; i < 20; i++)
                {
                    await server.ApplyChangesAsync(c => c.MaxPayload = 1024 + (i * 100));
                }

                await server.ShutdownAsync();
            });

        // Test 6: Concurrent rollbacks
        await results.AssertAsync(
            "Concurrent rollback attempts are handled safely",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 });

                // Create some version history
                for (int i = 0; i < 5; i++)
                {
                    await server.ApplyChangesAsync(c => c.MaxPayload = 1024 + (i * 100));
                }

                // Try concurrent rollbacks
                var tasks = new List<Task<ConfigurationResult>>();
                for (int i = 0; i < 3; i++)
                {
                    tasks.Add(server.RollbackAsync(toVersion: 2));
                }

                var rollbackResults = await Task.WhenAll(tasks);
                await server.ShutdownAsync();

                // At least one should succeed
                return rollbackResults.Any(r => r.Success);
            });

        // Test 7: Stress test - many rapid operations
        await results.AssertNoExceptionAsync(
            "Stress test: 100 rapid operations across multiple servers",
            async () =>
            {
                var servers = new List<NatsController>();
                try
                {
                    // Create 3 servers
                    for (int i = 0; i < 3; i++)
                    {
                        var server = new NatsController();
                        servers.Add(server);
                        await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 + i });
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
                        try { await server.ShutdownAsync(); } catch { }
                        server.Dispose();
                    }
                }
            });
    }
}
