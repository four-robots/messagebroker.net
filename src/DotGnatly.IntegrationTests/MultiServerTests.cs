using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests running multiple NATS servers in a single process.
/// Ensures proper isolation and no port conflicts.
/// </summary>
public class MultiServerTests : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        // Test 1: Start multiple servers on different ports
        await results.AssertNoExceptionAsync(
            "Multiple servers on different ports can start simultaneously",
            async () =>
            {
                using var server1 = new NatsController();
                using var server2 = new NatsController();
                using var server3 = new NatsController();

                var config1 = new BrokerConfiguration { Port = 14222, Description = "Server 1" };
                var config2 = new BrokerConfiguration { Port = 14223, Description = "Server 2" };
                var config3 = new BrokerConfiguration { Port = 14224, Description = "Server 3" };

                var result1 = await server1.ConfigureAsync(config1);
                var result2 = await server2.ConfigureAsync(config2);
                var result3 = await server3.ConfigureAsync(config3);

                if (!result1.Success || !result2.Success || !result3.Success)
                {
                    throw new Exception("One or more servers failed to start");
                }

                await Task.Delay(100); // Let servers stabilize

                await server1.ShutdownAsync();
                await server2.ShutdownAsync();
                await server3.ShutdownAsync();
            });

        // Test 2: Verify servers are truly isolated (independent configurations)
        await results.AssertAsync(
            "Multiple servers maintain independent configurations",
            async () =>
            {
                using var server1 = new NatsController();
                using var server2 = new NatsController();

                await server1.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Debug = true,
                    MaxPayload = 1024
                });

                await server2.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14223,
                    Debug = false,
                    MaxPayload = 2048
                });

                var info1 = await server1.GetInfoAsync();
                var info2 = await server2.GetInfoAsync();

                var config1 = info1.CurrentConfig;
                var config2 = info2.CurrentConfig;

                await server1.ShutdownAsync();
                await server2.ShutdownAsync();

                return config1.Port == 14222 && config1.Debug == true && config1.MaxPayload == 1024 &&
                       config2.Port == 14223 && config2.Debug == false && config2.MaxPayload == 2048;
            });

        // Test 3: Concurrent hot reloads on different servers
        await results.AssertNoExceptionAsync(
            "Concurrent hot reloads on multiple servers work correctly",
            async () =>
            {
                using var server1 = new NatsController();
                using var server2 = new NatsController();

                await server1.ConfigureAsync(new BrokerConfiguration { Port = 14222 });
                await server2.ConfigureAsync(new BrokerConfiguration { Port = 14223 });

                // Perform concurrent hot reloads
                var task1 = server1.ApplyChangesAsync(c => c.Debug = true);
                var task2 = server2.ApplyChangesAsync(c => c.Debug = false);

                await Task.WhenAll(task1, task2);

                if (!task1.Result.Success || !task2.Result.Success)
                {
                    throw new Exception("Concurrent hot reload failed");
                }

                await server1.ShutdownAsync();
                await server2.ShutdownAsync();
            });

        // Test 4: Sequential server lifecycle (start, stop, start again)
        await results.AssertNoExceptionAsync(
            "Sequential server lifecycle works correctly",
            async () =>
            {
                // Start first server
                using var server1 = new NatsController();
                await server1.ConfigureAsync(new BrokerConfiguration { Port = 14222 });
                await server1.ShutdownAsync();

                // Start second server on same port after first is stopped
                using var server2 = new NatsController();
                await server2.ConfigureAsync(new BrokerConfiguration { Port = 14222 });
                await server2.ShutdownAsync();
            });

        // Test 5: Servers with different JetStream configurations
        await results.AssertNoExceptionAsync(
            "Multiple servers with independent JetStream configurations",
            async () =>
            {
                using var server1 = new NatsController();
                using var server2 = new NatsController();

                await server1.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Jetstream = true,
                    JetstreamStoreDir = "./jetstream1"
                });

                await server2.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14223,
                    Jetstream = true,
                    JetstreamStoreDir = "./jetstream2"
                });

                await Task.Delay(100);

                await server1.ShutdownAsync();
                await server2.ShutdownAsync();
            });

        // Test 6: Many servers stress test (10 servers)
        await results.AssertNoExceptionAsync(
            "Stress test: 10 concurrent servers",
            async () =>
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

                    await Task.Delay(200); // Let servers stabilize

                    // Verify all are running
                    foreach (var server in servers)
                    {
                        var info = await server.GetInfoAsync();
                        if (!server.IsRunning)
                        {
                            throw new Exception($"Server failed to start");
                        }
                    }
                }
                finally
                {
                    // Cleanup
                    foreach (var server in servers)
                    {
                        try { await server.ShutdownAsync(); } catch { }
                        server.Dispose();
                    }
                }
            });
    }
}
