using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Implementation;

namespace MessageBroker.IntegrationTests;

/// <summary>
/// Tests hot reload functionality for various configuration scenarios.
/// </summary>
public class ConfigurationReloadTests : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        // Test 1: Basic hot reload
        await results.AssertAsync(
            "Basic hot reload changes configuration",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222, Debug = false });

                var result = await server.ApplyChangesAsync(c => c.Debug = true);

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return result.Success && info.CurrentConfiguration.Debug == true;
            });

        // Test 2: Multiple property hot reload
        await results.AssertAsync(
            "Hot reload multiple properties simultaneously",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    Debug = false,
                    Trace = false,
                    MaxPayload = 1024
                });

                var result = await server.ApplyChangesAsync(c =>
                {
                    c.Debug = true;
                    c.Trace = true;
                    c.MaxPayload = 2048;
                });

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return result.Success &&
                       info.CurrentConfiguration.Debug == true &&
                       info.CurrentConfiguration.Trace == true &&
                       info.CurrentConfiguration.MaxPayload == 2048;
            });

        // Test 3: Version tracking during reloads
        await results.AssertAsync(
            "Hot reload increments version number",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

                var info1 = await server.GetServerInfoAsync();
                var version1 = info1.CurrentVersion;

                await server.ApplyChangesAsync(c => c.Debug = true);

                var info2 = await server.GetServerInfoAsync();
                var version2 = info2.CurrentVersion;

                await server.ShutdownAsync();

                return version2 > version1;
            });

        // Test 4: Rollback after hot reload
        await results.AssertAsync(
            "Rollback restores previous configuration",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222, Debug = false });

                await server.ApplyChangesAsync(c => c.Debug = true);

                var rollbackResult = await server.RollbackAsync(toVersion: 1);

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return rollbackResult.Success && info.CurrentConfiguration.Debug == false;
            });

        // Test 5: Multiple sequential reloads
        await results.AssertAsync(
            "Multiple sequential hot reloads work correctly",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222, MaxPayload = 1024 });

                for (int i = 1; i <= 10; i++)
                {
                    await server.ApplyChangesAsync(c => c.MaxPayload = 1024 * (i + 1));
                }

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfiguration.MaxPayload == 1024 * 11;
            });

        // Test 6: Hot reload with validation failure
        await results.AssertAsync(
            "Hot reload with validation failure preserves original config",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222, MaxPayload = 1024 });

                var result = await server.ApplyChangesAsync(c => c.MaxPayload = -1);

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return !result.Success && info.CurrentConfiguration.MaxPayload == 1024;
            });

        // Test 7: JetStream hot reload
        await results.AssertAsync(
            "JetStream can be toggled via hot reload",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    Jetstream = false
                });

                var result = await server.ApplyChangesAsync(c =>
                {
                    c.Jetstream = true;
                    c.JetstreamStoreDir = "./jetstream-test";
                });

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfiguration.Jetstream == true;
            });

        // Test 8: Hot reload version history
        await results.AssertAsync(
            "Version history tracks all changes",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

                for (int i = 0; i < 5; i++)
                {
                    await server.ApplyChangesAsync(c => c.MaxPayload = 1024 + (i * 100));
                }

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return info.VersionHistory.Count >= 6; // Initial + 5 changes
            });

        // Test 9: Complex nested configuration reload
        await results.AssertAsync(
            "Hot reload of nested configuration (LeafNode)",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 0,
                        ImportSubjects = new List<string>()
                    }
                });

                var result = await server.ApplyChangesAsync(c =>
                {
                    c.LeafNode.Port = 7422;
                    c.LeafNode.ImportSubjects.Add("test.>");
                });

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return result.Success &&
                       info.CurrentConfiguration.LeafNode.Port == 7422 &&
                       info.CurrentConfiguration.LeafNode.ImportSubjects.Contains("test.>");
            });

        // Test 10: Hot reload preserves unmodified properties
        await results.AssertAsync(
            "Hot reload preserves unmodified properties",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    Debug = false,
                    Trace = false,
                    MaxPayload = 1024,
                    MaxControlLine = 4096
                });

                await server.ApplyChangesAsync(c => c.Debug = true);

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfiguration.Debug == true &&
                       info.CurrentConfiguration.Trace == false &&
                       info.CurrentConfiguration.MaxPayload == 1024 &&
                       info.CurrentConfiguration.MaxControlLine == 4096;
            });

        // Test 11: Fluent API extensions hot reload
        await results.AssertAsync(
            "Fluent API extensions work for hot reload",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

                await server.SetDebugAsync(true);
                await server.SetMaxPayloadAsync(2048);

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfiguration.Debug == true &&
                       info.CurrentConfiguration.MaxPayload == 2048;
            });

        // Test 12: Authentication hot reload
        await results.AssertAsync(
            "Authentication can be changed via hot reload",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

                await server.SetAuthenticationAsync("user", "pass");

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfiguration.Auth.Username == "user" &&
                       info.CurrentConfiguration.Auth.Password == "pass";
            });
    }
}
