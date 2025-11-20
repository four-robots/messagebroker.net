using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;

namespace DotGnatly.IntegrationTests;

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
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222, Debug = false });

                var result = await server.ApplyChangesAsync(c => c.Debug = true);

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                return result.Success && info.CurrentConfig.Debug == true;
            });

        // Test 2: Multiple property hot reload
        await results.AssertAsync(
            "Hot reload multiple properties simultaneously",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
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

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                return result.Success &&
                       info.CurrentConfig.Debug == true &&
                       info.CurrentConfig.Trace == true &&
                       info.CurrentConfig.MaxPayload == 2048;
            });

        // Test 3: Version tracking during reloads
        await results.AssertAsync(
            "Hot reload increments version number",
            async () =>
            {
                using var server = new NatsController();
                var result1 = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 });
                var version1 = result1.Version?.Version ?? 0;

                var result2 = await server.ApplyChangesAsync(c => c.Debug = true);
                var version2 = result2.Version?.Version ?? 0;

                await server.ShutdownAsync();

                return version2 > version1;
            });

        // Test 4: Rollback after hot reload
        await results.AssertAsync(
            "Rollback restores previous configuration",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222, Debug = false });

                await server.ApplyChangesAsync(c => c.Debug = true);

                var rollbackResult = await server.RollbackAsync(toVersion: 1);

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                return rollbackResult.Success && info.CurrentConfig.Debug == false;
            });

        // Test 5: Multiple sequential reloads
        await results.AssertAsync(
            "Multiple sequential hot reloads work correctly",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222, MaxPayload = 1024 });

                for (int i = 1; i <= 10; i++)
                {
                    await server.ApplyChangesAsync(c => c.MaxPayload = 1024 * (i + 1));
                }

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfig.MaxPayload == 1024 * 11;
            });

        // Test 6: Hot reload with validation failure
        await results.AssertAsync(
            "Hot reload with validation failure preserves original config",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222, MaxPayload = 1024 });

                var result = await server.ApplyChangesAsync(c => c.MaxPayload = -1);

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                return !result.Success && info.CurrentConfig.MaxPayload == 1024;
            });

        // Test 7: JetStream hot reload
        await results.AssertAsync(
            "JetStream can be toggled via hot reload",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Jetstream = false
                });

                var result = await server.ApplyChangesAsync(c =>
                {
                    c.Jetstream = true;
                    c.JetstreamStoreDir = "./jetstream-test";
                });

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfig.Jetstream == true;
            });

        // Test 8: Hot reload version tracking
        await results.AssertAsync(
            "Version number increments with each change",
            async () =>
            {
                using var server = new NatsController();
                var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 });
                var initialVersion = result.Version?.Version ?? 0;

                ConfigurationResult? lastResult = null;
                for (int i = 0; i < 5; i++)
                {
                    lastResult = await server.ApplyChangesAsync(c => c.MaxPayload = 1024 + (i * 100));
                }

                await server.ShutdownAsync();

                // Verify that the last version is at least 6 (initial + 5 changes)
                return lastResult != null && lastResult.Version != null && lastResult.Version.Version >= initialVersion + 5;
            });

        // Test 9: LeafNode configuration cannot be hot reloaded (NATS limitation)
        await results.AssertAsync(
            "LeafNode configuration changes require restart (expected failure)",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 0,
                        ImportSubjects = new List<string>()
                    }
                });

                // Attempt to hot reload LeafNode configuration
                var result = await server.ApplyChangesAsync(c =>
                {
                    c.LeafNode.Port = 17422;
                    c.LeafNode.ImportSubjects.Add("test.>");
                });

                await server.ShutdownAsync();

                // NATS server does not support hot reloading LeafNode configuration
                // This should fail with an appropriate error message
                if (result.Success)
                    throw new Exception("Expected hot reload to fail for LeafNode changes, but it succeeded");

                if (result.ErrorMessage == null || !result.ErrorMessage.Contains("LeafNode"))
                    throw new Exception($"Expected error message to mention LeafNode limitation, but got: {result.ErrorMessage}");

                return true;
            });

        // Test 10: Hot reload preserves unmodified properties
        await results.AssertAsync(
            "Hot reload preserves unmodified properties",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14222,
                    Debug = false,
                    Trace = false,
                    MaxPayload = 1024,
                    MaxControlLine = 4096
                });

                await server.ApplyChangesAsync(c => c.Debug = true);

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfig.Debug == true &&
                       info.CurrentConfig.Trace == false &&
                       info.CurrentConfig.MaxPayload == 1024 &&
                       info.CurrentConfig.MaxControlLine == 4096;
            });

        // Test 11: Fluent API extensions hot reload
        await results.AssertAsync(
            "Fluent API extensions work for hot reload",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 });

                await server.SetDebugAsync(true);
                await server.SetMaxPayloadAsync(2048);

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfig.Debug == true &&
                       info.CurrentConfig.MaxPayload == 2048;
            });

        // Test 12: Authentication hot reload
        await results.AssertAsync(
            "Authentication can be changed via hot reload",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 });

                await server.SetAuthenticationAsync("user", "pass");

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfig.Auth.Username == "user" &&
                       info.CurrentConfig.Auth.Password == "pass";
            });
    }
}
